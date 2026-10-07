using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Abstractions.Alimentacao;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ReconhecerRefeicaoImagem;

/// <summary>
/// Handles meal image recognition.
/// </summary>
public sealed class ReconhecerRefeicaoImagemCommandHandler : IRequestHandler<ReconhecerRefeicaoImagemCommand, Result<RefeicaoImagemReconhecidaResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAlimentacaoRepository _alimentacaoRepository;
    private readonly IAiCoachClient _aiCoachClient;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ReconhecerRefeicaoImagemCommandHandler(
        IAlimentacaoRepository alimentacaoRepository,
        IAiCoachClient aiCoachClient,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _alimentacaoRepository = alimentacaoRepository;
        _aiCoachClient = aiCoachClient;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<RefeicaoImagemReconhecidaResponse>> Handle(ReconhecerRefeicaoImagemCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<RefeicaoImagemReconhecidaResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        string? mealContext = CreateMealContext(command.Request.TipoRefeicao, command.Request.Contexto);
        Result<AiMealRecognitionClientReply> aiResult = await _aiCoachClient.ReconhecerRefeicaoAsync(
            new AiMealRecognitionClientRequest(command.Request.ImageBase64, mealContext),
            cancellationToken);

        if (aiResult.IsFailure)
        {
            return Result<RefeicaoImagemReconhecidaResponse>.Failure(aiResult.Errors);
        }

        AiMealRecognitionClientReply reply = aiResult.Value!;
        var analise = new AnaliseRefeicaoImagem
        {
            TenantId = command.TenantId,
            UsuarioId = command.UsuarioId,
            ConfiancaMedia = ClampConfidence(reply.Confidence),
            ModeloVisao = Truncate(reply.Model, 80),
            Status = StatusAnaliseImagem.Pendente,
            ResultadoJson = JsonSerializer.Serialize(reply, JsonOptions)
        };

        _alimentacaoRepository.AdicionarAnalise(analise);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RefeicaoImagemReconhecidaResponse>.Success(new RefeicaoImagemReconhecidaResponse(
            analise.Id,
            analise.ConfiancaMedia,
            reply.RequiresUserReview,
            reply.Model,
            reply.FallbackUsed,
            reply.Message,
            reply.Items.Select(MapItem).ToArray()));
    }

    private static ItemAlimentarReconhecidoResponse MapItem(AiRecognizedFoodItem item)
    {
        return new ItemAlimentarReconhecidoResponse(
            item.Name,
            item.Portion,
            item.Unit,
            item.Calories,
            item.ProteinG,
            item.CarbsG,
            item.FatG,
            ClampConfidence(item.Confidence));
    }

    private static string? CreateMealContext(string? tipoRefeicao, string? contexto)
    {
        if (string.IsNullOrWhiteSpace(tipoRefeicao) && string.IsNullOrWhiteSpace(contexto))
        {
            return null;
        }

        return $"tipo={tipoRefeicao?.Trim() ?? "não informado"}; contexto={contexto?.Trim() ?? "não informado"}";
    }

    private static decimal ClampConfidence(decimal confidence)
    {
        return Math.Clamp(Math.Round(confidence, 2), 0m, 100m);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
