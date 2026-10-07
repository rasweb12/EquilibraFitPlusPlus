using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using System.Globalization;
using System.Text;

namespace EquilibraFitPlusPlus.Application.Features.AiCoach;

/// <summary>
/// Validates AI Coach responses before they are shown to users.
/// </summary>
internal static class CoachResponseGuard
{
    private static readonly string[] ForbiddenTerms =
    [
        "você falhou",
        "você errou",
        "saiu da dieta",
        "saiu do plano",
        "estragou tudo",
        "alimento proibido",
        "comida proibida",
        "dieta estragada"
    ];

    private static readonly string[] MedicalRiskTerms =
    [
        "diagnóstico",
        "prescrevo",
        "substitui seu médico",
        "substitui médico",
        "substitui nutricionista"
    ];

    /// <summary>
    /// Ensures an AI Coach response is safe for the product philosophy.
    /// </summary>
    public static Result EnsureSafe(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Result.Failure(new Error("ia.coach_resposta_vazia", "Não foi possível gerar uma resposta útil agora. Podemos tentar novamente em instantes."));
        }

        string normalized = Normalize(content);

        if (ForbiddenTerms.Select(Normalize).Any(normalized.Contains))
        {
            return Result.Failure(new Error("ia.coach_linguagem_insegura", "A resposta da IA foi bloqueada por linguagem fora da filosofia do produto."));
        }

        if (MedicalRiskTerms.Select(Normalize).Any(normalized.Contains))
        {
            return Result.Failure(new Error("ia.coach_risco_clinico", "A resposta da IA foi bloqueada por risco de orientação clínica inadequada."));
        }

        return Result.Success();
    }

    private static string Normalize(string content)
    {
        string decomposed = content.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (char character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
