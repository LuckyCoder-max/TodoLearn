namespace TodoLearn.Models;

public sealed class ExternalLogin
{
    public int Id { get; set; }

    public int UserAccountId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public UserAccount UserAccount { get; set; } = null!;
}
