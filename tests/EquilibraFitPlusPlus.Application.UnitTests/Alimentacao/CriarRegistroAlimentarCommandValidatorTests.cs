using EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.CriarRegistroAlimentar;
using EquilibraFitPlusPlus.Contracts.Alimentacao;

namespace EquilibraFitPlusPlus.Application.UnitTests.Alimentacao;

/// <summary>
/// Tests food log validation.
/// </summary>
public sealed class CriarRegistroAlimentarCommandValidatorTests
{
    /// <summary>
    /// Ensures a food log without items is rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectFoodLog_WhenItemsAreMissing()
    {
        var validator = new CriarRegistroAlimentarCommandValidator();
        var request = new CriarRegistroAlimentarRequest(DateTimeOffset.UtcNow, "Almoco", []);

        var result = validator.Validate(new CriarRegistroAlimentarCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Ensures a realistic food log is accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptFoodLog_WhenDataIsValid()
    {
        var validator = new CriarRegistroAlimentarCommandValidator();
        var request = new CriarRegistroAlimentarRequest(
            DateTimeOffset.UtcNow,
            "Almoco",
            [new ItemAlimentarRequest("Arroz", 100, "g", 130, 2.5m, 28, 0.3m, "TACO")]);

        var result = validator.Validate(new CriarRegistroAlimentarCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.True(result.IsValid);
    }
}
