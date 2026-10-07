namespace EquilibraFitPlusPlus.Application.Common.Health;

/// <summary>
/// Provides basic health calculations used by application use cases.
/// </summary>
public static class HealthMetrics
{
    /// <summary>
    /// Calculates age at a reference date.
    /// </summary>
    public static int CalculateAge(DateOnly birthDate, DateOnly referenceDate)
    {
        int age = referenceDate.Year - birthDate.Year;
        return birthDate > referenceDate.AddYears(-age) ? age - 1 : age;
    }

    /// <summary>
    /// Calculates body mass index.
    /// </summary>
    public static decimal CalculateImc(decimal pesoKg, decimal alturaCm)
    {
        if (alturaCm <= 0)
        {
            return 0;
        }

        decimal alturaM = alturaCm / 100m;
        return Math.Round(pesoKg / (alturaM * alturaM), 2);
    }

    /// <summary>
    /// Returns a neutral BMI classification for educational context.
    /// </summary>
    public static string ClassifyImc(decimal imc)
    {
        return imc switch
        {
            < 18.5m => "Abaixo da faixa de referencia",
            < 25m => "Faixa de referencia",
            < 30m => "Acima da faixa de referencia",
            _ => "Bem acima da faixa de referencia"
        };
    }
}
