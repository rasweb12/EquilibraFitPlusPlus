using EquilibraFitPlusPlus.Contracts.Auth;
using EquilibraFitPlusPlus.Shared.Results;
using MediatR;

namespace EquilibraFitPlusPlus.Application.Features.Auth.Commands.CadastrarUsuario;

/// <summary>
/// Command used to register a user.
/// </summary>
public sealed record CadastrarUsuarioCommand(CadastrarUsuarioRequest Request, string? IpAddress) : IRequest<Result<AuthResponse>>;
