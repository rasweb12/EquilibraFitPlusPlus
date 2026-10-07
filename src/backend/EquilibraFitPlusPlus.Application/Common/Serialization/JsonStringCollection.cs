using System.Text.Json;

namespace EquilibraFitPlusPlus.Application.Common.Serialization;

/// <summary>
/// Serializes and deserializes user-provided string collections.
/// </summary>
public static class JsonStringCollection
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Serializes a collection to JSON.
    /// </summary>
    public static string Serialize(IReadOnlyCollection<string>? values)
    {
        string[] normalized = values?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()
            ?? [];

        return JsonSerializer.Serialize(normalized, Options);
    }

    /// <summary>
    /// Deserializes a JSON collection.
    /// </summary>
    public static IReadOnlyCollection<string> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        return JsonSerializer.Deserialize<string[]>(json, Options) ?? [];
    }
}
