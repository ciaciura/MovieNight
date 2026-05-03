using FluentValidation;
using Infrastructure.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions.Users;
using Shared.Models.Views.Users.Requests;
using Shared.Models.Views.Users.Responses;

namespace ApiService.Features.Users;

public sealed class UserCreate
{
    public static void Register(RouteGroupBuilder group)
    {
        group.MapPost("/", Handle)
            .Produces(StatusCodes.Status201Created)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    public static async Task<Results<Created<UserCreateResponse>, ValidationProblem, Conflict<ProblemDetails>>> Handle(
        AppDbContext dbContext,
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

        var toCreate = UserCreateRequest.MapToModel(new UserCreateRequest
        {
            DisplayName = canonicalDisplayName
        });

        dbContext.Users.Add(toCreate);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = UserCreateResponse.MapToView(toCreate);
        return TypedResults.Created($"/api/users/{response.Id}", response);
    }

    public sealed class UserCreateRequestValidator : AbstractValidator<UserCreateRequest>
    {
        public UserCreateRequestValidator()
        {
            RuleFor(x => x.DisplayName)
                .NotEmpty()
                .MaximumLength(80);
        }
    }
}
