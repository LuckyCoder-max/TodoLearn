namespace TodoLearn.Helpers
{
    public static class PasswordValidator
    {
        private const int MinPasswordLength = 8;

        public static (bool IsValid, string? ErrorMessage) Validate(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password cannot be empty");

            if (password.Length < MinPasswordLength)
                return (false, $"Password must be at least {MinPasswordLength} characters long");

            return (true, null);
        }
    }
}
