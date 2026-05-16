using Riverside.Elapsed.App.Models.Developer;

namespace Riverside.Elapsed.App.Services.Api;

public interface IApiDeveloperService
{
	Task<ApiResult<IReadOnlyList<DeveloperApp>>> GetOwnedAppsAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<IReadOnlyList<DeveloperApp>>> GetAllAppsAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<DeveloperApp>> CreateAppAsync(string name, string description, Uri homepageUrl, Uri? iconUrl, IReadOnlyList<Uri> redirectUris, IReadOnlyList<string> scopes, CancellationToken cancellationToken = default);
	Task<ApiResult<DeveloperApp>> UpdateAppAsync(Guid appId, string? name, string? description, Uri? homepageUrl, Uri? iconUrl, IReadOnlyList<Uri>? redirectUris, IReadOnlyList<string>? scopes, CancellationToken cancellationToken = default);
	Task<ApiResult<OAuthAppSecret>> RotateAppSecretAsync(Guid appId, CancellationToken cancellationToken = default);
	Task<ApiResult<IReadOnlyList<OAuthGrant>>> GetOwnedGrantsAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<bool>> RevokeGrantAsync(string grantId, CancellationToken cancellationToken = default);
	Task<Riverside.Elapsed.App.Models.User.User?> HydrateCreatedByAsync(
		Riverside.Elapsed.App.Models.User.User? createdBy,
		CancellationToken cancellationToken = default);
}
