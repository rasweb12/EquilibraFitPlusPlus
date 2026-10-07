using EquilibraFitPlusPlus.Domain.Common;
using EquilibraFitPlusPlus.Domain.Enums;

namespace EquilibraFitPlusPlus.Domain.Entities;

/// <summary>
/// Runtime feature flag.
/// </summary>
public sealed class FeatureFlag : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Feature key.</summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>Indicates whether the feature is enabled.</summary>
    public bool Habilitada { get; set; }

    /// <summary>Optional JSON configuration.</summary>
    public string? ConfiguracaoJson { get; set; }
}

/// <summary>
/// System configuration entry.
/// </summary>
public sealed class ConfiguracaoSistema : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Configuration key.</summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>Configuration value.</summary>
    public string Valor { get; set; } = string.Empty;

    /// <summary>Indicates whether the value is sensitive.</summary>
    public bool Sensivel { get; set; }
}

/// <summary>
/// Audit log for administrative and sensitive actions.
/// </summary>
public sealed class Auditoria : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Actor user identifier.</summary>
    public Guid? UsuarioId { get; set; }

    /// <summary>Action name.</summary>
    public string Acao { get; set; } = string.Empty;

    /// <summary>Entity name.</summary>
    public string Entidade { get; set; } = string.Empty;

    /// <summary>Affected entity identifier.</summary>
    public Guid? EntidadeId { get; set; }

    /// <summary>Safe metadata as JSON.</summary>
    public string? MetadadosJson { get; set; }

    /// <summary>IP address.</summary>
    public string? Ip { get; set; }

    /// <summary>User agent.</summary>
    public string? UserAgent { get; set; }
}

/// <summary>
/// User notification.
/// </summary>
public sealed class Notificacao : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Notification title.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>Notification message.</summary>
    public string Mensagem { get; set; } = string.Empty;

    /// <summary>Notification status.</summary>
    public StatusNotificacao Status { get; set; } = StatusNotificacao.Pendente;

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }
}

/// <summary>
/// Stores a successful response for an idempotent client write operation.
/// </summary>
public sealed class IdempotencyRecord : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User that owns the write operation.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Client-generated idempotency key.</summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>HTTP method for the original operation.</summary>
    public string Metodo { get; set; } = string.Empty;

    /// <summary>Normalized request path for the original operation.</summary>
    public string Caminho { get; set; } = string.Empty;

    /// <summary>Original response status code.</summary>
    public int ResponseStatusCode { get; set; }

    /// <summary>Original response content type.</summary>
    public string? ResponseContentType { get; set; }

    /// <summary>Original response body, when small enough to replay safely.</summary>
    public string? ResponseBody { get; set; }

    /// <summary>Expiration timestamp for this idempotency record.</summary>
    public DateTimeOffset ExpiresAt { get; set; }
}

/// <summary>
/// AI coach chat session.
/// </summary>
public sealed class ChatSession : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Session title.</summary>
    public string Titulo { get; set; } = string.Empty;

    /// <summary>User that owns the chat session.</summary>
    public Usuario? Usuario { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }

    /// <summary>Chat messages.</summary>
    public ICollection<ChatMessage> Mensagens { get; set; } = [];
}

/// <summary>
/// AI coach chat message.
/// </summary>
public sealed class ChatMessage : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Chat session identifier.</summary>
    public Guid ChatSessionId { get; set; }

    /// <summary>Message role.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Message content.</summary>
    public string Conteudo { get; set; } = string.Empty;

    /// <summary>AI model name.</summary>
    public string? ModeloIa { get; set; }

    /// <summary>Chat session that owns the message.</summary>
    public ChatSession? ChatSession { get; set; }
}

/// <summary>
/// Explicit and confirmed AI coach memory used for personalization.
/// </summary>
public sealed class AiCoachMemory : AuditableEntity, ITenantEntity, ISoftDelete
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>User identifier.</summary>
    public Guid UsuarioId { get; set; }

    /// <summary>Memory category, such as workout preference or schedule.</summary>
    public string Categoria { get; set; } = string.Empty;

    /// <summary>Stable memory key.</summary>
    public string Chave { get; set; } = string.Empty;

    /// <summary>Short memory value. This must not contain whole conversations.</summary>
    public string Valor { get; set; } = string.Empty;

    /// <summary>Fact type, separating explicit user facts from AI inferences.</summary>
    public string TipoFato { get; set; } = AiCoachMemoryFactTypes.UserPreference;

    /// <summary>Indicates whether the user explicitly confirmed this fact.</summary>
    public bool ConfirmadoPeloUsuario { get; set; } = true;

    /// <summary>Origin of the memory, without storing the full conversation.</summary>
    public string Fonte { get; set; } = "MensagemUsuario";

    /// <summary>When the memory was explicitly confirmed.</summary>
    public DateTimeOffset? ConfirmadoEm { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Memory owner.</summary>
    public Usuario? Usuario { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ExcluidoEm { get; set; }

    /// <inheritdoc />
    public Guid? ExcluidoPor { get; set; }

    /// <inheritdoc />
    public string? MotivoExclusao { get; set; }
}

/// <summary>
/// Supported AI coach memory fact types.
/// </summary>
public static class AiCoachMemoryFactTypes
{
    /// <summary>User explicitly stated or confirmed this fact.</summary>
    public const string ExplicitFact = "ExplicitFact";

    /// <summary>User explicitly stated or confirmed a stable preference.</summary>
    public const string UserPreference = "UserPreference";

    /// <summary>Short-lived context that must not be treated as a permanent fact.</summary>
    public const string TemporaryContext = "TemporaryContext";

    /// <summary>AI inferred this preference and it still needs confirmation.</summary>
    public const string ModelInference = "ModelInference";

    /// <summary>Backward-compatible alias for explicit user facts.</summary>
    public const string ExplicitUserFact = ExplicitFact;

    /// <summary>Backward-compatible alias for AI inference.</summary>
    public const string AiInference = ModelInference;
}

/// <summary>
/// Sanitized AI execution metadata for observability and cost control.
/// </summary>
public sealed class AiExecutionLog : AuditableEntity, ITenantEntity
{
    /// <inheritdoc />
    public Guid TenantId { get; set; }

    /// <summary>Hash of the user identifier, never the raw user id.</summary>
    public string UsuarioIdHash { get; set; } = string.Empty;

    /// <summary>Operation name, such as workout_generation.</summary>
    public string Operation { get; set; } = string.Empty;

    /// <summary>Prompt logical name.</summary>
    public string PromptName { get; set; } = string.Empty;

    /// <summary>Prompt version used in the call.</summary>
    public string PromptVersion { get; set; } = string.Empty;

    /// <summary>Model or local engine used.</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>Structured context contract version.</summary>
    public int ContextVersion { get; set; }

    /// <summary>Whether retrieval was used.</summary>
    public bool RetrievalUsed { get; set; }

    /// <summary>Retrieved document ids as sanitized JSON.</summary>
    public string? RetrievedDocumentIdsJson { get; set; }

    /// <summary>Whether a deterministic fallback handled the request.</summary>
    public bool FallbackUsed { get; set; }

    /// <summary>Execution latency in milliseconds.</summary>
    public long LatencyMs { get; set; }

    /// <summary>Input tokens when available from the provider.</summary>
    public int? InputTokens { get; set; }

    /// <summary>Output tokens when available from the provider.</summary>
    public int? OutputTokens { get; set; }

    /// <summary>Estimated cost when available.</summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>Structured confidence when returned by the operation.</summary>
    public decimal? Confidence { get; set; }

    /// <summary>Sanitized safety result summary.</summary>
    public string? SafetyResult { get; set; }

    /// <summary>Correlation id propagated from caller to AI service when available.</summary>
    public string? CorrelationId { get; set; }
}
