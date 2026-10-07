using EquilibraFitPlusPlus.Application.Features.Marketplace.Commands.SalvarParceiro;
using EquilibraFitPlusPlus.Contracts.Marketplace;

namespace EquilibraFitPlusPlus.Application.UnitTests.Marketplace;

/// <summary>
/// Tests marketplace partner validation.
/// </summary>
public sealed class SalvarParceiroCommandValidatorTests
{
    /// <summary>
    /// Ensures partner data is accepted when it uses known enums.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptPartner_WhenDataIsValid()
    {
        var validator = new SalvarParceiroCommandValidator();
        var request = new SalvarParceiroRequest(null, "Academia Equilibrio", "Academia", "Aprovado", "contato@academia.com", "12345678000100");

        var result = validator.Validate(new SalvarParceiroCommand(Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1", "tests", request));

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures unknown partner types are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectPartner_WhenTypeIsUnknown()
    {
        var validator = new SalvarParceiroCommandValidator();
        var request = new SalvarParceiroRequest(null, "Parceiro", "TipoLivre", "Aprovado", null, null);

        var result = validator.Validate(new SalvarParceiroCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, request));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Ensures invalid emails are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectPartner_WhenEmailIsInvalid()
    {
        var validator = new SalvarParceiroCommandValidator();
        var request = new SalvarParceiroRequest(null, "Parceiro", "Academia", "Aprovado", "email-invalido", null);

        var result = validator.Validate(new SalvarParceiroCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, request));

        Assert.False(result.IsValid);
    }
}
