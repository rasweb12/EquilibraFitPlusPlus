using EquilibraFitPlusPlus.Application.Features.Admin.Commands.RevogarSessoesUsuario;

namespace EquilibraFitPlusPlus.Application.UnitTests.Admin;

/// <summary>
/// Tests validation rules for administrative user session revocation.
/// </summary>
public sealed class RevogarSessoesUsuarioCommandValidatorTests
{
    /// <summary>
    /// Ensures a complete command is valid.
    /// </summary>
    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        var validator = new RevogarSessoesUsuarioCommandValidator();
        var command = new RevogarSessoesUsuarioCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "127.0.0.1", "tests");

        var result = validator.Validate(command);

        Assert.True(result.IsValid);
    }

    /// <summary>
    /// Ensures an empty target user id is rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        var validator = new RevogarSessoesUsuarioCommandValidator();
        var command = new RevogarSessoesUsuarioCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, null, null);

        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
