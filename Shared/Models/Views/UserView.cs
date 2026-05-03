using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views;

public sealed class UserView : FrontViewBase<UserView, UserModel>
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    protected override void ConfigureMappings(MappingRegistry<UserView, UserModel> registry)
    {
        registry
            .AddToModel(DefaultMappingName, view => new UserModel
            {
                Id = view.Id,
                DisplayName = view.DisplayName,
                NormalizedDisplayName = view.DisplayName.Trim().ToUpperInvariant(),
                CreatedAtUtc = DateTimeOffset.UtcNow
            })
            .AddToView(DefaultMappingName, model => new UserView
            {
                Id = model.Id,
                DisplayName = model.DisplayName
            });
    }
}
