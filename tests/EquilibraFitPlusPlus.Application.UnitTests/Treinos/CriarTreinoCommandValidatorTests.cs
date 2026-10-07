using EquilibraFitPlusPlus.Application.Features.Treinos.Commands.CriarTreino;
using EquilibraFitPlusPlus.Contracts.Treinos;

namespace EquilibraFitPlusPlus.Application.UnitTests.Treinos;

/// <summary>
/// Tests workout validation.
/// </summary>
public sealed class CriarTreinoCommandValidatorTests
{
    /// <summary>
    /// Ensures custom exercises require enough catalog data.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectWorkout_WhenCustomExerciseDataIsIncomplete()
    {
        var validator = new CriarTreinoCommandValidator();
        var request = new CriarTreinoRequest(
            "Treino A",
            "Condicionamento",
            3,
            [new TreinoExercicioRequest(null, "Agachamento", null, "Iniciante", null, null, 1, 3, "12", 60)]);

        var result = validator.Validate(new CriarTreinoCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Ensures duplicate exercise order is rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectWorkout_WhenExerciseOrderIsDuplicated()
    {
        var validator = new CriarTreinoCommandValidator();
        var exerciseId = Guid.NewGuid();
        var request = new CriarTreinoRequest(
            "Treino A",
            "Condicionamento",
            3,
            [
                new TreinoExercicioRequest(exerciseId, null, null, null, null, null, 1, 3, "12", 60),
                new TreinoExercicioRequest(exerciseId, null, null, null, null, null, 1, 3, "10", 60)
            ]);

        var result = validator.Validate(new CriarTreinoCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
    }
}
