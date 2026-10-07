using System.Diagnostics;
using System.Net.Http.Json;
using EquilibraFitPlusPlus.Application.Abstractions.Admin;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EquilibraFitPlusPlus.Infrastructure.AiCoach;

/// <summary>
/// HTTP health client for the external AI service.
/// </summary>
public sealed class AdminIaHealthClient : IAdminIaHealthClient
{
    private readonly HttpClient _httpClient;
    private readonly AiCoachOptions _options;
    private readonly ILogger<AdminIaHealthClient> _logger;

    /// <summary>
    /// Initializes the health client.
    /// </summary>
    public AdminIaHealthClient(HttpClient httpClient, IOptions<AiCoachOptions> options, ILogger<AdminIaHealthClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<AdminIaServicoStatusSourceData> ObterStatusAsync(CancellationToken cancellationToken)
    {
        if (_httpClient.BaseAddress is null)
        {
            return new AdminIaServicoStatusSourceData(
                false,
                false,
                null,
                _options.BaseUrl,
                _options.Model,
                "Serviço de IA não configurado neste ambiente.");
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync("/health", cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                return new AdminIaServicoStatusSourceData(
                    true,
                    false,
                    (int)stopwatch.ElapsedMilliseconds,
                    _httpClient.BaseAddress.ToString(),
                    _options.Model,
                    "Serviço de IA respondeu com falha HTTP.");
            }

            AiHealthResponse? health = await response.Content.ReadFromJsonAsync<AiHealthResponse>(cancellationToken);
            string model = string.IsNullOrWhiteSpace(health?.Model) ? _options.Model : health.Model;
            string status = string.IsNullOrWhiteSpace(health?.Status) ? "Healthy" : health.Status;

            return new AdminIaServicoStatusSourceData(
                true,
                status.Equals("Healthy", StringComparison.OrdinalIgnoreCase),
                (int)stopwatch.ElapsedMilliseconds,
                _httpClient.BaseAddress.ToString(),
                model,
                status.Equals("Healthy", StringComparison.OrdinalIgnoreCase)
                    ? "Serviço de IA online."
                    : "Serviço de IA respondeu, mas não está saudável.");
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning(exception, "AI health check failed.");
            return new AdminIaServicoStatusSourceData(
                true,
                false,
                (int)stopwatch.ElapsedMilliseconds,
                _httpClient.BaseAddress.ToString(),
                _options.Model,
                "Serviço de IA indisponível agora.");
        }
    }

    private sealed record AiHealthResponse(string? Status, string? Service, string? Model);
}
