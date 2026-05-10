using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views.Users.Responses;

public sealed class UserCreateResponse : FrontViewBase<UserCreateResponse, UserModel>
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public TwoFactorMethod TwoFactorMethod { get; set; }
    /// <summary>
    /// Only populated for Authenticator method. Scan with Google/Microsoft Authenticator to set up TOTP.
    /// </summary>
    public string? TotpSetupUri { get; set; }

    protected override void ConfigureMappings(MappingRegistry<UserCreateResponse, UserModel> registry)
    {
        registry
            .AddToModel(DefaultMappingName, view => new UserModel
            {
                Id = view.Id,
                DisplayName = view.DisplayName,
                NormalizedDisplayName = view.DisplayName.Trim().ToUpperInvariant(),
                CreatedAtUtc = DateTimeOffset.UtcNow,
                TwoFactorMethod = view.TwoFactorMethod
            })
            .AddToView(DefaultMappingName, model => new UserCreateResponse
            {
                Id = model.Id,
                DisplayName = model.DisplayName,
                TwoFactorMethod = model.TwoFactorMethod
            });
    }
}