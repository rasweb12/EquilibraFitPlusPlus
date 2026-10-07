using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Evolucao;
using EquilibraFitPlusPlus.Application.Abstractions.Onboarding;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Contracts.Evolucao;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Evolucao.Commands.SalvarPeso;

/// <summary>
/// Handles body weight progress log persistence.
/// </summary>
public sealed class SalvarPesoCommandHandler : IRequestHandler<SalvarPesoCommand, Result<RegistroEvolucaoResponse>>
{
    private readonly IEvolucaoRepository _evolucaoRepository;
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public SalvarPesoCommandHandler(
        IEvolucaoRepository evolucaoRepository,
        IOnboardingRepository onboardingRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _evolucaoRepository = evolucaoRepository;
        _onboardingRepository = onboardingRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<RegistroEvolucaoResponse>> Handle(SalvarPesoCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<RegistroEvolucaoResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        DateOnly data = command.Request.Data ?? DateOnly.FromDateTime(DateTime.UtcNow);
        RegistroEvolucao? registro = await _evolucaoRepository.ObterPorDataAsync(command.TenantId, command.UsuarioId, data, cancellationToken);
        if (registro is null)
        {
            registro = new RegistroEvolucao
            {
                TenantId = command.TenantId,
                UsuarioId = command.UsuarioId,
                Data = data
            };

            _evolucaoRepository.Adicionar(registro);
        }

        registro.PesoKg = command.Request.PesoKg;
        registro.PercentualGordura = command.Request.PercentualGordura;
        registro.PercentualMassaMagra = command.Request.PercentualMassaMagra;
        registro.Observacao = NormalizeOptionalText(command.Request.Observacao);

        PerfilSaude? perfil = await _onboardingRepository.ObterPorUsuarioAsync(command.TenantId, command.UsuarioId, cancellationToken);
        if (perfil is not null)
        {
            perfil.PesoAtualKg = command.Request.PesoKg;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<RegistroEvolucaoResponse>.Success(new RegistroEvolucaoResponse(
            registro.Id,
            registro.Data,
            registro.PesoKg,
            registro.PercentualGordura,
            registro.PercentualMassaMagra,
            registro.Observacao,
            "Peso registrado. Vamos observar a tendencia com calma, sem julgamentos."));
    }

    private static string? NormalizeOptionalText(string? value)
    {
        string? trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
