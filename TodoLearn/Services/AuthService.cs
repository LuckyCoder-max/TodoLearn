using Microsoft.EntityFrameworkCore;
using TodoLearn.Helpers;
using TodoLearn.Models;
using TodoLearn.Resources.Strings;

namespace TodoLearn.Services;

public sealed class AuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(1);

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly Dictionary<string, LoginAttempt> _loginAttempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _loginAttemptsLock = new();

    public AuthService(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<string?> RegisterAsync(string? username, string? password)
    {
        var validationError = CredentialsValidator.ValidateRegistration(username, password);
        if (validationError is not null)
            return validationError;

        var normalizedUsername = username!.Trim();
        var passwordToStore = password!;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var alreadyExists = await db.UserAccounts.AnyAsync(user => user.Username == normalizedUsername);
        if (alreadyExists)
            return AppResources.UsernameTaken;

        var (hash, salt) = PasswordHasher.Create(passwordToStore);
        db.UserAccounts.Add(new UserAccount
        {
            Username = normalizedUsername,
            PasswordHash = hash,
            PasswordSalt = salt
        });
        await db.SaveChangesAsync();

        return null;
    }

    public async Task<SignInResult> SignInAsync(string? username, string? password)
    {
        if (!CredentialsValidator.HasSignInValues(username, password))
            return new SignInResult(false);

        var normalizedUsername = username!.Trim();
        var passwordToVerify = password!;
        var lockoutRemaining = GetLockoutRemaining(normalizedUsername);
        if (lockoutRemaining.HasValue)
            return new SignInResult(false, lockoutRemaining);

        await using var db = await _dbFactory.CreateDbContextAsync();
        var user = await db.UserAccounts.SingleOrDefaultAsync(account => account.Username == normalizedUsername);

        if (user is not null && PasswordHasher.Verify(passwordToVerify, user.PasswordHash, user.PasswordSalt))
        {
            ClearFailedAttempts(normalizedUsername);
            return new SignInResult(true);
        }

        return new SignInResult(false, RegisterFailedAttempt(normalizedUsername));
    }

    private TimeSpan? GetLockoutRemaining(string username)
    {
        lock (_loginAttemptsLock)
        {
            if (!_loginAttempts.TryGetValue(username, out var attempt))
                return null;

            if (attempt.LockedUntil is null)
                return null;

            var remaining = attempt.LockedUntil.Value - DateTime.UtcNow;
            if (remaining > TimeSpan.Zero)
                return remaining;

            _loginAttempts.Remove(username);
            return null;
        }
    }

    private TimeSpan? RegisterFailedAttempt(string username)
    {
        lock (_loginAttemptsLock)
        {
            if (!_loginAttempts.TryGetValue(username, out var attempt))
            {
                attempt = new LoginAttempt();
                _loginAttempts[username] = attempt;
            }

            attempt.FailedAttempts++;
            if (attempt.FailedAttempts < MaxFailedAttempts)
                return null;

            attempt.LockedUntil = DateTime.UtcNow.Add(LockoutDuration);
            return LockoutDuration;
        }
    }

    private void ClearFailedAttempts(string username)
    {
        lock (_loginAttemptsLock)
            _loginAttempts.Remove(username);
    }

    private sealed class LoginAttempt
    {
        public int FailedAttempts { get; set; }
        public DateTime? LockedUntil { get; set; }
    }
}
