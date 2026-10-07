using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Planos;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Application.Common.Health;
using EquilibraFitPlusPlus.Application.Common.Serialization;
using EquilibraFitPlusPlus.Application.Features.Planos.Mappings;
using EquilibraFitPlusPlus.Contracts.Planos;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Planos.Commands.GerarPlanoAlimentar;

/// <summary>
/// Handles personalized diet plan generation.
/// </summary>
public sealed class GerarPlanoAlimentarCommandHandler : IRequestHandler<GerarPlanoAlimentarCommand, Result<PlanoAlimentarGeradoResponse>>
{
    private readonly IPlanoAlimentarRepository _planoRepository;
    private readonly IAiCoachClient _aiCoachClient;
    private readonly IAiInstructionRepository _aiInstructionRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public GerarPlanoAlimentarCommandHandler(
        IPlanoAlimentarRepository planoRepository,
        IAiCoachClient aiCoachClient,
        IAiInstructionRepository aiInstructionRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _planoRepository = planoRepository;
        _aiCoachClient = aiCoachClient;
        _aiInstructionRepository = aiInstructionRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<PlanoAlimentarGeradoResponse>> Handle(GerarPlanoAlimentarCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<PlanoAlimentarGeradoResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        PerfilSaude? perfil = await _planoRepository.ObterPerfilAsync(command.TenantId, command.UsuarioId, cancellationToken);
        if (perfil is null)
        {
            return Result<PlanoAlimentarGeradoResponse>.Failure(new Error(
                "perfil.questionario_necessario",
                "Vamos preencher o questionário inicial para gerar um plano alimentar com mais segurança."));
        }

        PlanBounds bounds = CalculateBounds(perfil);
        string[] preferences = Merge(JsonStringCollection.Deserialize(perfil.PreferenciasJson), command.Request.Preferencias);
        string[] restrictions = Merge(JsonStringCollection.Deserialize(perfil.RestricoesJson), command.Request.Restricoes);
        AiInstructionSet? instructions = await _aiInstructionRepository.ObterInstrucoesPublicadasAsync(command.TenantId, cancellationToken);

        Result<AiPlanGenerationClientReply> aiResult = await _aiCoachClient.GerarPlanoAlimentarAsync(
            new AiPlanGenerationClientRequest(
                perfil.Objetivo.ToString(),
                command.Request.Rotina,
                preferences,
                restrictions,
                bounds.MinCalories,
                bounds.MaxCalories,
                bounds.ProteinTargetG,
                instructions?.ToPromptFragment()),
            cancellationToken);

        AiPlanGenerationClientReply generated = aiResult.IsSuccess
            ? aiResult.Value!
            : CreateHybridPlan(perfil, bounds, preferences, restrictions);

        PlanoUsuario? planoAtivo = await _planoRepository.ObterPlanoAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken);
        if (planoAtivo is not null)
        {
            planoAtivo.Status = StatusPlano.Substituido;
        }

        var plano = new PlanoUsuario
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            Versao = await _planoRepository.ObterProximaVersaoAsync(command.TenantId, command.UsuarioId, cancellationToken),
            Status = StatusPlano.Ativo,
            CaloriasDia = Clamp(generated.Targets.Calories, bounds.MinCalories, bounds.MaxCalories),
            ObjetivoSemanalKg = bounds.WeeklyGoalKg,
            Explicacao = Truncate(generated.Explanation, 4000),
            FonteGeracao = generated.FallbackUsed ? "Hibrido" : "IA",
            ModeloIaVersao = Truncate(generated.Model, 80)
        };

        var meta = new MetaNutricional
        {
            TenantId = command.TenantId,
            PlanoUsuario = plano,
            PlanoUsuarioId = plano.Id,
            ProteinaG = RoundMacro(generated.Targets.ProteinG),
            CarboidratoG = RoundMacro(generated.Targets.CarbsG),
            GorduraG = RoundMacro(generated.Targets.FatG),
            FibraG = 25,
            AguaMl = CalculateWaterTarget(perfil.PesoAtualKg)
        };

        plano.MetasNutricionais.Add(meta);
        _planoRepository.AdicionarPlano(plano);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        SugestaoRefeicaoResponse[] meals = generated.Meals
            .Select(meal => new SugestaoRefeicaoResponse(meal.Name, meal.Description, meal.Calories))
            .ToArray();

        string[] notices = generated.SafetyNotices
            .Select(notice => notice.Message)
            .Append("A IA orienta e educa, mas não substitui médicos, nutricionistas ou profissionais habilitados.")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Result<PlanoAlimentarGeradoResponse>.Success(PlanoAlimentarMapper.Map(
            plano,
            meta,
            meals,
            generated.Alternatives.ToArray(),
            notices));
    }

    private static AiPlanGenerationClientReply CreateHybridPlan(PerfilSaude perfil, PlanBounds bounds, IReadOnlyCollection<string> preferences, IReadOnlyCollection<string> restrictions)
    {
        int calories = (bounds.MinCalories + bounds.MaxCalories) / 2;
        decimal protein = bounds.ProteinTargetG;
        decimal fat = Math.Round(calories * 0.28m / 9m, 1);
        decimal carbs = Math.Max(0, Math.Round((calories - protein * 4m - fat * 9m) / 4m, 1));
        string restrictionsText = restrictions.Count == 0 ? "sem restrições informadas" : string.Join(", ", restrictions);
        string preferencesText = preferences.Count == 0 ? "suas preferências principais" : string.Join(", ", preferences.Take(5));

        return new AiPlanGenerationClientReply(
            new AiMacroTargets(calories, protein, carbs, fat),
            [
                new AiMealSuggestion("Café da manhã flexível", $"Opção com proteína leve, fruta e carboidrato simples, considerando {preferencesText}.", (int)Math.Round(calories * 0.25m)),
                new AiMealSuggestion("Almoço equilibrado", $"Base com legumes, proteína e carboidrato ajustado; considerar {restrictionsText}.", (int)Math.Round(calories * 0.35m)),
                new AiMealSuggestion("Jantar tranquilo", "Refeição com boa saciedade, sem rigidez e compatível com a rotina.", (int)Math.Round(calories * 0.30m)),
                new AiMealSuggestion("Lanche opcional", "Opção simples para fome entre refeições, sem obrigatoriedade.", (int)Math.Round(calories * 0.10m))
            ],
            $"Plano híbrido gerado para objetivo {perfil.Objetivo}. Podemos ajustar conforme rotina, fome, preferências e evolução.",
            ["Trocar fontes de carboidrato por equivalentes culturais.", "Ajustar horários sem tratar refeições como obrigação rígida."],
            [
                new AiSafetyNotice("Esta proposta não substitui nutricionista ou médico.", false),
                new AiSafetyNotice("Condições clínicas exigem acompanhamento profissional.", true)
            ],
            "equilibrafit-plan-hybrid-v1",
            true);
    }

    private static PlanBounds CalculateBounds(PerfilSaude perfil)
    {
        int age = HealthMetrics.CalculateAge(perfil.DataNascimento, DateOnly.FromDateTime(DateTime.UtcNow));
        decimal sexOffset = perfil.SexoBiologico switch
        {
            SexoBiologico.Masculino => 5m,
            SexoBiologico.Feminino => -161m,
            _ => -78m
        };

        decimal bmr = (10m * perfil.PesoAtualKg) + (6.25m * perfil.AlturaCm) - (5m * age) + sexOffset;
        decimal factor = perfil.NivelAtividade switch
        {
            NivelAtividade.Sedentario => 1.2m,
            NivelAtividade.Leve => 1.375m,
            NivelAtividade.Moderado => 1.55m,
            NivelAtividade.Intenso => 1.725m,
            NivelAtividade.MuitoIntenso => 1.9m,
            _ => 1.35m
        };

        int maintenance = (int)Math.Round(bmr * factor);
        int adjustment = perfil.Objetivo switch
        {
            ObjetivoSaude.EmagrecimentoSustentavel => -350,
            ObjetivoSaude.GanhoMassa => 250,
            _ => 0
        };

        int center = maintenance + adjustment;
        int minimum = perfil.SexoBiologico == SexoBiologico.Masculino ? 1500 : 1200;
        int minCalories = Math.Max(minimum, center - 150);
        int maxCalories = Math.Max(minCalories + 100, center + 150);
        decimal proteinFactor = perfil.Objetivo == ObjetivoSaude.GanhoMassa ? 1.8m : 1.6m;
        decimal proteinTarget = Math.Round(perfil.PesoAtualKg * proteinFactor, 1);
        decimal weeklyGoal = perfil.Objetivo switch
        {
            ObjetivoSaude.EmagrecimentoSustentavel => 0.5m,
            ObjetivoSaude.GanhoMassa => 0.25m,
            _ => 0m
        };

        return new PlanBounds(minCalories, maxCalories, proteinTarget, weeklyGoal);
    }

    private static string[] Merge(IReadOnlyCollection<string>? first, IReadOnlyCollection<string>? second)
    {
        return (first ?? [])
            .Concat(second ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Select(item => item.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(30)
            .ToArray();
    }

    private static int CalculateWaterTarget(decimal weightKg)
    {
        return (int)Math.Round(weightKg * 35m / 50m) * 50;
    }

    private static int Clamp(int value, int min, int max)
    {
        return Math.Min(Math.Max(value, min), max);
    }

    private static decimal RoundMacro(decimal value)
    {
        return Math.Round(Math.Max(0, value), 1);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed record PlanBounds(int MinCalories, int MaxCalories, decimal ProteinTargetG, decimal WeeklyGoalKg);
}
