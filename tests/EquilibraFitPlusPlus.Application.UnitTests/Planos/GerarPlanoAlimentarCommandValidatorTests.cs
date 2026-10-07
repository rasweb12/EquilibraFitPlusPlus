using EquilibraFitPlusPlus.Application.Features.Planos.Commands.GerarPlanoAlimentar;
using EquilibraFitPlusPlus.Contracts.Planos;

namespace EquilibraFitPlusPlus.Application.UnitTests.Planos;

/// <summary>
/// Tests generated diet plan validation.
/// </summary>
public sealed class GerarPlanoAlimentarCommandValidatorTests
{
    /// <summary>
    /// Ensures reasonable optional inputs are accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptOptionalInputs_WhenWithinLimits()
    {
        var validator = new GerarPlanoAlimentarCommandValidator();
        var request = new GerarPlanoAlimentarRequest("trabalho em horario comercial", ["arroz"], ["lactose"]);

        var result = validator.Validate(new GerarPlanoAlimentarCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures excessive preferences are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectRequest_WhenPreferenceListIsTooLarge()
    {
        var validator = new GerarPlanoAlimentarCommandValidator();
        var request = new GerarPlanoAlimentarRequest(null, Enumerable.Range(1, 31).Select(item => $"preferencia {item}").ToArray(), null);

        var result = validator.Validate(new GerarPlanoAlimentarCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
    }
}
