using Microsoft.AspNetCore.Authorization;

namespace MovieNight.Auth;

public sealed class AdminApiKeyRequirement : IAuthorizationRequirement
{
    public const string PolicyName = "AdminApiKey";
    public const string HeaderName = "X-Api-Key";
}
