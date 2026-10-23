using TodoLearn.Resources.Strings;

namespace TodoLearn.Helpers;

public static class CredentialsValidator
{
    public const int MaxUsernameLength = 50;
    public const int MinPasswordLength = 8;

    public static string? ValidateRegistration(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username))
            return AppResources.UsernameRequired;

        if (username.Trim().Length > MaxUsernameLength)
            return string.Format(AppResources.UsernameTooLong, MaxUsernameLength);

        return ValidatePassword(password);
    }

    public static string? ValidatePassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return AppResources.PasswordRequired;

        if (password.Length < MinPasswordLength)
            return string.Format(AppResources.PasswordTooShort, MinPasswordLength);

        return null;
    }

    public static bool HasSignInValues(string? username, string? password) =>
        !string.IsNullOrWhiteSpace(username) && !string.IsNullOrEmpty(password);
}
