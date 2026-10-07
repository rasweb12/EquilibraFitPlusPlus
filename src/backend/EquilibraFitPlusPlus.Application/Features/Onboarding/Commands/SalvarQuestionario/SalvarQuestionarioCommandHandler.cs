using EquilibraFitPlusPlus.Application.Abstractions.Data;
using EquilibraFitPlusPlus.Application.Abstractions.Onboarding;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Application.Common.Health;
using EquilibraFitPlusPlus.Application.Common.Serialization;
using EquilibraFitPlusPlus.Contracts.Onboarding;
using EquilibraFitPlusPlus.Domain.Entities;
using EquilibraFitPlusPlus.Domain.Enums;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Onboarding.Commands.SalvarQuestionario;

/// <summary>
/// Handles questionnaire persistence.
/// </summary>
public sealed class SalvarQuestionarioCommandHandler : IRequestHandler<SalvarQuestionarioCommand, Result<QuestionarioResponse>>
{
    private readonly IOnboardingRepository _onboardingRepository;
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public SalvarQuestionarioCommandHandler(
        IOnboardingRepository onboardingRepository,
        IUsuarioRepository usuarioRepository,
        IUnitOfWork unitOfWork)
    {
        _onboardingRepository = onboardingRepository;
        _usuarioRepository = usuarioRepository;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<QuestionarioResponse>> Handle(SalvarQuestionarioCommand command, CancellationToken cancellationToken)
    {
        Usuario? usuario = await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken);
        if (usuario is null)
        {
            return Result<QuestionarioResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        SalvarQuestionarioRequest request = command.Request;
        var sexo = Enum.Parse<SexoBiologico>(request.SexoBiologico, ignoreCase: true);
        var objetivo = Enum.Parse<ObjetivoSaude>(request.Objetivo, ignoreCase: true);
        var nivelAtividade = Enum.Parse<NivelAtividade>(request.NivelAtividade, ignoreCase: true);

        PerfilSaude? perfil = await _onboardingRepository.ObterPorUsuarioAsync(command.TenantId, command.UsuarioId, cancellationToken);
        if (perfil is null)
        {
            perfil = new PerfilSaude
            {
                TenantId = command.TenantId,
                UsuarioId = command.UsuarioId
            };

            _onboardingRepository.Adicionar(perfil);
        }

        perfil.DataNascimento = request.DataNascimento;
        perfil.SexoBiologico = sexo;
        perfil.AlturaCm = request.AlturaCm;
        perfil.PesoAtualKg = request.PesoAtualKg;
        perfil.Objetivo = objetivo;
        perfil.NivelAtividade = nivelAtividade;
        perfil.DiasTreinoSemana = request.DiasTreinoSemana;
        perfil.PreferenciasJson = JsonStringCollection.Serialize(request.Preferencias);
        perfil.RestricoesJson = JsonStringCollection.Serialize(request.Restricoes);
        perfil.ObservacoesJson = JsonStringCollection.Serialize(request.Observacoes);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<QuestionarioResponse>.Success(Map(perfil));
    }

    internal static QuestionarioResponse Map(PerfilSaude perfil)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        int idade = HealthMetrics.CalculateAge(perfil.DataNascimento, today);
        decimal imc = HealthMetrics.CalculateImc(perfil.PesoAtualKg, perfil.AlturaCm);

        return new QuestionarioResponse(
            perfil.Id,
            perfil.DataNascimento,
            idade,
            perfil.SexoBiologico.ToString(),
            perfil.AlturaCm,
            perfil.PesoAtualKg,
            imc,
            HealthMetrics.ClassifyImc(imc),
            perfil.Objetivo.ToString(),
            perfil.NivelAtividade.ToString(),
            perfil.DiasTreinoSemana,
            JsonStringCollection.Deserialize(perfil.PreferenciasJson),
            JsonStringCollection.Deserialize(perfil.RestricoesJson),
            JsonStringCollection.Deserialize(perfil.ObservacoesJson),
            "Questionario salvo. Podemos ajustar seu plano com calma conforme sua rotina evolui.");
    }
}
