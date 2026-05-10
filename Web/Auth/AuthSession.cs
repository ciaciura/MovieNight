using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Shared.Models.Persistence;
using Shared.Models.Views.Auth.Responses;

namespace MovieNight.Web.Auth;

public sealed class AuthSession
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    public ClaimsPrincipal Principal { get; private set; } = Anonymous;
    public string? AccessToken { get; private set; }
    public DateTimeOffset? ExpiresAtUtc { get; private set; }

    // 2FA Challenge state (intermediate, not authenticated)
    public string? ChallengeToken { get; private set; }
    public TwoFactorMethod? ChallengeTwoFactorMethod { get; private set; }

    public bool IsExpired => ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc <= DateTimeOffset.UtcNow;

    public bool IsAuthenticated =>
        Principal.Identity?.IsAuthenticated is true &&
        !string.IsNullOrWhiteSpace(AccessToken) &&
        !IsExpired;

    public bool HasChallenge => !string.IsNullOrWhiteSpace(ChallengeToken);

    public void SetChallenge(string challengeToken, TwoFactorMethod twoFactorMethod)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(challengeToken);
        ChallengeToken = challengeToken;
        ChallengeTwoFactorMethod = twoFactorMethod;
    }

    public void SetFromVerify(AuthVerifyResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        AccessToken = response.AccessToken;
        ExpiresAtUtc = response.ExpiresAtUtc;
        Principal = CreatePrincipal(response.AccessToken);
        ChallengeToken = null;
        ChallengeTwoFactorMethod = null;
    }

    public void Clear()
    {
        AccessToken = null;
        ExpiresAtUtc = null;
        Principal = Anonymous;
        ChallengeToken = null;
        ChallengeTwoFactorMethod = null;
    }

    private static ClaimsPrincipal CreatePrincipal(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var identity = new ClaimsIdentity(jwt.Claims, authenticationType: "MovieNightJwt", nameType: ClaimTypes.Name, roleType: ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}