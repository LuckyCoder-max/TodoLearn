using System.Security.Cryptography;
using System.Text;

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

            var saltBytes = RandomNumberGenerator.GetBytes(16);
            var salt = Convert.ToBase64String(saltBytes);

            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(salt + plainPassword));
            var hash = Convert.ToBase64String(hashBytes);

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

                var salt = parts[0];
                var storedHash = parts[1];

                var computedHashBytes = SHA256.HashData(
                    Encoding.UTF8.GetBytes(salt + plainPassword));
                var computedHash = Convert.ToBase64String(computedHashBytes);

                return computedHash == storedHash;
            }
            catch
            {
                return false;
            }
        }
    }
}
