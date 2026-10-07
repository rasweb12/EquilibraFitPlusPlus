using EquilibraFitPlusPlus.Application.Features.Admin.Commands.SalvarFeatureFlag;
using EquilibraFitPlusPlus.Application.Features.Admin.Commands.PublicarAdminIaEnsino;
using EquilibraFitPlusPlus.Application.Features.Admin.Commands.RestaurarAdminIaEnsino;
using EquilibraFitPlusPlus.Application.Features.Admin.Commands.RevogarSessoesUsuario;
using EquilibraFitPlusPlus.Application.Features.Admin.Commands.TestarAdminIaEnsino;
using EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAdminUsuarios;
using EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAdminIaEnsinoVersoes;
using EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarAuditorias;
using EquilibraFitPlusPlus.Application.Features.Admin.Queries.ListarFeatureFlags;
using EquilibraFitPlusPlus.Application.Features.Admin.Queries.ObterAdminDashboard;
using EquilibraFitPlusPlus.Application.Features.Admin.Queries.ObterAdminIaOperacao;
using EquilibraFitPlusPlus.Application.Features.Gamificacao.Queries.ObterGamificacaoAdmin;
using EquilibraFitPlusPlus.Application.Features.Lgpd.Queries.ObterLgpdAdmin;
using EquilibraFitPlusPlus.Application.Features.Marketplace.Commands.SalvarParceiro;
using EquilibraFitPlusPlus.Application.Features.Marketplace.Queries.ListarParceiros;
using EquilibraFitPlusPlus.Application.Features.Notificacoes.Commands.CriarNotificacao;
using EquilibraFitPlusPlus.Application.Features.Notificacoes.Queries.ListarNotificacoes;
using EquilibraFitPlusPlus.Application.Features.Premium.Commands.SalvarCupom;
using EquilibraFitPlusPlus.Application.Features.Premium.Queries.ListarAssinaturasAdmin;
using EquilibraFitPlusPlus.Application.Features.Premium.Queries.ListarCuponsAdmin;
using EquilibraFitPlusPlus.Application.Features.Relatorios.Queries.ObterRelatorioAdmin;
using EquilibraFitPlusPlus.Contracts.Admin;
using EquilibraFitPlusPlus.Contracts.Gamificacao;
using EquilibraFitPlusPlus.Contracts.Lgpd;
using EquilibraFitPlusPlus.Contracts.Marketplace;
using EquilibraFitPlusPlus.Contracts.Notificacoes;
using EquilibraFitPlusPlus.Contracts.Premium;
using EquilibraFitPlusPlus.Contracts.Relatorios;
using EquilibraFitPlusPlus.Shared.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Administrative endpoints for operations, support and governance.
/// </summary>
[Authorize(Policy = "AdminOnly")]
[Route("api/v1/admin")]
public sealed class AdminController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public AdminController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets the administrative dashboard.
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(AdminDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterDashboard([FromQuery] DateOnly? data, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterAdminDashboardQuery(tenantId, data ?? DateOnly.FromDateTime(DateTime.UtcNow)), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets AI operation status, metrics, sanitized history and published teaching configuration.
    /// </summary>
    [HttpGet("ia/operacao")]
    [ProducesResponseType(typeof(AdminIaOperacaoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterIaOperacao([FromQuery] DateOnly? data, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterAdminIaOperacaoQuery(tenantId, data ?? DateOnly.FromDateTime(DateTime.UtcNow)), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists published AI teaching versions.
    /// </summary>
    [HttpGet("ia/ensino/versoes")]
    [ProducesResponseType(typeof(IReadOnlyCollection<AdminIaEnsinoVersaoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarIaEnsinoVersoes(CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarAdminIaEnsinoVersoesQuery(tenantId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Tests an AI teaching draft without publishing it.
    /// </summary>
    [HttpPost("ia/ensino/testar")]
    [ProducesResponseType(typeof(TestarAdminIaEnsinoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestarIaEnsino([FromBody] TestarAdminIaEnsinoRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new TestarAdminIaEnsinoCommand(tenantId, usuarioId, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Publishes AI teaching configuration for runtime AI workflows.
    /// </summary>
    [HttpPut("ia/ensino/publicar")]
    [ProducesResponseType(typeof(AdminIaEnsinoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PublicarIaEnsino([FromBody] PublicarAdminIaEnsinoRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new PublicarAdminIaEnsinoCommand(tenantId, usuarioId, GetIp(), GetUserAgent(), request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Restores a previous AI teaching version as a new published version.
    /// </summary>
    [HttpPost("ia/ensino/restaurar")]
    [ProducesResponseType(typeof(AdminIaEnsinoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RestaurarIaEnsino([FromBody] RestaurarAdminIaEnsinoRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new RestaurarAdminIaEnsinoCommand(tenantId, usuarioId, GetIp(), GetUserAgent(), request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists users for administrative support and governance.
    /// </summary>
    [HttpGet("usuarios")]
    [ProducesResponseType(typeof(PagedResult<AdminUsuarioResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarUsuarios([FromQuery] string? termo, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarAdminUsuariosQuery(tenantId, termo, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Revokes active refresh tokens for a user, forcing renewed authentication.
    /// </summary>
    [HttpPost("usuarios/{usuarioId:guid}/revogar-sessoes")]
    [ProducesResponseType(typeof(RevogarSessoesUsuarioResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RevogarSessoesUsuario(Guid usuarioId, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid adminUsuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new RevogarSessoesUsuarioCommand(tenantId, adminUsuarioId, usuarioId, GetIp(), GetUserAgent()), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists administrative audit entries.
    /// </summary>
    [HttpGet("auditorias")]
    [ProducesResponseType(typeof(PagedResult<AuditoriaResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAuditorias([FromQuery] string? entidade, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarAuditoriasQuery(tenantId, entidade, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists feature flags.
    /// </summary>
    [HttpGet("feature-flags")]
    [ProducesResponseType(typeof(PagedResult<FeatureFlagResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarFeatureFlags([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarFeatureFlagsQuery(tenantId, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates or updates a feature flag.
    /// </summary>
    [HttpPut("feature-flags")]
    [ProducesResponseType(typeof(FeatureFlagResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SalvarFeatureFlag([FromBody] SalvarFeatureFlagRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        string? ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        string? userAgent = Request.Headers["User-Agent"].ToString();
        var result = await _sender.Send(new SalvarFeatureFlagCommand(tenantId, usuarioId, ip, userAgent, request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists subscriptions for financial operations.
    /// </summary>
    [HttpGet("premium/assinaturas")]
    [ProducesResponseType(typeof(PagedResult<AssinaturaResumoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarAssinaturas([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarAssinaturasAdminQuery(tenantId, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists coupons for financial operations.
    /// </summary>
    [HttpGet("premium/cupons")]
    [ProducesResponseType(typeof(PagedResult<CupomResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarCupons([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarCuponsAdminQuery(tenantId, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates or updates a coupon.
    /// </summary>
    [HttpPut("premium/cupons")]
    [ProducesResponseType(typeof(CupomResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SalvarCupom([FromBody] SalvarCupomRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new SalvarCupomCommand(tenantId, usuarioId, GetIp(), GetUserAgent(), request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists marketplace partners for administrators.
    /// </summary>
    [HttpGet("marketplace/parceiros")]
    [ProducesResponseType(typeof(PagedResult<ParceiroResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarParceiros([FromQuery] string? termo, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarParceirosQuery(tenantId, termo, false, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates or updates a marketplace partner.
    /// </summary>
    [HttpPut("marketplace/parceiros")]
    [ProducesResponseType(typeof(ParceiroResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SalvarParceiro([FromBody] SalvarParceiroRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new SalvarParceiroCommand(tenantId, usuarioId, GetIp(), GetUserAgent(), request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lists notifications for administrators.
    /// </summary>
    [HttpGet("notificacoes")]
    [ProducesResponseType(typeof(PagedResult<NotificacaoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListarNotificacoes([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ListarNotificacoesQuery(tenantId, null, page, pageSize), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a user notification.
    /// </summary>
    [HttpPost("notificacoes")]
    [ProducesResponseType(typeof(NotificacaoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CriarNotificacao([FromBody] CriarNotificacaoRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out Guid usuarioId))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new CriarNotificacaoCommand(tenantId, usuarioId, GetIp(), GetUserAgent(), request), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets administrative report summary.
    /// </summary>
    [HttpGet("relatorios/resumo")]
    [ProducesResponseType(typeof(RelatorioAdminResumoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterRelatorioAdmin([FromQuery] DateOnly? inicio, [FromQuery] DateOnly? fim, CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        DateOnly end = fim ?? DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly start = inicio ?? end.AddDays(-29);
        var result = await _sender.Send(new ObterRelatorioAdminQuery(tenantId, start, end), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets administrative gamification summary.
    /// </summary>
    [HttpGet("gamificacao/resumo")]
    [ProducesResponseType(typeof(GamificacaoAdminResumoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterGamificacaoAdmin(CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterGamificacaoAdminQuery(tenantId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets administrative LGPD summary.
    /// </summary>
    [HttpGet("lgpd/resumo")]
    [ProducesResponseType(typeof(LgpdAdminResumoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObterLgpdAdmin(CancellationToken cancellationToken)
    {
        if (!TryGetUserContext(out Guid tenantId, out _))
        {
            return UnauthorizedUserContext();
        }

        var result = await _sender.Send(new ObterLgpdAdminQuery(tenantId), cancellationToken);
        return HandleResult(result);
    }

    private string? GetIp() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private string? GetUserAgent() => Request.Headers["User-Agent"].ToString();
}
