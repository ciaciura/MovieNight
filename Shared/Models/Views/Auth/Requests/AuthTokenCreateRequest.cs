using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views.Auth.Requests;

public sealed class AuthTokenCreateRequest : FrontViewBase<AuthTokenCreateRequest, UserModel>
{
    public string DisplayName { get; set; } = string.Empty;

    protected override void ConfigureMappings(MappingRegistry<AuthTokenCreateRequest, UserModel> registry)
    {
        registry
            .AddToModel(DefaultMappingName, view => new UserModel
            {
                DisplayName = view.DisplayName,
                NormalizedDisplayName = view.DisplayName.Trim().ToUpperInvariant()
            })
            .AddToView(DefaultMappingName, model => new AuthTokenCreateRequest
            {
                DisplayName = model.DisplayName
            });
    }
}