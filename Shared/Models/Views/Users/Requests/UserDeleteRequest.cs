using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views.Users.Requests;

public sealed class UserDeleteRequest : FrontViewBase<UserDeleteRequest, UserModel>
{
    public int Id { get; set; }

    protected override void ConfigureMappings(MappingRegistry<UserDeleteRequest, UserModel> registry)
    {
        registry
            .AddToModel(DefaultMappingName, view => new UserModel
            {
                Id = view.Id
            })
            .AddToView(DefaultMappingName, model => new UserDeleteRequest
            {
                Id = model.Id
            });
    }
}
