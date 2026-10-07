using EquilibraFitPlusPlus.Contracts.Auth;
using Microsoft.JSInterop;

namespace EquilibraFitPlusPlus.Admin.Services;

/// <summary>
/// Holds the authenticated administrative session for the current Blazor circuit.
/// </summary>
public sealed class AdminSessionState
{
    private const string StorageKey = "equilibrafit.plusplus.admin.session";
    private readonly IJSRuntime _jsRuntime;

    /// <summary>Initializes the session state.</summary>
    public AdminSessionState(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>Current JWT access token.</summary>
    public string? AccessToken { get; private set; }

    /// <summary>Current refresh token.</summary>
    public string? RefreshToken { get; private set; }

    /// <summary>Authenticated user summary.</summary>
    public UsuarioAutenticadoResponse? User { get; private set; }

    /// <summary>Indicates whether an administrator is authenticated in this circuit.</summary>
    public bool IsAuthenticated => !string.IsNullOrWhiteSpace(AccessToken) && User is not null;

    /// <summary>Indicates whether the current role may access administrative operations.</summary>
    public bool IsAdmin => User?.Role is "Administrador" or "SuperAdmin";

    /// <summary>Starts a new administrative session.</summary>
    public void Start(AuthResponse response)
    {
        AccessToken = response.AccessToken;
        RefreshToken = response.RefreshToken;
        User = response.Usuario;
    }

    /// <summary>Stores the current administrative session in browser session storage.</summary>
    public async Task StoreAsync(CancellationToken cancellationToken)
    {
        if (!IsAuthenticated)
        {
            return;
        }

        var snapshot = new AdminSessionSnapshot(AccessToken!, RefreshToken ?? string.Empty, User!);
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", cancellationToken, StorageKey, System.Text.Json.JsonSerializer.Serialize(snapshot));
    }

    /// <summary>Restores the administrative session from browser session storage.</summary>
    public async Task<bool> RestoreAsync(CancellationToken cancellationToken)
    {
        if (IsAuthenticated)
        {
            return true;
        }

        string? serialized = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", cancellationToken, StorageKey);
        if (string.IsNullOrWhiteSpace(serialized))
        {
            return false;
        }

        AdminSessionSnapshot? snapshot;
        try
        {
            snapshot = System.Text.Json.JsonSerializer.Deserialize<AdminSessionSnapshot>(serialized);
        }
        catch (System.Text.Json.JsonException)
        {
            await ClearStorageAsync(cancellationToken);
            return false;
        }

        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.AccessToken) || snapshot.User is null)
        {
            await ClearStorageAsync(cancellationToken);
            return false;
        }

        AccessToken = snapshot.AccessToken;
        RefreshToken = snapshot.RefreshToken;
        User = snapshot.User;
        return IsAdmin;
    }

    /// <summary>Clears the current administrative session.</summary>
    public void Clear()
    {
        AccessToken = null;
        RefreshToken = null;
        User = null;
    }

    /// <summary>Clears the current administrative session and browser storage.</summary>
    public async Task ClearAsync(CancellationToken cancellationToken)
    {
        Clear();
        await ClearStorageAsync(cancellationToken);
    }

    private ValueTask ClearStorageAsync(CancellationToken cancellationToken)
    {
        return _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", cancellationToken, StorageKey);
    }

    private sealed record AdminSessionSnapshot(string AccessToken, string RefreshToken, UsuarioAutenticadoResponse User);
}
