using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shared.Models.Persistence;
using Shared.Models.Views.Auth.Requests;
using Shared.Models.Views.Auth.Responses;
using Shared.Models.Views.Users.Requests;
using Shared.Models.Views.Users.Responses;
using Xunit;

namespace ApiService.IntegrationTests;

public sealed class AuthAndUsersIntegrationTests
{
    [Fact]
    public async Task Register_AuthenticatorUser_ReturnsCreatedWithTotpUri()
    {
        using var factory = new ApiServiceWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new UserCreateRequest
        {
            DisplayName = "new-user-auth",
            TwoFactorMethod = TwoFactorMethod.Authenticator
        };

        using var response = await client.PostAsJsonAsync("/api/users", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<UserCreateResponse>();
        Assert.NotNull(created);
        Assert.True(created.Id > 0);
        Assert.Equal("new-user-auth", created.DisplayName);
        Assert.Equal(TwoFactorMethod.Authenticator, created.TwoFactorMethod);
        Assert.False(string.IsNullOrWhiteSpace(created.TotpSetupUri));
        Assert.StartsWith("otpauth://totp/", created.TotpSetupUri, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_DuplicateDisplayName_ReturnsConflict()
    {
        using var factory = new ApiServiceWebApplicationFactory();
        using var client = factory.CreateClient();

        var firstRequest = new UserCreateRequest
        {
            DisplayName = "dupe-user",
            TwoFactorMethod = TwoFactorMethod.Email,
            Email = "dupe1@example.com"
        };

        using var firstResponse = await client.PostAsJsonAsync("/api/users", firstRequest);
        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateRequest = new UserCreateRequest
        {
            DisplayName = "DUPE-USER",
            TwoFactorMethod = TwoFactorMethod.Email,
            Email = "dupe2@example.com"
        };

        using var duplicateResponse = await client.PostAsJsonAsync("/api/users", duplicateRequest);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithoutBearerToken_ReturnsUnauthorized()
    {
        using var factory = new ApiServiceWebApplicationFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AuthenticatorFlow_TokenThenVerifyThenMe_Succeeds()
    {
        using var factory = new ApiServiceWebApplicationFactory();
        using var client = factory.CreateClient();

        var registerRequest = new UserCreateRequest
        {
            DisplayName = "signin-auth-user",
            TwoFactorMethod = TwoFactorMethod.Authenticator
        };

        using var registerResponse = await client.PostAsJsonAsync("/api/users", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        using var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new AuthTokenCreateRequest
        {
            DisplayName = "signin-auth-user"
        });

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<AuthTokenCreateResponse>();
        Assert.NotNull(tokenPayload);
        Assert.False(string.IsNullOrWhiteSpace(tokenPayload.ChallengeToken));
        Assert.Equal(TwoFactorMethod.Authenticator, tokenPayload.TwoFactorMethod);

        using var verifyResponse = await client.PostAsJsonAsync("/api/auth/verify", new AuthVerifyRequest
        {
            ChallengeToken = tokenPayload.ChallengeToken,
            Code = "123456"
        });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);

        var verifyPayload = await verifyResponse.Content.ReadFromJsonAsync<AuthVerifyResponse>();
        Assert.NotNull(verifyPayload);
        Assert.False(string.IsNullOrWhiteSpace(verifyPayload.AccessToken));

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", verifyPayload.AccessToken);
        using var meResponse = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);

        var mePayload = await meResponse.Content.ReadFromJsonAsync<AuthMeResponse>();
        Assert.NotNull(mePayload);
        Assert.Equal("signin-auth-user", mePayload.DisplayName);
        Assert.False(mePayload.IsAdmin);
    }

    [Fact]
    public async Task Verify_WithWrongCode_ReturnsUnauthorized()
    {
        using var factory = new ApiServiceWebApplicationFactory();
        using var client = factory.CreateClient();

        var registerRequest = new UserCreateRequest
        {
            DisplayName = "invalid-code-user",
            TwoFactorMethod = TwoFactorMethod.Authenticator
        };

        using var registerResponse = await client.PostAsJsonAsync("/api/users", registerRequest);
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        using var tokenResponse = await client.PostAsJsonAsync("/api/auth/token", new AuthTokenCreateRequest
        {
            DisplayName = "invalid-code-user"
        });

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<AuthTokenCreateResponse>();
        Assert.NotNull(tokenPayload);

        using var verifyResponse = await client.PostAsJsonAsync("/api/auth/verify", new AuthVerifyRequest
        {
            ChallengeToken = tokenPayload.ChallengeToken,
            Code = "000000"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, verifyResponse.StatusCode);
    }
}
