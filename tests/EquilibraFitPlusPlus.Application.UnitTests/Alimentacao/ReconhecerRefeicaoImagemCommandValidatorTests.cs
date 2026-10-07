using EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.ReconhecerRefeicaoImagem;
using EquilibraFitPlusPlus.Contracts.Alimentacao;

namespace EquilibraFitPlusPlus.Application.UnitTests.Alimentacao;

/// <summary>
/// Tests meal image recognition validation.
/// </summary>
public sealed class ReconhecerRefeicaoImagemCommandValidatorTests
{
    /// <summary>
    /// Ensures invalid Base64 images are rejected.
    /// </summary>
    [Fact]
    public void Validate_ShouldRejectImage_WhenBase64IsInvalid()
    {
        var validator = new ReconhecerRefeicaoImagemCommandValidator();
        var request = new ReconhecerRefeicaoImagemRequest("imagem-invalida", "Almoco", null);

        var result = validator.Validate(new ReconhecerRefeicaoImagemCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.False(result.IsValid);
    }

    /// <summary>
    /// Ensures a valid Base64 image payload is accepted.
    /// </summary>
    [Fact]
    public void Validate_ShouldAcceptImage_WhenBase64IsValid()
    {
        var validator = new ReconhecerRefeicaoImagemCommandValidator();
        string image = Convert.ToBase64String([1, 2, 3, 4]);
        var request = new ReconhecerRefeicaoImagemRequest(image, "Almoco", "prato caseiro");

        var result = validator.Validate(new ReconhecerRefeicaoImagemCommand(Guid.NewGuid(), Guid.NewGuid(), request));

        Assert.True(result.IsValid);
    }
}
