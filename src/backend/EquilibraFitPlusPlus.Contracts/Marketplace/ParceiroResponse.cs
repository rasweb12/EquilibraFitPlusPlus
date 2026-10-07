namespace EquilibraFitPlusPlus.Contracts.Marketplace;

/// <summary>
/// Marketplace partner response.
/// </summary>
public sealed record ParceiroResponse(
    Guid Id,
    string Nome,
    string Tipo,
    string Status,
    string? EmailContato,
    string? Documento);

/// <summary>
/// Request used to create or update a marketplace partner.
/// </summary>
public sealed record SalvarParceiroRequest(
    Guid? Id,
    string Nome,
    string Tipo,
    string Status,
    string? EmailContato,
    string? Documento);
