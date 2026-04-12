using Microsoft.AspNetCore.Authorization;

namespace MovieNight.Auth;

public sealed class AdminApiKeyHandler : AuthorizationHandler<AdminApiKeyRequirement>
{
    private readonly IConfiguration _configuration;

    public AdminApiKeyHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AdminApiKeyRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
        {
            return Task.CompletedTask;
        }

        var configuredApiKey = _configuration["Authentication:AdminApiKey"];
        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            return Task.CompletedTask;
        }

        if (!httpContext.Request.Headers.TryGetValue(AdminApiKeyRequirement.HeaderName, out var providedApiKey))
        {
            return Task.CompletedTask;
        }

        if (string.Equals(providedApiKey.ToString(), configuredApiKey, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
