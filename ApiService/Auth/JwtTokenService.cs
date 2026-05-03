using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Shared.Extensions.Users;
using Shared.Models.Persistence;

namespace MovieNight.Auth;

public interface IJwtTokenService
{
    AuthTokenResult CreateToken(UserModel user, bool isAdmin);
}

public sealed record AuthTokenResult(string AccessToken, DateTimeOffset ExpiresAtUtc);

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public AuthTokenResult CreateToken(UserModel user, bool isAdmin)
    {
        ArgumentNullException.ThrowIfNull(user);

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

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(signingKey)),
            SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: issuedAtUtc.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: credentials);

        var encodedToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);
        return new AuthTokenResult(encodedToken, expiresAtUtc);
    }
}