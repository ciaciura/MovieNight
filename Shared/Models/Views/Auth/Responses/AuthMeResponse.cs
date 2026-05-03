using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views.Auth.Responses;

public sealed class AuthMeResponse : FrontViewBase<AuthMeResponse, UserModel>
{
    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }

    protected override void ConfigureMappings(MappingRegistry<AuthMeResponse, UserModel> registry)
    {
        registry
            .AddToModel(DefaultMappingName, view => new UserModel
            {
                Id = view.UserId,
                DisplayName = view.DisplayName,
                NormalizedDisplayName = view.DisplayName.Trim().ToUpperInvariant()
            })
            .AddToView(DefaultMappingName, model => new AuthMeResponse
            {
                UserId = model.Id,
                DisplayName = model.DisplayName
            });
    }
}