namespace TodoLearn.Models;

public sealed record SignInResult(bool IsAuthenticated, TimeSpan? LockoutRemaining = null)
{
    public bool IsLocked => LockoutRemaining.HasValue;
}
