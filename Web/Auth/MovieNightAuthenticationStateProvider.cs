using Microsoft.AspNetCore.Components.Authorization;
using Shared.Models.Views.Auth.Responses;

namespace MovieNight.Web.Auth;

public sealed class MovieNightAuthenticationStateProvider(AuthSession session) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous = new(new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity()));

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!session.IsAuthenticated)
        {
            if (session.AccessToken is not null)
            {
                session.Clear();
            }

            return Task.FromResult(Anonymous);
        }

        return Task.FromResult(new AuthenticationState(session.Principal));
    }

    public Task SetChallengeAsync(string challengeToken, Shared.Models.Persistence.TwoFactorMethod twoFactorMethod)
    {
        session.SetChallenge(challengeToken, twoFactorMethod);
        return Task.CompletedTask;
    }

    public Task SignInAsync(AuthVerifyResponse response)
    {
        session.SetFromVerify(response);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        return Task.CompletedTask;
    }

    public Task SignOutAsync()
    {
        session.Clear();
        NotifyAuthenticationStateChanged(Task.FromResult(Anonymous));
        return Task.CompletedTask;
    }
}