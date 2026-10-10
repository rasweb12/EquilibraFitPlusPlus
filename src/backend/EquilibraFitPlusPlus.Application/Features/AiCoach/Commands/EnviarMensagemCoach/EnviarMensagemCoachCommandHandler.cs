using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EquilibraFitPlusPlus.Application.Abstractions.AiContext;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Application.Features.AiCoach.Mappings;
using EquilibraFitPlusPlus.Contracts.AiCoach;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach.Commands.EnviarMensagemCoach;

/// <summary>
/// Handles AI Coach message processing.
/// </summary>
public sealed class EnviarMensagemCoachCommandHandler : IRequestHandler<EnviarMensagemCoachCommand, Result<CoachReplyResponse>>
{
    private const string HealthDisclaimer = CoachResponseGuard.HealthDisclaimer;
    private const string SystemPromptVersion = "coach-safety-v1";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly IAiCoachRepository _aiCoachRepository;
    private readonly IAiUserContextBuilder _aiUserContextBuilder;
    private readonly IAiCoachClient _aiCoachClient;
    private readonly IAiInstructionRepository _aiInstructionRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public EnviarMensagemCoachCommandHandler(
        IAiCoachRepository aiCoachRepository,
        IAiUserContextBuilder aiUserContextBuilder,
        IAiCoachClient aiCoachClient,
        IAiInstructionRepository aiInstructionRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _aiCoachRepository = aiCoachRepository;
        _aiUserContextBuilder = aiUserContextBuilder;
        _aiCoachClient = aiCoachClient;
        _aiInstructionRepository = aiInstructionRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<CoachReplyResponse>> Handle(EnviarMensagemCoachCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<CoachReplyResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        ChatSession? session = command.Request.SessaoId.HasValue
            ? await _aiCoachRepository.ObterSessaoAsync(command.TenantId, command.UsuarioId, command.Request.SessaoId.Value, cancellationToken)
            : null;

        if (command.Request.SessaoId.HasValue && session is null)
        {
            return Result<CoachReplyResponse>.Failure(new Error("ia.sessao_nao_encontrada", "Não encontramos essa conversa do Coach IA."));
        }

        session ??= CreateSession(command.TenantId, command.UsuarioId, command.Request.Mensagem);

        CoachAiContext context = await _aiUserContextBuilder.BuildCoachContextAsync(command.TenantId, command.UsuarioId, cancellationToken);
        AiInstructionSet? instructions = await _aiInstructionRepository.ObterInstrucoesPublicadasAsync(command.TenantId, cancellationToken);
        string contextJson = JsonSerializer.Serialize(CreateSafeContext(context, instructions), JsonOptions);

        Result<AiCoachClientReply> coachReply = await _aiCoachClient.EnviarAsync(
            new AiCoachClientRequest(command.TenantId, command.UsuarioId, session.Id, command.Request.Mensagem.Trim(), SystemPromptVersion, contextJson, command.Request.Provedor),
            cancellationToken);

        if (coachReply.IsFailure && coachReply.Errors.Any(AiServiceErrors.IsInfrastructureError))
        {
            return Result<CoachReplyResponse>.Failure(coachReply.Errors);
        }

        AiCoachClientReply reply = coachReply.IsSuccess
            ? coachReply.Value!
            : CreateHybridCoachReply(command.Request.Mensagem, context);

        Result safeResponse = CoachResponseGuard.EnsureSafe(reply.Conteudo);
        if (safeResponse.IsFailure)
        {
            return Result<CoachReplyResponse>.Failure(safeResponse.Errors);
        }

        var userMessage = new ChatMessage
        {
            TenantId = command.TenantId,
            ChatSessionId = session.Id,
            Role = "user",
            Conteudo = command.Request.Mensagem.Trim()
        };

        var assistantMessage = new ChatMessage
        {
            TenantId = command.TenantId,
            ChatSessionId = session.Id,
            Role = "assistant",
            Conteudo = reply.Conteudo.Trim(),
            ModeloIa = reply.Modelo
        };

        session.Mensagens.Add(userMessage);
        session.Mensagens.Add(assistantMessage);

        if (command.Request.SessaoId is null)
        {
            _aiCoachRepository.AdicionarSessao(session);
        }
        else
        {
            // Client-assigned message IDs must be inserted, not inferred as existing rows.
            _aiCoachRepository.AdicionarMensagem(userMessage);
            _aiCoachRepository.AdicionarMensagem(assistantMessage);
        }

        await UpsertExplicitMemoriesAsync(command.TenantId, command.UsuarioId, command.Request.Mensagem, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<CoachReplyResponse>.Success(new CoachReplyResponse(
            session.Id,
            CoachSessionMapper.MapMessage(userMessage),
            CoachSessionMapper.MapMessage(assistantMessage),
            HealthDisclaimer,
            reply.FallbackUsed));
    }

    private static ChatSession CreateSession(Guid tenantId, Guid usuarioId, string firstMessage)
    {
        string title = firstMessage.Trim();
        if (title.Length > 80)
        {
            title = string.Concat(title.AsSpan(0, 77), "...");
        }

        return new ChatSession
        {
            TenantId = tenantId,
            UsuarioId = usuarioId,
            Titulo = string.IsNullOrWhiteSpace(title) ? "Conversa com Coach IA" : title
        };
    }

    private static AiCoachClientReply CreateHybridCoachReply(string message, CoachAiContext context)
    {
        string normalizedMessage = message.Trim().ToLowerInvariant();
        int recentMeals = context.AlimentacaoRecente.Count;
        decimal recentCalories = context.AlimentacaoRecente.Sum(registro => registro.CaloriasTotal);
        decimal recentProtein = context.AlimentacaoRecente.Sum(registro => registro.ProteinaTotalG);

        string weightPart = CreateWeightPart(context);

        string planPart = context.PlanoAlimentarAtual is null
            ? "Ainda não há plano alimentar ativo; podemos gerar um plano flexível a partir do questionário."
            : $"Seu plano ativo está em {context.PlanoAlimentarAtual.CaloriasDia} kcal/dia, com meta semanal de {context.PlanoAlimentarAtual.ObjetivoSemanalKg:0.##} kg.";

        string foodPart = recentMeals == 0
            ? "Hoje eu ainda não encontrei refeições recentes registradas."
            : $"Nos registros recentes, encontrei {recentMeals} refeição(ões), cerca de {recentCalories:0} kcal e {recentProtein:0.#} g de proteína.";

        string action = normalizedMessage.Contains("treino", StringComparison.Ordinal)
            ? "Para hoje, priorize consistência: escolha um treino do plano, marque os exercícios concluídos e reduza intensidade se sono, dor ou cansaço estiverem ruins."
            : normalizedMessage.Contains("peso", StringComparison.Ordinal) || normalizedMessage.Contains("evolução", StringComparison.Ordinal)
                ? "O próximo passo mais útil é registrar o peso em condições parecidas com as anteriores e observar a tendência, não um único dia isolado."
                : normalizedMessage.Contains("água", StringComparison.Ordinal) || normalizedMessage.Contains("hidratação", StringComparison.Ordinal)
                    ? "Uma ação simples agora é registrar água em copos pequenos ao longo do dia, sem tentar compensar tudo de uma vez."
                    : "Ajuste uma coisa pequena agora: registre a próxima refeição, inclua uma fonte de proteína se fizer sentido e mantenha água por perto.";

        string content = string.Join(
            Environment.NewLine + Environment.NewLine,
            "Sem problemas. Estou usando o modo híbrido enquanto a IA externa não responde.",
            foodPart,
            planPart,
            weightPart,
            action,
            HealthDisclaimer);

        return new AiCoachClientReply(content, "equilibrafit-coach-hybrid-v1", FallbackUsed: true);
    }

    private static string CreateWeightPart(CoachAiContext context)
    {
        if (context.Evolucao?.PesoAtualKg is null)
        {
            return "Ainda não encontrei um peso recente registrado.";
        }

        return context.Evolucao.UltimoRegistroEm.HasValue
            ? $"Seu último peso registrado foi {context.Evolucao.PesoAtualKg:0.#} kg em {context.Evolucao.UltimoRegistroEm.Value:dd/MM/yyyy}."
            : $"Seu peso atual registrado é {context.Evolucao.PesoAtualKg:0.#} kg.";
    }

    private async Task UpsertExplicitMemoriesAsync(Guid tenantId, Guid usuarioId, string message, CancellationToken cancellationToken)
    {
        foreach (AiCoachMemory memory in ExtractExplicitMemories(tenantId, usuarioId, message))
        {
            AiCoachMemory? existing = await _aiCoachRepository.ObterMemoriaAsync(tenantId, usuarioId, memory.Chave, cancellationToken);
            if (existing is null)
            {
                _aiCoachRepository.AdicionarMemoria(memory);
                continue;
            }

            existing.Categoria = memory.Categoria;
            existing.Valor = memory.Valor;
            existing.TipoFato = AiCoachMemoryFactTypes.UserPreference;
            existing.ConfirmadoPeloUsuario = true;
            existing.Fonte = "MensagemUsuario";
            existing.ConfirmadoEm = DateTimeOffset.UtcNow;
        }
    }

    private static IReadOnlyCollection<AiCoachMemory> ExtractExplicitMemories(Guid tenantId, Guid usuarioId, string message)
    {
        string normalized = RemoveDiacritics(message).ToLowerInvariant();
        var memories = new Dictionary<string, AiCoachMemory>(StringComparer.OrdinalIgnoreCase);

        AddWhen(memories, tenantId, usuarioId, normalized.Contains("prefiro maquinas", StringComparison.Ordinal) || normalized.Contains("gosto de maquinas", StringComparison.Ordinal), "PreferenciaEquipamento", "preferencia_maquinas", "maquinas");
        AddWhen(memories, tenantId, usuarioId, normalized.Contains("nao gosto de corrida", StringComparison.Ordinal) || normalized.Contains("odeio corrida", StringComparison.Ordinal) || normalized.Contains("evito corrida", StringComparison.Ordinal), "PreferenciaAtividade", "evita_corrida", "nao gosta de corrida");
        AddWhen(memories, tenantId, usuarioId, normalized.Contains("treino curto", StringComparison.Ordinal) || normalized.Contains("treinos curtos", StringComparison.Ordinal) || normalized.Contains("prefiro treinar pouco", StringComparison.Ordinal), "PreferenciaRotina", "preferencia_treino_curto", "treino curto");
        AddWhen(memories, tenantId, usuarioId, ContainsAny(normalized, "priorizar bracos", "prioridade bracos", "focar bracos"), "PrioridadeTreino", "prioridade_bracos", "bracos");
        AddWhen(memories, tenantId, usuarioId, ContainsAny(normalized, "priorizar costas", "prioridade costas", "focar costas"), "PrioridadeTreino", "prioridade_costas", "costas");
        AddWhen(memories, tenantId, usuarioId, ContainsAny(normalized, "priorizar gluteos", "prioridade gluteos", "focar gluteos"), "PrioridadeTreino", "prioridade_gluteos", "gluteos");
        AddWhen(memories, tenantId, usuarioId, ContainsAny(normalized, "gosto de supino", "prefiro supino"), "PreferenciaExercicio", "gosta_supino", "supino");
        AddWhen(memories, tenantId, usuarioId, ContainsAny(normalized, "gosto de agachamento", "prefiro agachamento"), "PreferenciaExercicio", "gosta_agachamento", "agachamento");
        AddWhen(memories, tenantId, usuarioId, ContainsAny(normalized, "treinar de manha", "prefiro de manha", "prefiro treinar de manha"), "PreferenciaHorario", "preferencia_horario_manha", "manha");
        AddWhen(memories, tenantId, usuarioId, ContainsAny(normalized, "treinar a noite", "prefiro a noite", "prefiro treinar a noite"), "PreferenciaHorario", "preferencia_horario_noite", "noite");

        return memories.Values.ToArray();
    }

    private static void AddWhen(
        IDictionary<string, AiCoachMemory> memories,
        Guid tenantId,
        Guid usuarioId,
        bool condition,
        string category,
        string key,
        string value)
    {
        if (!condition)
        {
            return;
        }

        memories[key] = new AiCoachMemory
        {
            TenantId = tenantId,
            UsuarioId = usuarioId,
            Categoria = category,
            Chave = key,
            Valor = value,
            TipoFato = AiCoachMemoryFactTypes.UserPreference,
            ConfirmadoPeloUsuario = true,
            Fonte = "MensagemUsuario",
            ConfirmadoEm = DateTimeOffset.UtcNow
        };
    }

    private static bool ContainsAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.Ordinal));
    }

    private static string RemoveDiacritics(string value)
    {
        string normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (char character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static object CreateSafeContext(CoachAiContext context, AiInstructionSet? instructions)
    {
        return new
        {
            operacaoIa = instructions is null
                ? null
                : new
                {
                    instructions.Versao,
                    Orientacao = instructions.ToPromptFragment()
                },
            perfil = context.Perfil,
            objetivos = context.Objetivos,
            rotina = context.Rotina,
            treinoAtual = context.TreinoAtual,
            segurancaTreino = context.SegurancaTreino,
            plano = context.PlanoAlimentarAtual,
            ultimaEvolucao = context.Evolucao,
            recuperacao = context.Recuperacao,
            alimentacaoRecente = context.AlimentacaoRecente,
            memoriasConfirmadas = context.MemoriasConfirmadas
        };
    }
}
