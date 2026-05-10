using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Shared.Extensions.Users;
using Shared.Models.Persistence;

namespace MovieNight.Auth;

public sealed record AuthTokenResult(string AccessToken, DateTimeOffset ExpiresAtUtc);
public sealed record ChallengeTokenResult(string ChallengeToken, DateTimeOffset ExpiresAtUtc);
public sealed record ChallengeTokenClaims(int UserId, TwoFactorMethod TwoFactorMethod);

public interface IJwtTokenService
{
    AuthTokenResult CreateToken(UserModel user, bool isAdmin);
    ChallengeTokenResult CreateChallengeToken(int userId, TwoFactorMethod twoFactorMethod);
    ChallengeTokenClaims? ValidateChallengeToken(string token);
}

public sealed class JwtTokenService : IJwtTokenService
{
    private const string ChallengePurpose = "2fa_challenge";
    private const string PurposeClaim = "purpose";
    private const string TfaMethodClaim = "tfa_method";

    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AuthTokenResult CreateToken(UserModel user, bool isAdmin)
    {
        ArgumentNullException.ThrowIfNull(user);

        var (issuer, audience, signingKey) = ReadJwtSettings();
        var tokenLifetimeMinutes = _configuration.GetValue<int?>("Authentication:Jwt:TokenLifetimeMinutes") ?? 60;
        var issuedAtUtc = DateTimeOffset.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(Math.Max(1, tokenLifetimeMinutes));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.DisplayName),
            new(JwtRegisteredClaimNames.UniqueName, user.DisplayName),
            new(UserClaimTypes.Admin, isAdmin ? bool.TrueString : bool.FalseString)
        };

        var token = BuildToken(issuer, audience, signingKey, claims, issuedAtUtc, expiresAtUtc);
        return new AuthTokenResult(token, expiresAtUtc);
    }

    public ChallengeTokenResult CreateChallengeToken(int userId, TwoFactorMethod twoFactorMethod)
    {
        var (issuer, audience, signingKey) = ReadJwtSettings();
        var issuedAtUtc = DateTimeOffset.UtcNow;
        var expiresAtUtc = issuedAtUtc.AddMinutes(5);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString(CultureInfo.InvariantCulture)),
            new Claim(PurposeClaim, ChallengePurpose),
            new Claim(TfaMethodClaim, ((int)twoFactorMethod).ToString(CultureInfo.InvariantCulture))
        };

        var token = BuildToken(issuer, audience, signingKey, claims, issuedAtUtc, expiresAtUtc);
        return new ChallengeTokenResult(token, expiresAtUtc);
    }

    public ChallengeTokenClaims? ValidateChallengeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var (issuer, audience, signingKey) = ReadJwtSettings();

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, validationParameters, out _);

            if (principal.FindFirstValue(PurposeClaim) != ChallengePurpose)
                return null;

            var sub = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);

            if (!int.TryParse(sub, out var userId))
                return null;

            var methodRaw = principal.FindFirstValue(TfaMethodClaim);
            if (!int.TryParse(methodRaw, out var methodInt) || !Enum.IsDefined(typeof(TwoFactorMethod), methodInt))
                return null;

            return new ChallengeTokenClaims(userId, (TwoFactorMethod)methodInt);
        }
        catch
        {
            return null;
        }
    }

    private (string Issuer, string Audience, string SigningKey) ReadJwtSettings()
    {
        var jwtSection = _configuration.GetSection("Authentication:Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var signingKey = jwtSection["SigningKey"];

        if (string.IsNullOrWhiteSpace(issuer) ||
            string.IsNullOrWhiteSpace(audience) ||
            string.IsNullOrWhiteSpace(signingKey))
        {
            throw new InvalidOperationException("JWT settings are missing in Authentication:Jwt.");
        }

        return (issuer, audience, signingKey);
    }

    private static string BuildToken(
        string issuer, string audience, string signingKey,
        IEnumerable<Claim> claims, DateTimeOffset notBefore, DateTimeOffset expires)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: notBefore.UtcDateTime,
            expires: expires.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(jwtToken);
    }
}