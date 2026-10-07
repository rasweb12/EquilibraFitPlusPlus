using System.Text.Json;
using FluentValidation;

namespace EquilibraFitPlusPlus.Application.Features.Admin.Commands.SalvarFeatureFlag;

/// <summary>
/// Validates feature flag changes.
/// </summary>
public sealed class SalvarFeatureFlagCommandValidator : AbstractValidator<SalvarFeatureFlagCommand>
{
    /// <summary>
    /// Initializes validation rules.
    /// </summary>
    public SalvarFeatureFlagCommandValidator()
    {
        RuleFor(command => command.TenantId).NotEmpty();
        RuleFor(command => command.AdminUsuarioId).NotEmpty();
        RuleFor(command => command.Request.Chave)
            .NotEmpty()
            .MaximumLength(120)
            .Matches("^[A-Za-z0-9][A-Za-z0-9._:-]{2,119}$")
            .WithMessage("A chave da feature flag deve ter letras, numeros, ponto, hifen, underline ou dois-pontos.");
        RuleFor(command => command.Request.ConfiguracaoJson)
            .MaximumLength(4000)
            .Must(BeEmptyOrValidJson)
            .WithMessage("ConfiguracaoJson deve ser um JSON válido quando informada.");
    }

    private static bool BeEmptyOrValidJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(value);
            return document.RootElement.ValueKind is JsonValueKind.Object or JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
