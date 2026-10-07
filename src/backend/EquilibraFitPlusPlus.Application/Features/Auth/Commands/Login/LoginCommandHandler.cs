using EquilibraFitPlusPlus.Application.Abstractions.Authentication;
using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Auth.Commands.Login;

/// <summary>
/// Handles login commands.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IAuthenticationService _authenticationService;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public LoginCommandHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <inheritdoc />
    public Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        return _authenticationService.LoginAsync(request.Request, request.IpAddress, cancellationToken);
    }
}
