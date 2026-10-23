using Microsoft.Maui.Storage;

namespace TodoLearn.Services;

public sealed class AuthSessionService
{
    private const string RememberedUsernameKey = "todo.auth.remembered_username";
    private readonly AuthService _authService;

    public AuthSessionService(AuthService authService)
    {
        _authService = authService;
    }

    public async Task<bool> RememberAccountAsync(string username)
    {
        try
        {
            await SecureStorage.Default.SetAsync(RememberedUsernameKey, username);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string?> GetRememberedUsernameAsync()
    {
        try
        {
            var username = await SecureStorage.Default.GetAsync(RememberedUsernameKey);
            if (!string.IsNullOrWhiteSpace(username) && await _authService.AccountExistsAsync(username))
                return username;
        }
        catch
        {
        }

        ClearRememberedAccount();
        return null;
    }

    public void ClearRememberedAccount()
    {
        try
        {
            SecureStorage.Default.Remove(RememberedUsernameKey);
        }
        catch
        {
        }
    }
}
