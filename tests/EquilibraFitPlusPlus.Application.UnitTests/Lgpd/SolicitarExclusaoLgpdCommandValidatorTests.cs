using EquilibraFitPlusPlus.Application.Features.Lgpd.Commands.SolicitarExclusaoLgpd;
using EquilibraFitPlusPlus.Contracts.Lgpd;

namespace EquilibraFitPlusPlus.Application.UnitTests.Lgpd;

/// <summary>
/// Tests LGPD deletion request validation.
/// </summary>
public sealed class SolicitarExclusaoLgpdCommandValidatorTests
{
    /// <summary>
    /// Ensures an explicit confirmation is accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptDeletionRequest_WhenConfirmationIsExplicit()
    {
        var validator = new SolicitarExclusaoLgpdCommandValidator();
        var request = new SolicitarExclusaoLgpdRequest("EXCLUIR");

        var result = validator.Validate(new SolicitarExclusaoLgpdCommand(Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1", "tests", request));

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures non-explicit confirmations are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectDeletionRequest_WhenConfirmationIsNotExplicit()
    {
        var validator = new SolicitarExclusaoLgpdCommandValidator();
        var request = new SolicitarExclusaoLgpdRequest("sim");

        var result = validator.Validate(new SolicitarExclusaoLgpdCommand(Guid.NewGuid(), Guid.NewGuid(), null, null, request));

        Assert.False(result.IsValid);
    }
}
