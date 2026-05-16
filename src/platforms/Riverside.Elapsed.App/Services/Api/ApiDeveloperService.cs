using Riverside.Elapsed.App.Models.Developer;
using Riverside.Elapsed.Developer.CreateApp;
using Riverside.Elapsed.Developer.GetAllApps;
using Riverside.Elapsed.Developer.GetAllOwnedApps;
using Riverside.Elapsed.Developer.GetOwnedOAuthGrants;
using Riverside.Elapsed.Developer.RevokeOAuthGrant;
using Riverside.Elapsed.Developer.RotateAppSecret;
using Riverside.Elapsed.Developer.UpdateApp;

namespace Riverside.Elapsed.App.Services.Api;

public sealed class ApiDeveloperService(IApiClientFacade client, IApiUserService userService) : IApiDeveloperService
{
	public async Task<ApiResult<IReadOnlyList<DeveloperApp>>> GetOwnedAppsAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.Developer.GetAllOwnedApps.PostAsGetAllOwnedAppsPostResponseAsync(new GetAllOwnedAppsPostRequestBody(), cancellationToken: cancellationToken), cancellationToken);
		if (response?.GetAllOwnedAppsPostResponseMember1?.Data?.Apps is { } apps)
		{
			return ApiResult<IReadOnlyList<DeveloperApp>>.Success(apps.Select(app => ApiMappingExtensions.MapDeveloperApp(app, ResolveCreatedBy)).ToArray());
		}

		var error = response?.GetAllOwnedAppsPostResponseMember2?.Message ?? "Failed to load developer apps.";
		return ApiResult<IReadOnlyList<DeveloperApp>>.Failure(error);
	}

	public async Task<ApiResult<IReadOnlyList<DeveloperApp>>> GetAllAppsAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.Developer.GetAllApps.PostAsGetAllAppsPostResponseAsync(new GetAllAppsPostRequestBody(), cancellationToken: cancellationToken), cancellationToken);
		if (response?.GetAllAppsPostResponseMember1?.Data?.Apps is { } apps)
		{
			return ApiResult<IReadOnlyList<DeveloperApp>>.Success(apps.Select(app => ApiMappingExtensions.MapDeveloperApp(app, ResolveCreatedBy)).ToArray());
		}

		var error = response?.GetAllAppsPostResponseMember2?.Message ?? "Failed to load apps.";
		return ApiResult<IReadOnlyList<DeveloperApp>>.Failure(error);
	}

	public async Task<ApiResult<DeveloperApp>> CreateAppAsync(string name, string description, Uri homepageUrl, Uri? iconUrl, IReadOnlyList<Uri> redirectUris, IReadOnlyList<string> scopes, CancellationToken cancellationToken = default)
	{
		var body = new CreateAppPostRequestBody
		{
			Name = name,
			Description = description,
			HomepageUrl = homepageUrl.ToString(),
			IconUrl = iconUrl?.ToString() ?? string.Empty,
			RedirectUris = redirectUris.Select(uri => uri.ToString()).ToList(),
			Scopes = scopes.ToList(),
		};

		var response = await client.SendAsync(x => x.Developer.CreateApp.PostAsCreateAppPostResponseAsync(body, cancellationToken: cancellationToken), cancellationToken);
		if (response?.CreateAppPostResponseMember1?.Data?.App is { } app)
		{
			return ApiResult<DeveloperApp>.Success(ApiMappingExtensions.MapDeveloperApp(app, ResolveCreatedBy));
		}

		var error = response?.CreateAppPostResponseMember2?.Message ?? "Failed to create app.";
		return ApiResult<DeveloperApp>.Failure(error);
	}

	public async Task<ApiResult<DeveloperApp>> UpdateAppAsync(Guid appId, string? name, string? description, Uri? homepageUrl, Uri? iconUrl, IReadOnlyList<Uri>? redirectUris, IReadOnlyList<string>? scopes, CancellationToken cancellationToken = default)
	{
		var body = new UpdateAppPostRequestBody
		{
			Id = appId,
			Name = name,
			Description = description,
			HomepageUrl = homepageUrl?.ToString(),
			IconUrl = iconUrl?.ToString() ?? string.Empty,
			RedirectUris = redirectUris?.Select(uri => uri.ToString()).ToList(),
			Scopes = scopes?.ToList(),
		};

		var response = await client.SendAsync(x => x.Developer.UpdateApp.PostAsUpdateAppPostResponseAsync(body, cancellationToken: cancellationToken), cancellationToken);
		if (response?.UpdateAppPostResponseMember1?.Data?.App is { } app)
		{
			return ApiResult<DeveloperApp>.Success(ApiMappingExtensions.MapDeveloperApp(app, ResolveCreatedBy));
		}

		var error = response?.UpdateAppPostResponseMember2?.Message ?? "Failed to update app.";
		return ApiResult<DeveloperApp>.Failure(error);
	}

	public async Task<ApiResult<OAuthAppSecret>> RotateAppSecretAsync(Guid appId, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.Developer.RotateAppSecret.PostAsRotateAppSecretPostResponseAsync(new RotateAppSecretPostRequestBody
		{
			Id = appId,
		}, cancellationToken: cancellationToken), cancellationToken);

		if (response?.RotateAppSecretPostResponseMember1?.Data?.ClientSecret is { } secret)
		{
			return ApiResult<OAuthAppSecret>.Success(new OAuthAppSecret
			{
				App = new DeveloperApp
				{
					AppId = appId,
					Name = string.Empty,
					Description = string.Empty,
					HomepageUrl = new Uri("https://example.com", UriKind.Absolute),
					IconUrl = null,
					RedirectUris = Array.Empty<Uri>(),
					Scopes = Array.Empty<string>(),
					TrustLevel = TrustLevel.Untrusted,
					ClientId = string.Empty,
					CreatedAt = DateTimeOffset.MinValue,
					CreatedBy = null,
				},
				ClientSecret = secret,
			});
		}

		var error = response?.RotateAppSecretPostResponseMember2?.Message ?? "Failed to rotate app secret.";
		return ApiResult<OAuthAppSecret>.Failure(error);
	}

	public async Task<ApiResult<IReadOnlyList<OAuthGrant>>> GetOwnedGrantsAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.Developer.GetOwnedOAuthGrants.PostAsGetOwnedOAuthGrantsPostResponseAsync(new GetOwnedOAuthGrantsPostRequestBody(), cancellationToken: cancellationToken), cancellationToken);
		if (response?.GetOwnedOAuthGrantsPostResponseMember1?.Data?.Grants is { } grants)
		{
			return ApiResult<IReadOnlyList<OAuthGrant>>.Success(grants.Select(ApiMappingExtensions.MapOAuthGrant).ToArray());
		}

		var error = response?.GetOwnedOAuthGrantsPostResponseMember2?.Message ?? "Failed to load OAuth grants.";
		return ApiResult<IReadOnlyList<OAuthGrant>>.Failure(error);
	}

	public async Task<ApiResult<bool>> RevokeGrantAsync(string grantId, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.Developer.RevokeOAuthGrant.PostAsRevokeOAuthGrantPostResponseAsync(new RevokeOAuthGrantPostRequestBody
		{
			GrantId = grantId,
		}, cancellationToken: cancellationToken), cancellationToken);

		if (response?.RevokeOAuthGrantPostResponseMember1 is not null)
		{
			return ApiResult<bool>.Success(true);
		}

		var error = response?.RevokeOAuthGrantPostResponseMember2?.Message ?? "Failed to revoke grant.";
		return ApiResult<bool>.Failure(error);
	}

	public Task<Riverside.Elapsed.App.Models.User.User?> HydrateCreatedByAsync(Riverside.Elapsed.App.Models.User.User? createdBy, CancellationToken cancellationToken = default)
		=> userService.HydrateUserAsync(createdBy, cancellationToken);

	private Riverside.Elapsed.App.Models.User.User? ResolveCreatedBy(Riverside.Elapsed.App.Models.User.User? user)
		=> user;
}
