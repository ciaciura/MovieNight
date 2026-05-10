namespace Shared.Models.Views.Auth.Requests;

public sealed class AuthVerifyRequest
{
    public string ChallengeToken { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
