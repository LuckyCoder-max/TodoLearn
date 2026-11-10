using TodoLearn.Helpers;
using TodoLearn.Resources.Strings;
using TodoLearn.Services;

namespace TodoLearn.Tests.Unit;

[TestFixture, Category("Unit")]
public sealed class CredentialsTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void RegistrationRequiresUsername(string? username) =>
        Assert.That(CredentialsValidator.ValidateRegistration(username, "Password1"), Is.EqualTo(AppResources.UsernameRequired));

    [TestCase(50, true)]
    [TestCase(51, false)]
    public void RegistrationChecksTrimmedUsernameLength(int length, bool valid) =>
        Assert.That(CredentialsValidator.ValidateRegistration(" " + new string('a', length) + " ", "Password1") is null, Is.EqualTo(valid));

    [TestCase(null, false)]
    [TestCase("", false)]
    [TestCase("        ", false)]
    [TestCase("1234567", false)]
    [TestCase("12345678", true)]
    [TestCase(" pass word ", true)]
    public void PasswordValidationHandlesRequiredAndMinimumLength(string? password, bool valid)
    {
        var result = PasswordValidator.Validate(password);
        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.EqualTo(valid));
            Assert.That(result.ErrorMessage is null, Is.EqualTo(valid));
        });
    }

    [TestCase(null, "password", false)]
    [TestCase(" ", "password", false)]
    [TestCase("user", null, false)]
    [TestCase("user", "", false)]
    [TestCase("user", "password", true)]
    public void SignInRequiresBothValues(string? username, string? password, bool valid) =>
        Assert.That(CredentialsValidator.HasSignInValues(username, password), Is.EqualTo(valid));

    [Test]
    public void HashRoundTripRejectsWrongPassword()
    {
        var (hash, salt) = PasswordHasher.Create(" correct password ");
        Assert.Multiple(() =>
        {
            Assert.That(PasswordHasher.Verify(" correct password ", hash, salt), Is.True);
            Assert.That(PasswordHasher.Verify("correct password", hash, salt), Is.False);
            Assert.That(PasswordHasher.Verify("wrong password", hash, salt), Is.False);
            Assert.That(Convert.FromBase64String(salt), Has.Length.EqualTo(16));
            Assert.That(Convert.FromBase64String(hash), Has.Length.EqualTo(32));
        });
    }

    [Test]
    public void IdenticalPasswordsReceiveDifferentSaltsAndHashes()
    {
        var first = PasswordHasher.Create("Password1");
        var second = PasswordHasher.Create("Password1");
        Assert.Multiple(() =>
        {
            Assert.That(first.Salt, Is.Not.EqualTo(second.Salt));
            Assert.That(first.Hash, Is.Not.EqualTo(second.Hash));
        });
    }

    [TestCase("not base64", "also invalid")]
    [TestCase("", "")]
    [TestCase("YQ==", "Yg==")]
    public void MalformedStoredCredentialsFailClosed(string hash, string salt) =>
        Assert.That(PasswordHasher.Verify("Password1", hash, salt), Is.False);
}
