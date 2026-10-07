using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command used to rotate a refresh token.
/// </summary>
public sealed record RefreshTokenCommand(RefreshTokenRequest Request, string? IpAddress) : IRequest<Result<AuthResponse>>;
