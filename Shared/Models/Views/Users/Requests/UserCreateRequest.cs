using Shared.Models.Base;
using Shared.Models.Persistence;

namespace Shared.Models.Views.Users.Requests;

public sealed class UserCreateRequest : FrontViewBase<UserCreateRequest, UserModel>
{
	public string DisplayName { get; set; } = string.Empty;
	public TwoFactorMethod TwoFactorMethod { get; set; }
	public string? Email { get; set; }

	protected override void ConfigureMappings(MappingRegistry<UserCreateRequest, UserModel> registry)
	{
		registry
			.AddToModel(DefaultMappingName, view => new UserModel
			{
				DisplayName = view.DisplayName,
				NormalizedDisplayName = view.DisplayName.Trim().ToUpperInvariant(),
				CreatedAtUtc = DateTimeOffset.UtcNow,
				TwoFactorMethod = view.TwoFactorMethod,
				Email = view.Email != null ? view.Email.Trim().ToLowerInvariant() : null
			})
			.AddToView(DefaultMappingName, model => new UserCreateRequest
			{
				DisplayName = model.DisplayName,
				TwoFactorMethod = model.TwoFactorMethod,
				Email = model.Email
			});
	}
}
