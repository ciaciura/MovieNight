using Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Views.Users.Responses;

namespace ApiService.Features.Users;

public sealed class UserGetAll
{
    public static void Register(RouteGroupBuilder group)
    {
        group.MapGet("/", Handle)
            .WithTags("Users")
            .WithSummary("List all users")
            .WithDescription("""
                Returns all registered users ordered alphabetically by display name.
                Requires a valid JWT bearer token.
                """)
            .Produces<IReadOnlyList<UserGetAllResponse>>(StatusCodes.Status200OK);
    }

    public static async Task<Ok<List<UserGetAllResponse>>> Handle(
        AppDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .AsNoTracking()
            .OrderBy(user => user.DisplayName)
            .ToListAsync(cancellationToken);

        var response = users
            .Select(model => UserGetAllResponse.MapToView(model))
            .ToList();

        return TypedResults.Ok(response);
    }
}
