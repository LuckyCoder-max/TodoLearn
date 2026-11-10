using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using TodoLearn.Helpers;
using TodoLearn.Models;
using TodoLearn.Resources.Strings;

namespace TodoLearn.Services;

public sealed class AuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(1);

    private readonly IDbContextFactory<AppDbContext> _dbFactory;
    private readonly TimeProvider _timeProvider;
    private readonly Dictionary<string, LoginAttempt> _loginAttempts = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _loginAttemptsLock = new();

    public AuthService(IDbContextFactory<AppDbContext> dbFactory, TimeProvider? timeProvider = null)
    {
        _dbFactory = dbFactory;
        _timeProvider = timeProvider ?? TimeProvider.System;
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

    public async Task<int?> GetAccountIdAsync(string username)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db.UserAccounts
            .Where(account => account.Username == username)
            .Select(account => (int?)account.Id)
            .SingleOrDefaultAsync();
    }

    public async Task<string> GetOrCreateExternalAccountAsync(string provider, string subject, string email)
    {
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(email))
            throw new ArgumentException();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var externalLogin = await db.ExternalLogins
            .Include(login => login.UserAccount)
            .SingleOrDefaultAsync(login => login.Provider == provider && login.Subject == subject);
        if (externalLogin is not null)
            return externalLogin.UserAccount.Username;

        var username = email.Trim();
        if (username.Length > 50 || await db.UserAccounts.AnyAsync(account => account.Username == username))
            username = $"{provider.ToLowerInvariant()}_{subject}";

        var randomPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var (passwordHash, passwordSalt) = PasswordHasher.Create(randomPassword);
        var account = new UserAccount
        {
            Username = username,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt
        };

        db.UserAccounts.Add(account);
        account.ExternalLogins.Add(new ExternalLogin
        {
            Provider = provider,
            Subject = subject,
            UserAccount = account
        });
        await db.SaveChangesAsync();
        return account.Username;
    }

    private TimeSpan? GetLockoutRemaining(string username)
    {
        lock (_loginAttemptsLock)
        {
            if (!_loginAttempts.TryGetValue(username, out var attempt))
                return null;

            if (attempt.LockedUntil is null)
                return null;

            var remaining = attempt.LockedUntil.Value - _timeProvider.GetUtcNow().UtcDateTime;
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

            attempt.LockedUntil = _timeProvider.GetUtcNow().UtcDateTime.Add(LockoutDuration);
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
