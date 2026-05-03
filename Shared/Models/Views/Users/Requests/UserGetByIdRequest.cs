using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views.Users.Requests;

public sealed class UserGetByIdRequest : FrontViewBase<UserGetByIdRequest, UserModel>
{
    public int Id { get; set; }

    protected override void ConfigureMappings(MappingRegistry<UserGetByIdRequest, UserModel> registry)
    {
        registry
            .AddToModel(DefaultMappingName, view => new UserModel
            {
                Id = view.Id
            })
            .AddToView(DefaultMappingName, model => new UserGetByIdRequest
            {
                Id = model.Id
            });
    }
}
