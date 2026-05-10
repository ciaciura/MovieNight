using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views.Auth.Responses;

/// <summary>
/// Returned by POST /api/auth/token. Contains a short-lived challenge token and the
/// 2FA method the client must use to complete sign-in via POST /api/auth/verify.
/// </summary>
public sealed class AuthTokenCreateResponse : FrontViewBase<AuthTokenCreateResponse, UserModel>
{
    public string ChallengeToken { get; set; } = string.Empty;
    public TwoFactorMethod TwoFactorMethod { get; set; }

    protected override void ConfigureMappings(MappingRegistry<AuthTokenCreateResponse, UserModel> registry)
    {
        registry
            .AddToModel(DefaultMappingName, view => new UserModel
            {
                TwoFactorMethod = view.TwoFactorMethod
            })
            .AddToView(DefaultMappingName, model => new AuthTokenCreateResponse
            {
                TwoFactorMethod = model.TwoFactorMethod
            });
    }
}