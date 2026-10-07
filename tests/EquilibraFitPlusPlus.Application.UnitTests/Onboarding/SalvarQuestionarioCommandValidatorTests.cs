using EquilibraFitPlusPlus.Application.Features.Onboarding.Commands.SalvarQuestionario;
using EquilibraFitPlusPlus.Contracts.Onboarding;

namespace EquilibraFitPlusPlus.Application.UnitTests.Onboarding;

/// <summary>
/// Tests questionnaire validation.
/// </summary>
public sealed class SalvarQuestionarioCommandValidatorTests
{
    /// <summary>
    /// Ensures users below minimum age are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectQuestionnaire_WhenUserIsBelowMinimumAge()
    {
        var validator = new SalvarQuestionarioCommandValidator();
        var request = CreateValidRequest() with { DataNascimento = DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-10)) };

        var result = validator.Validate(new SalvarQuestionarioCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName.EndsWith(nameof(SalvarQuestionarioRequest.DataNascimento), StringComparison.Ordinal));
    }

    /// <summary>
    /// Ensures a complete questionnaire is accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptQuestionnaire_WhenDataIsValid()
    {
        var validator = new SalvarQuestionarioCommandValidator();

        var result = validator.Validate(new SalvarQuestionarioCommand(Guid.NewGuid(), Guid.NewGuid(), CreateValidRequest()));

        Assert.True(result.IsValid);
    }

    private static SalvarQuestionarioRequest CreateValidRequest()
    {
        return new SalvarQuestionarioRequest(
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30)),
            "Feminino",
            165,
            68,
            "EmagrecimentoSustentavel",
            "Moderado",
            3,
            ["Comida brasileira"],
            ["Lactose"],
            ["Rotina corrida"]);
    }
}
