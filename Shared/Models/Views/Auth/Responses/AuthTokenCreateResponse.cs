using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views.Auth.Responses;

public sealed class AuthTokenCreateResponse : FrontViewBase<AuthTokenCreateResponse, UserModel>
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTimeOffset ExpiresAtUtc { get; set; }

    protected override void ConfigureMappings(MappingRegistry<AuthTokenCreateResponse, UserModel> registry)
    {
        registry
            .AddToModel(DefaultMappingName, view => new UserModel
            {
                Id = view.UserId,
                DisplayName = view.DisplayName,
                NormalizedDisplayName = view.DisplayName.Trim().ToUpperInvariant()
            })
            .AddToView(DefaultMappingName, model => new AuthTokenCreateResponse
            {
                UserId = model.Id,
                DisplayName = model.DisplayName
            });
    }
}