namespace ApiService.Endpoints.Modules;

public static class EndpointRegistrationExtensions
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder app)
    {
        IEndpointModule[] modules =
        [
            new AuthEndpointModule(),
            new UsersEndpointModule()
        ];

        foreach (var module in modules)
        {
            module.Register(app);
        }

        return app;
    }
}
