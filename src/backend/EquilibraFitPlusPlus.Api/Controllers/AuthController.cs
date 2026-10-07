using EquilibraFitPlusPlus.Application.Features.Auth.Commands.CadastrarUsuario;
using EquilibraFitPlusPlus.Application.Features.Auth.Commands.Login;
using EquilibraFitPlusPlus.Application.Features.Auth.Commands.RefreshToken;
using EquilibraFitPlusPlus.Contracts.Auth;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EquilibraFitPlusPlus.Api.Controllers;

/// <summary>
/// Authentication endpoints.
/// </summary>
[Route("api/v1/auth")]
[EnableRateLimiting("auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Initializes the controller.
    /// </summary>
    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Registers a new user.
    /// </summary>
    [HttpPost("cadastrar")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cadastrar([FromBody] CadastrarUsuarioRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CadastrarUsuarioCommand(request, GetIpAddress()), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Authenticates a user.
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new LoginCommand(request, GetIpAddress()), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Rotates a refresh token.
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RefreshTokenCommand(request, GetIpAddress()), cancellationToken);
        return HandleResult(result);
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}
