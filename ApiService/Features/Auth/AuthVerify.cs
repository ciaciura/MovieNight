using FluentValidation;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MovieNight.Auth;
using Shared.Extensions.Users;
using Shared.Models.Persistence;
using Shared.Models.Views.Auth.Requests;
using Shared.Models.Views.Auth.Responses;

namespace ApiService.Features.Auth;

public sealed class AuthVerify
{
    public static void Register(RouteGroupBuilder group)
    {
        group.MapPost("/verify", Handle)
            .AllowAnonymous()
            .WithTags("Auth")
            .WithSummary("Verify a 2FA code and receive an access token")
            .WithDescription("""
                Step 2 of the two-factor sign-in flow.
                Accepts the challenge token issued by POST /api/auth/token and the user's 2FA code.
                - Email method: submit the 6-digit OTP sent to the registered email (valid for 10 minutes, single-use).
                - Authenticator method: submit the current 6-digit TOTP code from Google/Microsoft Authenticator.
                On success, returns a signed JWT bearer token for API access.
                No authentication is required to call this endpoint.
                """)
            .Produces<AuthVerifyResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    public static async Task<Results<Ok<AuthVerifyResponse>, ValidationProblem, ProblemHttpResult>> Handle(
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        ITwoFactorService twoFactorService,
        IConfiguration configuration,
        AuthVerifyRequestValidator validator,
        AuthVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        var claims = jwtTokenService.ValidateChallengeToken(request.ChallengeToken);
        if (claims is null)
        {
            return TypedResults.Problem(
                title: "Invalid challenge token",
                detail: "The challenge token is missing, expired, or invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == claims.UserId, cancellationToken);

        if (user is null)
        {
            return TypedResults.Problem(
                title: "Invalid credentials",
                detail: "The user associated with this challenge no longer exists.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        bool codeValid;

        if (claims.TwoFactorMethod == TwoFactorMethod.Authenticator)
        {
            codeValid = !string.IsNullOrWhiteSpace(user.TotpSecret)
                        && twoFactorService.VerifyTotp(user.TotpSecret, request.Code);
        }
        else // Email
        {
            var otp = await dbContext.TwoFactorOtps
                .Where(o => o.UserId == user.Id
                            && o.UsedAtUtc == null
                            && o.ExpiresAtUtc > DateTimeOffset.UtcNow)
                .OrderByDescending(o => o.ExpiresAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (otp is null)
            {
                return TypedResults.Problem(
                    title: "Code expired",
                    detail: "No valid one-time code found. Please restart the sign-in process.",
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            codeValid = twoFactorService.VerifyEmailOtpHash(request.Code, otp.CodeHash);

            if (codeValid)
            {
                otp.UsedAtUtc = DateTimeOffset.UtcNow;
                dbContext.TwoFactorOtps.Update(otp);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        if (!codeValid)
        {
            return TypedResults.Problem(
                title: "Invalid code",
                detail: "The verification code is incorrect.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var configuredAdminUsers = configuration.GetSection("Authentication:AdminUsers").Get<string[]>() ?? [];
        var adminSet = configuredAdminUsers
            .Select(UserExtensions.NormalizeDisplayName)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .ToHashSet(StringComparer.Ordinal);

        var isAdmin = adminSet.Contains(user.NormalizedDisplayName);
        var token = jwtTokenService.CreateToken(user, isAdmin);

        return TypedResults.Ok(new AuthVerifyResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            IsAdmin = isAdmin,
            AccessToken = token.AccessToken,
            TokenType = "Bearer",
            ExpiresAtUtc = token.ExpiresAtUtc
        });
    }

    public sealed class AuthVerifyRequestValidator : AbstractValidator<AuthVerifyRequest>
    {
        public AuthVerifyRequestValidator()
        {
            RuleFor(x => x.ChallengeToken).NotEmpty();
            RuleFor(x => x.Code)
                .NotEmpty()
                .Length(6)
                .Matches(@"^\d{6}$")
                .WithMessage("Code must be exactly 6 digits.");
        }
    }
}
