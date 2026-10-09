using EquilibraFitPlusPlus.Shared.Errors;

namespace EquilibraFitPlusPlus.Application.Abstractions.AiCoach;

/// <summary>Transport failures, distinct from clinical or input validation errors.</summary>
public static class AiServiceErrors
{
    /// <summary>The AI service cannot currently accept requests.</summary>
    public const string Unavailable = "ia.servico_indisponivel";
    /// <summary>The upstream deadline elapsed.</summary>
    public const string Timeout = "ia.servico_timeout";
    /// <summary>The AI gateway returned an unsuccessful response.</summary>
    public const string BadGateway = "ia.upstream_bad_gateway";
    /// <summary>Authentication between trusted services failed, not the user's session.</summary>
    public const string Authentication = "ia.upstream_authentication_failed";
    /// <summary>The upstream response did not satisfy the expected contract.</summary>
    public const string InvalidResponse = "ia.resposta_invalida";
    /// <summary>The trusted service connection is not configured.</summary>
    public const string NotConfigured = "ia.servico_nao_configurado";

    /// <summary>Whether the error must bypass business/clinical fallback processing.</summary>
    public static bool IsInfrastructureError(Error error) => error.Code is
        Unavailable or Timeout or BadGateway or Authentication or InvalidResponse or NotConfigured;

    /// <summary>Creates a safe error without upstream bodies or credentials.</summary>
    public static Error Create(string code) => new(code, code == Timeout
        ? "O servico de IA demorou para responder. Tente novamente em instantes."
        : "IA temporariamente indisponivel. Tente novamente em instantes; os registros manuais continuam disponiveis.");
}
