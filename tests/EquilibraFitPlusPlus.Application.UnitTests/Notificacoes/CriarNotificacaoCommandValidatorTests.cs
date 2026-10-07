using EquilibraFitPlusPlus.Application.Features.Notificacoes.Commands.CriarNotificacao;
using EquilibraFitPlusPlus.Contracts.Notificacoes;

namespace EquilibraFitPlusPlus.Application.UnitTests.Notificacoes;

/// <summary>
/// Tests notification creation validation.
/// </summary>
public sealed class CriarNotificacaoCommandValidatorTests
{
    /// <summary>
    /// Ensures a supportive notification is accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptNotification_WhenDataIsValid()
    {
        var validator = new CriarNotificacaoCommandValidator();
        var request = new CriarNotificacaoRequest(Guid.NewGuid(), "Vamos adaptar", "Sem problemas. Podemos ajustar seu plano para hoje.");

        var result = validator.Validate(new CriarNotificacaoCommand(Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1", "tests", request));

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures notifications need a target user.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectNotification_WhenUserIsMissing()
    {
        var validator = new CriarNotificacaoCommandValidator();
        var request = new CriarNotificacaoRequest(Guid.Empty, "Titulo", "Mensagem acolhedora.");

        var result = validator.Validate(new CriarNotificacaoCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, request));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Ensures empty notification messages are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectNotification_WhenMessageIsEmpty()
    {
        var validator = new CriarNotificacaoCommandValidator();
        var request = new CriarNotificacaoRequest(Guid.NewGuid(), "Titulo", string.Empty);

        var result = validator.Validate(new CriarNotificacaoCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, request));

        Assert.False(result.IsValid);
    }
}
