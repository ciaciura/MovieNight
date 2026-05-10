namespace Shared.Models.Persistence;

public sealed class TwoFactorOtpModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? UsedAtUtc { get; set; }

    public UserModel User { get; set; } = null!;
}
