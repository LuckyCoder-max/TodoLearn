using Microsoft.Maui.Storage;

namespace TodoLearn.Services;

public sealed class AuthSessionService : IAccountSession
{
    private const string RememberedUsernameKey = "todo.auth.remembered_username";
    private readonly AuthService _authService;

    public string? CurrentUsername { get; private set; }
    public int? CurrentAccountId { get; private set; }

    public AuthSessionService(AuthService authService)
    {
        _authService = authService;
    }

    public async Task<bool> RememberAccountAsync(string username)
    {
        var accountId = await _authService.GetAccountIdAsync(username);
        if (!accountId.HasValue)
            return false;

        CurrentUsername = username;
        CurrentAccountId = accountId.Value;
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
            if (!string.IsNullOrWhiteSpace(username))
            {
                var accountId = await _authService.GetAccountIdAsync(username);
                if (accountId.HasValue)
                {
                    CurrentUsername = username;
                    CurrentAccountId = accountId.Value;
                    return username;
                }
            }
        }
        catch
        {
        }

        ClearRememberedAccount();
        return null;
    }

    public void ClearRememberedAccount()
    {
        CurrentUsername = null;
        CurrentAccountId = null;
        try
        {
            SecureStorage.Default.Remove(RememberedUsernameKey);
        }
        catch
        {
        }
    }
}
