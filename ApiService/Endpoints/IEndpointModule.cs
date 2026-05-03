namespace ApiService.Endpoints;

public interface IEndpointModule
{
    void Register(IEndpointRouteBuilder app);
}