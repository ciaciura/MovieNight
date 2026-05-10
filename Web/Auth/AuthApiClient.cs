using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Views.Auth.Requests;
using Shared.Models.Views.Auth.Responses;

namespace MovieNight.Web.Auth;

public sealed class AuthApiClient(IHttpClientFactory httpClientFactory)
{
    public const string AnonymousClientName = "MovieNightApiAnonymous";
    public const string AuthenticatedClientName = "MovieNightApiAuthenticated";

    /// <summary>Step 1: Request a 2FA challenge token.</summary>
    public async Task<ChallengeRequestResult> RequestChallengeAsync(string displayName, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(AnonymousClientName);
        using var response = await client.PostAsJsonAsync(
            "/api/auth/token",
            new AuthTokenCreateRequest { DisplayName = displayName },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var challengeResponse = await response.Content.ReadFromJsonAsync<AuthTokenCreateResponse>(cancellationToken: cancellationToken);
            return challengeResponse is null
                ? ChallengeRequestResult.Failure("The challenge response was empty.")
                : ChallengeRequestResult.Success(challengeResponse);
        }

        return await CreateFailureResultAsync<ChallengeRequestResult>(response, cancellationToken);
    }

    /// <summary>Step 2: Verify the 2FA code and receive an access token.</summary>
    public async Task<VerifyRequestResult> VerifyAsync(string challengeToken, string code, CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(AnonymousClientName);
        using var response = await client.PostAsJsonAsync(
            "/api/auth/verify",
            new AuthVerifyRequest { ChallengeToken = challengeToken, Code = code },
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var verifyResponse = await response.Content.ReadFromJsonAsync<AuthVerifyResponse>(cancellationToken: cancellationToken);
            return verifyResponse is null
                ? VerifyRequestResult.Failure("The verification response was empty.")
                : VerifyRequestResult.Success(verifyResponse);
        }

        return await CreateFailureResultAsync<VerifyRequestResult>(response, cancellationToken);
    }

    public async Task<AuthMeResponse?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var client = httpClientFactory.CreateClient(AuthenticatedClientName);
        using var response = await client.GetAsync("/api/auth/me", cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthMeResponse>(cancellationToken: cancellationToken);
    }

    private static async Task<T> CreateFailureResultAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
        where T : class
    {
        var errorMessage = "The request failed.";

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var validationProblem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: cancellationToken);
            var firstError = validationProblem?.Errors
                .SelectMany(entry => entry.Value)
                .FirstOrDefault(error => !string.IsNullOrWhiteSpace(error));

            errorMessage = firstError ?? "The request was invalid.";
        }
        else if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            errorMessage = "The provided credentials were invalid.";
        }
        else
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: cancellationToken);
            errorMessage = problem?.Detail ?? problem?.Title ?? "The request failed.";
        }

        if (typeof(T) == typeof(ChallengeRequestResult))
            return (ChallengeRequestResult.Failure(errorMessage) as T)!;

        if (typeof(T) == typeof(VerifyRequestResult))
            return (VerifyRequestResult.Failure(errorMessage) as T)!;

        throw new InvalidOperationException($"Unsupported result type: {typeof(T).Name}");
    }
}

public sealed record ChallengeRequestResult(bool Succeeded, AuthTokenCreateResponse? Response, string? ErrorMessage)
{
    public static ChallengeRequestResult Success(AuthTokenCreateResponse response) => new(true, response, null);
    public static ChallengeRequestResult Failure(string errorMessage) => new(false, null, errorMessage);
}

public sealed record VerifyRequestResult(bool Succeeded, AuthVerifyResponse? Response, string? ErrorMessage)
{
    public static VerifyRequestResult Success(AuthVerifyResponse response) => new(true, response, null);
    public static VerifyRequestResult Failure(string errorMessage) => new(false, null, errorMessage);
}