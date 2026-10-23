namespace TodoLearn.Services;

public interface IGoogleSignInService
{
    Task<GoogleAccountIdentity> SignInAsync(CancellationToken cancellationToken = default);
}

public sealed record GoogleAccountIdentity(string Subject, string Email);

public sealed class GoogleSignInCancelledException : Exception
{
}

public sealed class GoogleClientConfigurationException : Exception
{
}
