using EquilibraFitPlusPlus.Application.Features.Premium.Commands.SalvarCupom;
using EquilibraFitPlusPlus.Contracts.Premium;

namespace EquilibraFitPlusPlus.Application.UnitTests.Premium;

/// <summary>
/// Tests coupon command validation.
/// </summary>
public sealed class SalvarCupomCommandValidatorTests
{
    /// <summary>
    /// Ensures a percentage coupon is accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptCoupon_WhenPercentageDiscountIsValid()
    {
        var validator = new SalvarCupomCommandValidator();
        var request = new SalvarCupomRequest("BEMVINDO20", 20, null, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), 100);

        var result = validator.Validate(new SalvarCupomCommand(Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1", "tests", request));

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures coupons cannot mix percentage and fixed discount.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectCoupon_WhenTwoDiscountTypesAreProvided()
    {
        var validator = new SalvarCupomCommandValidator();
        var request = new SalvarCupomRequest("DUPLO", 10, 15, null, null);

        var result = validator.Validate(new SalvarCupomCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, request));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Ensures coupons need exactly one discount type.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectCoupon_WhenDiscountIsMissing()
    {
        var validator = new SalvarCupomCommandValidator();
        var request = new SalvarCupomRequest("SEMVALOR", null, null, null, null);

        var result = validator.Validate(new SalvarCupomCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, request));

        Assert.False(result.IsValid);
    }
}
