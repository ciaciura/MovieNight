using ApiService.Features.Users;

namespace ApiService.Endpoints.Modules;

public sealed class UsersEndpointModule : IEndpointModule
{
    public void Register(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        UserGetAll.Register(group);
        UserGetById.Register(group);
        UserCreate.Register(group);
        UserDelete.Register(group);
    }
}
