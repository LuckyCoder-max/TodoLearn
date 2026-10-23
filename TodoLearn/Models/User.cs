using TodoLearn.Services;

namespace TodoLearn.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User() { }

        public User(string username, string password)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(username);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            Username = username;
            SetPassword(password);
        }

        public void SetPassword(string plainPassword)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);

            var (hash, salt) = PasswordHasher.Create(plainPassword);
            PasswordHash = $"{salt}:{hash}";
        }

        public bool VerifyPassword(string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(plainPassword))
                return false;

            try
            {
                var parts = PasswordHash.Split(':', 2);
                if (parts.Length != 2)
                    return false;

                return PasswordHasher.Verify(plainPassword, parts[1], parts[0]);
            }
            catch
            {
                return false;
            }
        }
    }
}
