using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.GerarTreinoIa;
using EquilibraFitPlusPlus.Contracts.Treinos;

namespace EquilibraFitPlusPlus.Application.UnitTests.Treinos;

/// <summary>
/// Tests AI workout generation validation.
/// </summary>
public sealed class GerarTreinoIaCommandValidatorTests
{
    /// <summary>
    /// Ensures reasonable optional inputs are accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptOptionalInputs_WhenWithinLimits()
    {
        var validator = new GerarTreinoIaCommandValidator();
        var request = new GerarTreinoIaRequest("iniciante", ["joelho sensivel"], ["halteres"]);

        var result = validator.Validate(new GerarTreinoIaCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures excessive equipment entries are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectRequest_WhenEquipmentListIsTooLarge()
    {
        var validator = new GerarTreinoIaCommandValidator();
        var request = new GerarTreinoIaRequest(null, null, Enumerable.Range(1, 21).Select(item => $"equipamento {item}").ToArray());

        var result = validator.Validate(new GerarTreinoIaCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
    }
}
