using EquilibraFitPlusPlus.Application.Features.Admin.Commands.SalvarFeatureFlag;
using EquilibraFitPlusPlus.Contracts.Admin;

namespace EquilibraFitPlusPlus.Application.UnitTests.Admin;

/// <summary>
/// Tests feature flag validation.
/// </summary>
public sealed class SalvarFeatureFlagCommandValidatorTests
{
    /// <summary>
    /// Ensures valid feature flag data is accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptFeatureFlag_WhenRequestIsValid()
    {
        var validator = new SalvarFeatureFlagCommandValidator();
        var request = new SalvarFeatureFlagRequest("coach.ai", true, "{\"limit\":20}");

        var result = validator.Validate(new SalvarFeatureFlagCommand(Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1", "tests", request));

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures malformed JSON configuration is rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectFeatureFlag_WhenConfigurationJsonIsInvalid()
    {
        var validator = new SalvarFeatureFlagCommandValidator();
        var request = new SalvarFeatureFlagRequest("coach.ai", true, "{invalid");

        var result = validator.Validate(new SalvarFeatureFlagCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, request));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Ensures unsafe keys are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectFeatureFlag_WhenKeyContainsUnsafeCharacters()
    {
        var validator = new SalvarFeatureFlagCommandValidator();
        var request = new SalvarFeatureFlagRequest("coach ai!", true, null);

        var result = validator.Validate(new SalvarFeatureFlagCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, request));

        Assert.False(result.IsValid);
    }
}
