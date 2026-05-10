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

public sealed class AuthTokenCreate
{
    public static void Register(RouteGroupBuilder group)
    {
        group.MapPost("/token", Handle)
            .AllowAnonymous()
            .WithTags("Auth")
            .WithSummary("Initiate sign-in and receive a 2FA challenge")
            .WithDescription("""
                Step 1 of the two-factor sign-in flow.
                Looks up the user by display name and issues a short-lived challenge token (5 minutes).
                - Email method: a 6-digit OTP is generated and sent to the registered email address.
                - Authenticator method: no email is sent; the user opens their authenticator app.
                Complete sign-in by submitting the challenge token and the code to POST /api/auth/verify.
                No authentication is required to call this endpoint.
                """)
            .Produces<AuthTokenCreateResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    public static async Task<Results<Ok<AuthTokenCreateResponse>, ValidationProblem, ProblemHttpResult>> Handle(
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        ITwoFactorService twoFactorService,
        IEmailService emailService,
        AuthTokenCreateRequestValidator validator,
        AuthTokenCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());

        var normalizedDisplayName = UserExtensions.NormalizeDisplayName(request.DisplayName);

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NormalizedDisplayName == normalizedDisplayName, cancellationToken);

        if (user is null)
        {
            return TypedResults.Problem(
                title: "Invalid credentials",
                detail: "The provided credentials are invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!Enum.IsDefined(user.TwoFactorMethod))
        {
            return TypedResults.Problem(
                title: "Account setup incomplete",
                detail: "This account does not have a two-factor authentication method configured.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (user.TwoFactorMethod == TwoFactorMethod.Email)
        {
            // Remove any unconsumed OTPs for this user before issuing a new one.
            var stale = await dbContext.TwoFactorOtps
                .Where(o => o.UserId == user.Id && o.UsedAtUtc == null)
                .ToListAsync(cancellationToken);
            dbContext.TwoFactorOtps.RemoveRange(stale);

            var (code, codeHash) = twoFactorService.GenerateEmailOtp();
            dbContext.TwoFactorOtps.Add(new TwoFactorOtpModel
            {
                UserId = user.Id,
                CodeHash = codeHash,
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10)
            });
            await dbContext.SaveChangesAsync(cancellationToken);
            await emailService.SendOtpAsync(user.Email!, user.DisplayName, code, cancellationToken);
        }

        var challenge = jwtTokenService.CreateChallengeToken(user.Id, user.TwoFactorMethod);

        return TypedResults.Ok(new AuthTokenCreateResponse
        {
            ChallengeToken = challenge.ChallengeToken,
            TwoFactorMethod = user.TwoFactorMethod
        });
    }

    public sealed class AuthTokenCreateRequestValidator : AbstractValidator<AuthTokenCreateRequest>
    {
        public AuthTokenCreateRequestValidator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MaximumLength(80);
        }
    }
}