using System.Text.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Features.AiCoach;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.TestarAdminIaEnsino;

/// <summary>
/// Handles AI teaching draft tests.
/// </summary>
public sealed class TestarAdminIaEnsinoCommandHandler : IRequestHandler<TestarAdminIaEnsinoCommand, Result<TestarAdminIaEnsinoResponse>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAiCoachClient _aiCoachClient;
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public TestarAdminIaEnsinoCommandHandler(IAiCoachClient aiCoachClient, IAdminRepository adminRepository, IUnitOfWork unitOfWork)
    {
        _aiCoachClient = aiCoachClient;
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<TestarAdminIaEnsinoResponse>> Handle(TestarAdminIaEnsinoCommand command, CancellationToken cancellationToken)
    {
        var instructionSet = new AiInstructionSet(
            "draft-admin",
            command.Request.InstrucoesCoach.Trim(),
            command.Request.RegrasAlimentacao.Trim(),
            command.Request.RegrasTreino.Trim(),
            command.Request.BaseConhecimento.Trim(),
            command.Request.ExemplosBoasRespostas.Trim());

        string contextJson = JsonSerializer.Serialize(new
        {
            modo = "teste_admin",
            instrucaoOperacionalRascunho = instructionSet.ToPromptFragment(),
            aviso = "Teste de rascunho. Não persistir conversa de usuário."
        }, JsonOptions);

        Result<AiCoachClientReply> aiReply = await _aiCoachClient.EnviarAsync(
            new AiCoachClientRequest(
                command.TenantId,
                command.AdminUsuarioId,
                Guid.NewGuid(),
                command.Request.Pergunta.Trim(),
                "admin-draft",
                contextJson),
            cancellationToken);

        if (aiReply.IsFailure)
        {
            return Result<TestarAdminIaEnsinoResponse>.Failure(aiReply.Errors);
        }

        Result safeResponse = CoachResponseGuard.EnsureSafe(aiReply.Value!.Conteudo);
        if (safeResponse.IsFailure)
        {
            _adminRepository.AdicionarAuditoria(new Auditoria
            {
                TenantId = command.TenantId,
                UsuarioId = command.AdminUsuarioId,
                Acao = "ia.resposta.bloqueada",
                Entidade = "OperacaoIA",
                MetadadosJson = JsonSerializer.Serialize(new { Origem = "TesteAdmin", Erros = safeResponse.Errors.Select(error => error.Code) }, JsonOptions)
            });
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<TestarAdminIaEnsinoResponse>.Success(new TestarAdminIaEnsinoResponse(
                "A resposta foi bloqueada pela validação de segurança. Podemos ajustar as instruções e testar novamente.",
                aiReply.Value.Modelo,
                true,
                safeResponse.Errors.Select(error => error.Message).ToArray()));
        }

        string model = aiReply.Value.Modelo;
        bool fallbackUsed = model.Equals("equilibrafit-coach-rules-v1", StringComparison.OrdinalIgnoreCase);
        string[] warnings =
        [
            "Teste executado com rascunho; nada foi publicado.",
            "A IA orienta e educa, mas não substitui profissionais habilitados."
        ];

        return Result<TestarAdminIaEnsinoResponse>.Success(new TestarAdminIaEnsinoResponse(
            aiReply.Value.Conteudo.Trim(),
            model,
            fallbackUsed,
            warnings));
    }
}
