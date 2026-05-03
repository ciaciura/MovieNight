using ApiService.Features.Auth;

namespace ApiService.Endpoints.Modules;

public sealed class AuthEndpointModule : IEndpointModule
{
    public void Register(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth")
            .RequireAuthorization();

        AuthTokenCreate.Register(group);
        AuthMeGet.Register(group);
    }
}
