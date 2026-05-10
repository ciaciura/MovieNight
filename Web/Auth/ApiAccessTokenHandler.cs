using System.Net.Http.Headers;

namespace MovieNight.Web.Auth;

public sealed class ApiAccessTokenHandler(AuthSession session) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (session.IsAuthenticated && !string.IsNullOrWhiteSpace(session.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        }

        return base.SendAsync(request, cancellationToken);
    }
}