using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Planos;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Planos.Commands.SubstituirPlanoRefeicao;

/// <summary>
/// Applies a previously generated alternative only after the user confirms it.
/// </summary>
public sealed class SubstituirPlanoRefeicaoCommandHandler : IRequestHandler<SubstituirPlanoRefeicaoCommand, Result>
{
    private readonly IPlanoAlimentarRepository _planoRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public SubstituirPlanoRefeicaoCommandHandler(IPlanoAlimentarRepository planoRepository, IUnitOfWork unitOfWork)
    {
        _planoRepository = planoRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result> Handle(SubstituirPlanoRefeicaoCommand command, CancellationToken cancellationToken)
    {
        var meal = await _planoRepository.ObterRefeicaoAtivaAsync(
            command.TenantId,
            command.UsuarioId,
            command.PlanoId,
            command.RefeicaoId,
            cancellationToken);

        if (meal is null)
        {
            return Result.Failure(new Error("planos.refeicao_nao_encontrada", "Não encontramos esta refeição no seu plano ativo."));
        }

        AiMealAlternative[] alternatives;
        try
        {
            alternatives = JsonSerializer.Deserialize<AiMealAlternative[]>(meal.AlternativasJson) ?? [];
        }
        catch (JsonException)
        {
            return Result.Failure(new Error("planos.substituicoes_indisponiveis", "As substituições desta refeição não estão disponíveis agora."));
        }

        if (command.Request.IndiceAlternativa >= alternatives.Length)
        {
            return Result.Failure(new Error("planos.substituicao_nao_encontrada", "A alternativa escolhida não está mais disponível."));
        }

        AiMealAlternative alternative = alternatives[command.Request.IndiceAlternativa];
        meal.Nome = Truncate(alternative.Name, 180);
        meal.Descricao = Truncate(alternative.Description, 1200);
        meal.Calorias = alternative.Calories ?? meal.Calorias;
        meal.AlternativasJson = "[]";
        meal.ReceitaRapidaJson = null;
        meal.AtualizadoEm = DateTimeOffset.UtcNow;
        meal.AtualizadoPor = command.UsuarioId;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
