using EquilibraFitPlusPlus.Application.Abstractions.Authentication;
using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Handles refresh token rotation.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IAuthenticationService _authenticationService;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public RefreshTokenCommandHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <inheritdoc />
    public Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        return _authenticationService.RefreshAsync(request.Request, request.IpAddress, cancellationToken);
    }
}
