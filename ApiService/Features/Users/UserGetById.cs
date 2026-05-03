using FluentValidation;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Shared.Models.Views.Users.Requests;
using Shared.Models.Views.Users.Responses;

namespace ApiService.Features.Users;

public sealed class UserGetById
{
    public static void Register(RouteGroupBuilder group)
    {
        group.MapGet("/{id:int}", Handle)
            .WithTags("Users")
            .WithSummary("Get a user by ID")
            .WithDescription("""
                Returns the user with the specified ID.
                Returns 404 if no user with that ID exists.
                Requires a valid JWT bearer token.
                """)
            .Produces<UserGetByIdResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);
    }

    public static async Task<Results<Ok<UserGetByIdResponse>, NotFound, ValidationProblem>> Handle(
        AppDbContext dbContext,
        UserGetByIdRequestValidator validator,
        int id,
        CancellationToken cancellationToken)
    {
        var request = new UserGetByIdRequest { Id = id };
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        return user is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(UserGetByIdResponse.MapToView(user));
    }

    public sealed class UserGetByIdRequestValidator : AbstractValidator<UserGetByIdRequest>
    {
        public UserGetByIdRequestValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
