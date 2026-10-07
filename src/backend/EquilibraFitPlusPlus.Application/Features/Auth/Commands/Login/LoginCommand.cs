using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command used to authenticate a user.
/// </summary>
public sealed record LoginCommand(LoginRequest Request, string? IpAddress) : IRequest<Result<AuthResponse>>;
