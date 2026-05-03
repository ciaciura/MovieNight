using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.HttpResults;
using Shared.Extensions.Users;
using Shared.Models.Views.Auth.Responses;

namespace ApiService.Features.Auth;

public sealed class AuthMeGet
{
    public static void Register(RouteGroupBuilder group)
    {
        group.MapGet("/me", Handle)
            .Produces<AuthMeResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
    }

    public static Results<Ok<AuthMeResponse>, ProblemHttpResult> Handle(HttpContext httpContext)
    {
        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated is not true)
        {
            return TypedResults.Problem(
                title: "Unauthorized",
                detail: "A valid bearer token is required.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var displayName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        var isAdmin = string.Equals(
            user.FindFirstValue(UserClaimTypes.Admin),
            bool.TrueString,
            StringComparison.OrdinalIgnoreCase);

        if (!int.TryParse(userIdClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var userId) ||
            string.IsNullOrWhiteSpace(displayName))
        {
            return TypedResults.Problem(
                title: "Unauthorized",
                detail: "The bearer token did not contain required identity claims.",
                statusCode: StatusCodes.Status401Unauthorized);
        }

        return TypedResults.Ok(new AuthMeResponse
        {
            UserId = userId,
            DisplayName = displayName,
            IsAdmin = isAdmin
        });
    }
}