using FluentValidation;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MovieNight.Auth;
using Shared.Models.Views.Users.Requests;

namespace ApiService.Features.Users;

public sealed class UserDelete
{
    public static void Register(RouteGroupBuilder group)
    {
        group.MapDelete("/{id:int}", Handle)
            .RequireAuthorization(AdminApiKeyRequirement.PolicyName)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest);
    }

    public static async Task<Results<NoContent, NotFound, ValidationProblem>> Handle(
        AppDbContext dbContext,
        UserDeleteRequestValidator validator,
        int id,
        CancellationToken cancellationToken)
    {
        var request = new UserDeleteRequest { Id = id };
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
        if (user is null)
        {
            return TypedResults.NotFound();
        }

        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    public sealed class UserDeleteRequestValidator : AbstractValidator<UserDeleteRequest>
    {
        public UserDeleteRequestValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0);
        }
    }
}
