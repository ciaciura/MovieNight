using FluentValidation;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MovieNight.Auth;
using Shared.Extensions.Users;
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
            .WithSummary("Request a JWT access token")
            .WithDescription("""
                Authenticates a user by display name and returns a signed JWT bearer token.
                The token includes identity claims and, where applicable, an admin claim.
                No authentication is required to call this endpoint.
                """)
            .Produces<AuthTokenCreateResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    public static async Task<Results<Ok<AuthTokenCreateResponse>, ValidationProblem, ProblemHttpResult>> Handle(
        AppDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IConfiguration configuration,
        AuthTokenCreateRequestValidator validator,
        AuthTokenCreateRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var normalizedDisplayName = UserExtensions.NormalizeDisplayName(request.DisplayName);

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                model => model.NormalizedDisplayName == normalizedDisplayName,
                cancellationToken);

        if (user is null)
        {
            return TypedResults.Problem(
                title: "Invalid credentials",
                detail: "The provided credentials are invalid.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var configuredAdminUsers = configuration.GetSection("Authentication:AdminUsers").Get<string[]>() ?? [];
        var adminSet = configuredAdminUsers
            .Select(UserExtensions.NormalizeDisplayName)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.Ordinal);

        var isAdmin = adminSet.Contains(user.NormalizedDisplayName);
        var token = jwtTokenService.CreateToken(user, isAdmin);

        return TypedResults.Ok(new AuthTokenCreateResponse
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            IsAdmin = isAdmin,
            AccessToken = token.AccessToken,
            TokenType = "Bearer",
            ExpiresAtUtc = token.ExpiresAtUtc
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