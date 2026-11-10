using Microsoft.EntityFrameworkCore;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn.Tests.Integration;

[TestFixture, Category("Integration")]
public sealed class AuthServiceTests : DatabaseTest
{
    private AuthService Auth => new(Database, Clock);
    private const string Password = " Password1 ";

    [Test]
    public async Task RegistrationStoresTrimmedUsernameAndHashedPassword()
    {
        Assert.That(await Auth.RegisterAsync(" alice ", Password), Is.Null);
        await using var db = Database.CreateDbContext();
        var account = await db.UserAccounts.SingleAsync(user => user.Username == "alice");
        Assert.Multiple(() =>
        {
            Assert.That(account.PasswordHash, Is.Not.EqualTo(Password));
            Assert.That(PasswordHasher.Verify(Password, account.PasswordHash, account.PasswordSalt), Is.True);
            Assert.That(PasswordHasher.Verify(Password.Trim(), account.PasswordHash, account.PasswordSalt), Is.False);
        });
        Assert.That((await Auth.SignInAsync(" alice ", Password)).IsAuthenticated, Is.True);
    }

    [Test]
    public async Task DuplicateUsernameIsRejectedWithoutAddingAccount()
    {
        await Auth.RegisterAsync("alice", Password);
        Assert.That(await Auth.RegisterAsync(" alice ", Password), Is.EqualTo(AppResources.UsernameTaken));
        await using var db = Database.CreateDbContext();
        Assert.That(await db.UserAccounts.CountAsync(user => user.Username == "alice"), Is.EqualTo(1));
    }

    [TestCase(null, "Password1")]
    [TestCase("alice", "short")]
    [TestCase("alice", null)]
    public async Task InvalidRegistrationDoesNotWriteToDatabase(string? username, string? password)
    {
        var contexts = Database.ContextsCreated;
        Assert.That(await Auth.RegisterAsync(username, password), Is.Not.Null);
        Assert.That(Database.ContextsCreated, Is.EqualTo(contexts));
    }

    [TestCase("alice", "wrong")]
    [TestCase("missing", "Password1")]
    [TestCase(null, "Password1")]
    [TestCase("alice", null)]
    public async Task InvalidSignInIsRejected(string? username, string? password)
    {
        await Auth.RegisterAsync("alice", Password);
        Assert.That((await Auth.SignInAsync(username, password)).IsAuthenticated, Is.False);
    }

    [Test]
    public async Task FifthFailureLocksAccountAndExpiryAllowsCorrectPassword()
    {
        var auth = Auth;
        await auth.RegisterAsync("alice", Password);
        for (var attempt = 1; attempt < 5; attempt++)
            Assert.That((await auth.SignInAsync("alice", "wrong")).IsLocked, Is.False);
        var blocked = await auth.SignInAsync("alice", "wrong");
        Assert.That(blocked.LockoutRemaining, Is.EqualTo(TimeSpan.FromMinutes(1)));
        Assert.That((await auth.SignInAsync("ALICE", Password)).IsLocked, Is.True);
        Assert.That((await auth.SignInAsync("alice", Password)).IsAuthenticated, Is.False);
        Clock.Advance(TimeSpan.FromMinutes(1));
        Assert.That((await auth.SignInAsync("alice", Password)).IsAuthenticated, Is.True);
    }

    [Test]
    public async Task SuccessfulSignInResetsFailureCount()
    {
        var auth = Auth;
        await auth.RegisterAsync("alice", Password);
        for (var attempt = 0; attempt < 4; attempt++) await auth.SignInAsync("alice", "wrong");
        Assert.That((await auth.SignInAsync("alice", Password)).IsAuthenticated, Is.True);
        Assert.That((await auth.SignInAsync("alice", "wrong")).IsLocked, Is.False);
    }

    [Test]
    public async Task LockoutDoesNotBlockOtherAccounts()
    {
        var auth = Auth;
        await auth.RegisterAsync("alice", Password);
        await auth.RegisterAsync("bob", Password);
        for (var attempt = 0; attempt < 5; attempt++) await auth.SignInAsync("alice", "wrong");
        Assert.That((await auth.SignInAsync("bob", Password)).IsAuthenticated, Is.True);
    }

    [Test]
    public async Task AccountLookupReturnsNullForUnknownUser()
    {
        Assert.That(await Auth.GetAccountIdAsync("first"), Is.EqualTo(1));
        Assert.That(await Auth.GetAccountIdAsync("unknown"), Is.Null);
    }

    [Test]
    public async Task ExternalIdentityIsReusedEvenAfterEmailChanges()
    {
        var auth = Auth;
        var first = await auth.GetOrCreateExternalAccountAsync("Google", "subject-1", "alice@example.com");
        var second = await auth.GetOrCreateExternalAccountAsync("Google", "subject-1", "new@example.com");
        Assert.That(second, Is.EqualTo(first));
        await using var db = Database.CreateDbContext();
        Assert.That(await db.ExternalLogins.CountAsync(), Is.EqualTo(1));
        Assert.That(await db.UserAccounts.CountAsync(), Is.EqualTo(3));
    }

    [Test]
    public async Task ExternalEmailCollisionDoesNotLinkLocalAccount()
    {
        var username = await Auth.GetOrCreateExternalAccountAsync("Google", "subject-1", "first");
        Assert.That(username, Is.Not.EqualTo("first"));
        Assert.That(await Auth.GetAccountIdAsync(username), Is.Not.EqualTo(1));
    }

    [TestCase("", "subject", "email")]
    [TestCase("Google", " ", "email")]
    [TestCase("Google", "subject", "")]
    public void ExternalIdentityRequiresAllFields(string provider, string subject, string email) =>
        Assert.ThrowsAsync<ArgumentException>(() => Auth.GetOrCreateExternalAccountAsync(provider, subject, email));
}
