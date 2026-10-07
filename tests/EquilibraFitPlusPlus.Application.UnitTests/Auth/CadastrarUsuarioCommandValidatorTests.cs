using EquilibraFitPlusPlus.Application.Features.Auth.Commands.CadastrarUsuario;
using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Application.UnitTests.Auth;

/// <summary>
/// Tests registration command validation rules.
/// </summary>
public sealed class CadastrarUsuarioCommandValidatorTests
{
    /// <summary>
    /// Ensures mandatory LGPD consent is required during registration.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectRegistration_WhenPrivacyConsentIsMissing()
    {
        var validator = new CadastrarUsuarioCommandValidator();
        var request = new CadastrarUsuarioRequest(
            "Mariana Silva",
            "mariana@email.com",
            "SenhaForte123",
            [new ConsentimentoRequest(TipoConsentimento.TermosUso.ToString(), "1.0")]);

        var result = validator.Validate(new CadastrarUsuarioCommand(request, "127.0.0.1"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage.Contains("privacidade", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Ensures weak passwords are blocked before reaching infrastructure.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectRegistration_WhenPasswordIsWeak()
    {
        var validator = new CadastrarUsuarioCommandValidator();
        var request = new CadastrarUsuarioRequest(
            "Mariana Silva",
            "mariana@email.com",
            "senhafraca",
            [
                new ConsentimentoRequest(TipoConsentimento.TermosUso.ToString(), "1.0"),
                new ConsentimentoRequest(TipoConsentimento.Privacidade.ToString(), "1.0")
            ]);

        var result = validator.Validate(new CadastrarUsuarioCommand(request, "127.0.0.1"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith(nameof(CadastrarUsuarioRequest.Senha), StringComparison.Ordinal));
    }

    /// <summary>
    /// Ensures a complete request satisfies application-level validation.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptRegistration_WhenRequiredDataIsValid()
    {
        var validator = new CadastrarUsuarioCommandValidator();
        var request = new CadastrarUsuarioRequest(
            "Mariana Silva",
            "mariana@email.com",
            "SenhaForte123",
            [
                new ConsentimentoRequest(TipoConsentimento.TermosUso.ToString(), "1.0"),
                new ConsentimentoRequest(TipoConsentimento.Privacidade.ToString(), "1.0")
            ]);

        var result = validator.Validate(new CadastrarUsuarioCommand(request, "127.0.0.1"));

        Assert.True(result.IsValid);
    }
}
