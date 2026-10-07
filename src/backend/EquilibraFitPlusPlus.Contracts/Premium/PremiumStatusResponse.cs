namespace EquilibraFitPlusPlus.Contracts.Premium;

/// <summary>
/// Premium entitlement summary for the authenticated user.
/// </summary>
public sealed record PremiumStatusResponse(
    Guid? AssinaturaId,
    string PlanoCodigo,
    string Status,
    bool PremiumAtivo,
    DateOnly? TerminaEm,
    int ReconhecimentosFotoDia,
    int MensagensCoachDia,
    string Mensagem);

/// <summary>
/// Administrative subscription summary.
/// </summary>
public sealed record AssinaturaResumoResponse(
    Guid Id,
    Guid UsuarioId,
    string PlanoCodigo,
    string Status,
    DateOnly InicioEm,
    DateOnly? TerminaEm,
    string? ProviderId);

/// <summary>
/// Administrative payment summary.
/// </summary>
public sealed record PagamentoResumoResponse(
    Guid Id,
    Guid AssinaturaId,
    string Provider,
    string Metodo,
    decimal Valor,
    string Moeda,
    string Status,
    string? TransacaoExternaId);

/// <summary>
/// Coupon response used by premium and admin modules.
/// </summary>
public sealed record CupomResponse(
    Guid Id,
    string Codigo,
    decimal? PercentualDesconto,
    decimal? ValorDesconto,
    DateOnly? ExpiraEm,
    int? UsoMaximo,
    int UsoAtual,
    bool Ativo);

/// <summary>
/// Request used to create or update a coupon.
/// </summary>
public sealed record SalvarCupomRequest(
    string Codigo,
    decimal? PercentualDesconto,
    decimal? ValorDesconto,
    DateOnly? ExpiraEm,
    int? UsoMaximo);
