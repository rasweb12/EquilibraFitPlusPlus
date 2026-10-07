using EquilibraFitPlusPlus.Application.Features.Habitos.Commands.SalvarHabitosDiarios;
using EquilibraFitPlusPlus.Contracts.Habitos;

namespace EquilibraFitPlusPlus.Application.UnitTests.Habitos;

/// <summary>
/// Tests daily habits validation.
/// </summary>
public sealed class SalvarHabitosDiariosCommandValidatorTests
{
    /// <summary>
    /// Ensures realistic daily habits are accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptDailyHabits_WhenDataIsValid()
    {
        var validator = new SalvarHabitosDiariosCommandValidator();
        var request = new SalvarHabitosDiariosRequest(DateOnly.FromDateTime(DateTime.UtcNow), 1800, 2700, 7.5m, 8m, 4, true, false);

        var result = validator.Validate(new SalvarHabitosDiariosCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures unsafe habit values are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectDailyHabits_WhenValuesAreUnsafe()
    {
        var validator = new SalvarHabitosDiariosCommandValidator();
        var request = new SalvarHabitosDiariosRequest(DateOnly.FromDateTime(DateTime.UtcNow.AddDays(3)), 20000, 0, 30m, 0m, 8, false, false);

        var result = validator.Validate(new SalvarHabitosDiariosCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
    }
}
