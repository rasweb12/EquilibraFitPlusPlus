using EquilibraFitPlusPlus.Application.Features.AiCoach.Commands.EnviarMensagemCoach;
using EquilibraFitPlusPlus.Contracts.AiCoach;

namespace EquilibraFitPlusPlus.Application.UnitTests.AiCoach;

/// <summary>
/// Tests AI Coach message validation.
/// </summary>
public sealed class EnviarMensagemCoachCommandValidatorTests
{
    /// <summary>
    /// Ensures a friendly, bounded user message is accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptCoachMessage_WhenRequestIsValid()
    {
        var validator = new EnviarMensagemCoachCommandValidator();
        var request = new EnviarMensagemCoachRequest(null, "Hoje nao consegui treinar. Como posso ajustar?");

        var result = validator.Validate(new EnviarMensagemCoachCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures empty messages are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectCoachMessage_WhenMessageIsEmpty()
    {
        var validator = new EnviarMensagemCoachCommandValidator();
        var request = new EnviarMensagemCoachRequest(null, string.Empty);

        var result = validator.Validate(new EnviarMensagemCoachCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Ensures very large messages are rejected before reaching the AI service.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectCoachMessage_WhenMessageExceedsLimit()
    {
        var validator = new EnviarMensagemCoachCommandValidator();
        var request = new EnviarMensagemCoachRequest(null, new string('a', 2001));

        var result = validator.Validate(new EnviarMensagemCoachCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
    }
}
