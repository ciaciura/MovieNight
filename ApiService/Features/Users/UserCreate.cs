using FluentValidation;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions.Users;
using Shared.Models.Views.Users.Requests;
using Shared.Models.Views.Users.Responses;
using Infrastructure.Services;
using Shared.Models.Persistence;

namespace ApiService.Features.Users;

public sealed class UserCreate
{
    public static void Register(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
            .WithTags("Users")
            .WithSummary("Create a new user")
            .WithDescription("""
                Creates a new user with the given display name.
                Display names are canonicalized and must be unique (case-insensitive).
                The caller must choose a two-factor authentication method:
                  - Email (1): provide a valid email address; an OTP will be sent on each sign-in.
                  - Authenticator (2): an otpauth:// setup URI is returned for QR code scanning.
                Returns 409 if a user with the same normalized name already exists.
                Requires a valid JWT bearer token.
                """)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    public static async Task<Results<Created<UserCreateResponse>, ValidationProblem, Conflict<ProblemDetails>>> Handle(
        AppDbContext dbContext,
        ITwoFactorService twoFactorService,
        UserCreateRequestValidator validator,
        UserCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request,cancellationToken);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var normalizedDisplayName = UserExtensions.NormalizeDisplayName(request.DisplayName);
        var canonicalDisplayName = UserExtensions.CanonicalizeDisplayName(request.DisplayName);

        var duplicate = await dbContext.Users.AnyAsync(
            user => user.NormalizedDisplayName == normalizedDisplayName,
            cancellationToken);

        if (duplicate)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "User already exists",
                Detail = $"A user named '{canonicalDisplayName}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        string? totpSecret = null;
        if (request.TwoFactorMethod == TwoFactorMethod.Authenticator)
            totpSecret = twoFactorService.GenerateTotpSecret();

        var toCreate = new UserModel
        {
            DisplayName = canonicalDisplayName,
            NormalizedDisplayName = normalizedDisplayName,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            TwoFactorMethod = request.TwoFactorMethod,
            Email = request.Email?.Trim().ToLowerInvariant(),
            TotpSecret = totpSecret
        };

        dbContext.Users.Add(toCreate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = UserCreateResponse.MapToView(toCreate);
        if (totpSecret is not null)
            response.TotpSetupUri = twoFactorService.GetTotpSetupUri("MovieNight", toCreate.DisplayName, totpSecret);

        return TypedResults.Created($"/api/users/{response.Id}", response);
    }

    public sealed class UserCreateRequestValidator : AbstractValidator<UserCreateRequest>
    {
        public UserCreateRequestValidator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MaximumLength(80);

            RuleFor(x => x.TwoFactorMethod)
                .IsInEnum()
                .Must(m => m == TwoFactorMethod.Email || m == TwoFactorMethod.Authenticator)
                .WithMessage("TwoFactorMethod must be Email (1) or Authenticator (2).");

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress()
                .MaximumLength(320)
                .When(x => x.TwoFactorMethod == TwoFactorMethod.Email);
        }
    }
}
