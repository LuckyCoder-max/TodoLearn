namespace TodoLearn.Helpers
{
    public static class PasswordValidator
    {
        public const int MinPasswordLength = CredentialsValidator.MinPasswordLength;

        public static (bool IsValid, string? ErrorMessage) Validate(string? password)
        {
            var error = CredentialsValidator.ValidatePassword(password);
            return (error is null, error);
        }
    }
}
