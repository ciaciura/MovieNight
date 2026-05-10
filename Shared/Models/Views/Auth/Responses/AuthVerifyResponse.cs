namespace Shared.Models.Views.Auth.Responses;

/// <summary>
/// Returned by POST /api/auth/verify once the 2FA code is validated.
/// Contains the full JWT bearer token for API access.
/// </summary>
public sealed class AuthVerifyResponse
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
