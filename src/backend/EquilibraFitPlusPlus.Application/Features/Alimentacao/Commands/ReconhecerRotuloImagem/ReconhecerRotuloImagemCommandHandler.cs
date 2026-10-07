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

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ReconhecerRotuloImagem;

/// <summary>
/// Handles nutrition label image recognition.
/// </summary>
public sealed class ReconhecerRotuloImagemCommandHandler : IRequestHandler<ReconhecerRotuloImagemCommand, Result<RotuloNutricionalReconhecidoResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAlimentacaoRepository _alimentacaoRepository;
    private readonly IAiCoachClient _aiCoachClient;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public ReconhecerRotuloImagemCommandHandler(
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
    public async Task<Result<RotuloNutricionalReconhecidoResponse>> Handle(ReconhecerRotuloImagemCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<RotuloNutricionalReconhecidoResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        Result<AiLabelRecognitionClientReply> aiResult = await _aiCoachClient.ReconhecerRotuloAsync(
            new AiLabelRecognitionClientRequest(command.Request.ImageBase64, command.Request.Contexto),
            cancellationToken);

        if (aiResult.IsFailure)
        {
            return Result<RotuloNutricionalReconhecidoResponse>.Failure(aiResult.Errors);
        }

        AiLabelRecognitionClientReply reply = aiResult.Value!;
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

        ItemAlimentarReconhecidoResponse? item = CreateItem(reply);

        return Result<RotuloNutricionalReconhecidoResponse>.Success(new RotuloNutricionalReconhecidoResponse(
            analise.Id,
            analise.ConfiancaMedia,
            reply.RequiresUserReview,
            reply.Model,
            reply.FallbackUsed,
            reply.Message,
            item));
    }

    private static ItemAlimentarReconhecidoResponse? CreateItem(AiLabelRecognitionClientReply reply)
    {
        if (reply.Calories is null && reply.ProteinG is null && reply.CarbsG is null && reply.FatG is null)
        {
            return null;
        }

        return new ItemAlimentarReconhecidoResponse(
            "Rotulo nutricional",
            1,
            string.IsNullOrWhiteSpace(reply.ServingSize) ? "porcao" : reply.ServingSize,
            reply.Calories ?? 0,
            reply.ProteinG ?? 0,
            reply.CarbsG ?? 0,
            reply.FatG ?? 0,
            ClampConfidence(reply.Confidence));
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
