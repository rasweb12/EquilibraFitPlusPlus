using EquilibraFitPlusPlus.Application.Abstractions.Authentication;
using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Auth.Commands.CadastrarUsuario;

/// <summary>
/// Handles user registration.
/// </summary>
public sealed class CadastrarUsuarioCommandHandler : IRequestHandler<CadastrarUsuarioCommand, Result<AuthResponse>>
{
    private readonly IAuthenticationService _authenticationService;

    /// <summary>
    /// Initializes the handler.
    /// </summary>
    public CadastrarUsuarioCommandHandler(IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    /// <inheritdoc />
    public Task<Result<AuthResponse>> Handle(CadastrarUsuarioCommand request, CancellationToken cancellationToken)
    {
        return _authenticationService.CadastrarAsync(request.Request, request.IpAddress, cancellationToken);
    }
}
