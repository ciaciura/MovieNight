namespace Shared.Models.Persistence;

public sealed class UserModel
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string NormalizedDisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public TwoFactorMethod TwoFactorMethod { get; set; }
    public string? Email { get; set; }
    public string? TotpSecret { get; set; }

    public ICollection<TwoFactorOtpModel> TwoFactorOtps { get; set; } = [];
}
