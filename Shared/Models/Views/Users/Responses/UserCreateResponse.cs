using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views.Users.Responses;

public sealed class UserCreateResponse : FrontViewBase<UserCreateResponse, UserModel>
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    protected override void ConfigureMappings(MappingRegistry<UserCreateResponse, UserModel> registry)
    {
        registry
            .AddToModel(DefaultMappingName, view => new UserModel
            {
                Id = view.Id,
                DisplayName = view.DisplayName,
                NormalizedDisplayName = view.DisplayName.Trim().ToUpperInvariant(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            })
            .AddToView(DefaultMappingName, model => new UserCreateResponse
            {
                Id = model.Id,
                DisplayName = model.DisplayName
            });
    }
}