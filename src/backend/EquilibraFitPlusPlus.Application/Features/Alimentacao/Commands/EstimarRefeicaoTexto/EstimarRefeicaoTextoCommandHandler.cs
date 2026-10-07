using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using EquilibraFitPlusPlus.Application.Abstractions.AiCoach;
using EquilibraFitPlusPlus.Application.Abstractions.Users;
using EquilibraFitPlusPlus.Contracts.Alimentacao;
using EquilibraFitPlusPlus.Shared.Errors;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Alimentacao.Commands.EstimarRefeicaoTexto;

/// <summary>
/// Handles text meal estimation requests.
/// </summary>
public sealed class EstimarRefeicaoTextoCommandHandler : IRequestHandler<EstimarRefeicaoTextoCommand, Result<RefeicaoTextoEstimadaResponse>>
{
    private readonly IAiCoachClient _aiCoachClient;
    private readonly IUsuarioRepository _usuarioRepository;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public EstimarRefeicaoTextoCommandHandler(IAiCoachClient aiCoachClient, IUsuarioRepository usuarioRepository)
    {
        _aiCoachClient = aiCoachClient;
        _usuarioRepository = usuarioRepository;
    }

    /// <inheritdoc />
    public async Task<Result<RefeicaoTextoEstimadaResponse>> Handle(EstimarRefeicaoTextoCommand command, CancellationToken cancellationToken)
    {
        if (await _usuarioRepository.ObterAtivoAsync(command.TenantId, command.UsuarioId, cancellationToken) is null)
        {
            return Result<RefeicaoTextoEstimadaResponse>.Failure(new Error("usuario.nao_encontrado", "Não foi possível localizar seu usuário ativo."));
        }

        Result<AiMealTextEstimationClientReply> aiResult = await _aiCoachClient.EstimarRefeicaoTextoAsync(
            new AiMealTextEstimationClientRequest(command.Request.Descricao.Trim(), command.Request.TipoRefeicao?.Trim()),
            cancellationToken);

        AiMealTextEstimationClientReply reply = aiResult.IsSuccess
            ? aiResult.Value!
            : CreateHybridEstimate(command.Request.Descricao);

        ItemAlimentarResponse[] items = reply.Items
            .Select(item => new ItemAlimentarResponse(
                Guid.Empty,
                item.Name,
                item.Portion,
                item.Unit,
                item.Calories,
                item.ProteinG,
                item.CarbsG,
                item.FatG,
                "IA"))
            .ToArray();

        return Result<RefeicaoTextoEstimadaResponse>.Success(new RefeicaoTextoEstimadaResponse(
            items.Sum(item => item.Calorias),
            items.Sum(item => item.ProteinaG),
            items.Sum(item => item.CarboidratoG),
            items.Sum(item => item.GorduraG),
            items,
            reply.Model,
            reply.FallbackUsed,
            reply.Message));
    }

    private static AiMealTextEstimationClientReply CreateHybridEstimate(string description)
    {
        string normalized = NormalizeForSearch(description);
        var items = new List<AiRecognizedFoodItem>();

        AddKnownFood(
            items,
            normalized,
            singularName: "pão",
            pluralName: "pães",
            displayName: "Pão francês",
            defaultPortion: 1m,
            unit: "unidade",
            calories: 135m,
            proteinG: 4.5m,
            carbsG: 28m,
            fatG: 1.5m);

        AddKnownFood(
            items,
            normalized,
            singularName: "ovo",
            pluralName: "ovos",
            displayName: "Ovo",
            defaultPortion: 1m,
            unit: "unidade",
            calories: 78m,
            proteinG: 6.3m,
            carbsG: 0.6m,
            fatG: 5.3m);

        AddKnownFood(
            items,
            normalized,
            singularName: "banana",
            pluralName: "bananas",
            displayName: "Banana",
            defaultPortion: 1m,
            unit: "unidade",
            calories: 86m,
            proteinG: 1.1m,
            carbsG: 22m,
            fatG: 0.3m);

        AddKnownFood(
            items,
            normalized,
            singularName: "frango",
            pluralName: "frangos",
            displayName: "Frango grelhado",
            defaultPortion: 100m,
            unit: "g",
            calories: 165m,
            proteinG: 31m,
            carbsG: 0m,
            fatG: 3.6m);

        AddKnownFood(
            items,
            normalized,
            singularName: "arroz",
            pluralName: "arroz",
            displayName: "Arroz cozido",
            defaultPortion: 100m,
            unit: "g",
            calories: 128m,
            proteinG: 2.5m,
            carbsG: 28m,
            fatG: 0.2m);

        AddKnownFood(
            items,
            normalized,
            singularName: "feijão",
            pluralName: "feijões",
            displayName: "Feijão cozido",
            defaultPortion: 100m,
            unit: "g",
            calories: 76m,
            proteinG: 4.8m,
            carbsG: 13.6m,
            fatG: 0.5m);

        if (items.Count == 0)
        {
            items.Add(new AiRecognizedFoodItem(
                "Refeição descrita",
                1m,
                "porção",
                250m,
                12m,
                30m,
                8m,
                45m));
        }

        return new AiMealTextEstimationClientReply(
            items,
            "equilibrafit-meal-text-hybrid-v1",
            true,
            "Estimativa híbrida criada. Revise porções e itens antes de salvar.");
    }

    private static void AddKnownFood(
        ICollection<AiRecognizedFoodItem> items,
        string normalizedText,
        string singularName,
        string pluralName,
        string displayName,
        decimal defaultPortion,
        string unit,
        decimal calories,
        decimal proteinG,
        decimal carbsG,
        decimal fatG)
    {
        string normalizedSingular = NormalizeForSearch(singularName);
        string normalizedPlural = NormalizeForSearch(pluralName);
        if (!normalizedText.Contains(normalizedSingular, StringComparison.Ordinal)
            && !normalizedText.Contains(normalizedPlural, StringComparison.Ordinal))
        {
            return;
        }

        decimal quantity = DetectQuantity(normalizedText, normalizedSingular, normalizedPlural, defaultPortion);
        decimal multiplier = unit.Equals("g", StringComparison.OrdinalIgnoreCase)
            ? quantity / 100m
            : quantity;

        items.Add(new AiRecognizedFoodItem(
            displayName,
            quantity,
            unit,
            Math.Round(calories * multiplier, 1),
            Math.Round(proteinG * multiplier, 1),
            Math.Round(carbsG * multiplier, 1),
            Math.Round(fatG * multiplier, 1),
            65m));
    }

    private static decimal DetectQuantity(string normalizedText, string singularName, string pluralName, decimal defaultPortion)
    {
        string pattern = $@"(?<quantity>\d+(?:[\.,]\d+)?)\s*(?:de\s+)?(?:{Regex.Escape(singularName)}|{Regex.Escape(pluralName)})";
        Match numericMatch = Regex.Match(normalizedText, pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50));
        if (numericMatch.Success
            && decimal.TryParse(
                numericMatch.Groups["quantity"].Value.Replace(',', '.'),
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal numericQuantity)
            && numericQuantity > 0)
        {
            return numericQuantity;
        }

        if (ContainsQuantityWord(normalizedText, "dois", pluralName) || ContainsQuantityWord(normalizedText, "duas", pluralName))
        {
            return 2m;
        }

        if (ContainsQuantityWord(normalizedText, "tres", pluralName) || ContainsQuantityWord(normalizedText, "três", pluralName))
        {
            return 3m;
        }

        if (ContainsQuantityWord(normalizedText, "um", singularName) || ContainsQuantityWord(normalizedText, "uma", singularName))
        {
            return 1m;
        }

        return defaultPortion;
    }

    private static bool ContainsQuantityWord(string normalizedText, string quantityWord, string foodName)
    {
        string normalizedQuantity = NormalizeForSearch(quantityWord);
        string normalizedFood = NormalizeForSearch(foodName);
        return normalizedText.Contains($"{normalizedQuantity} {normalizedFood}", StringComparison.Ordinal);
    }

    private static string NormalizeForSearch(string value)
    {
        string formD = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);

        foreach (char character in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
