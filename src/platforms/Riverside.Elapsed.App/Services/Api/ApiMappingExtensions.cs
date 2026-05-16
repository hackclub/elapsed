using System.Globalization;
using Microsoft.Kiota.Abstractions.Serialization;
using Riverside.Elapsed.App.Models.Admin;
using Riverside.Elapsed.App.Models.Auth;
using Riverside.Elapsed.App.Models.Developer;
using Riverside.Elapsed.App.Models.Global;
using Riverside.Elapsed.App.Models.Hackatime;
using Riverside.Elapsed.App.Models.Timelapses;
using Riverside.Elapsed.App.Models.User;
using Riverside.Elapsed.Auth.Token;
using Riverside.Elapsed.Global.ActiveUsers;
using Riverside.Elapsed.Global.RecentTimelapses;
using Riverside.Elapsed.Global.WeeklyLeaderboard;
using Riverside.Elapsed.Hackatime.AllProjects;
using Riverside.Elapsed.Hackatime.MyTimelapsesForProject;
using Riverside.Elapsed.Hackatime.TimelapsesForProject;
using Riverside.Elapsed.User.Myself;
using Riverside.Elapsed.User.Query;
using Riverside.Elapsed.User.QueryByEmail;
using Riverside.Elapsed.User.Update;
using Riverside.Elapsed.User.GetDevices;
using Riverside.Elapsed.User.RegisterDevice;
using Riverside.Elapsed.User.RequestKeyRelay;
using Riverside.Elapsed.User.ReceiveKeyRelay;
using Riverside.Elapsed.User.QueryKeyRelayRequest;
using Riverside.Elapsed.User.GetTotalTimelapseTime;
using Riverside.Elapsed.User.HackatimeProjects;
using Riverside.Elapsed.User.ProvideKeyRelay;
using Riverside.Elapsed.User.DenyKeyRelay;
using Riverside.Elapsed.User.EmitHeartbeat;
using Riverside.Elapsed.Timelapse.Query;
using Riverside.Elapsed.Timelapse.MyPublishedTimelapses;
using Riverside.Elapsed.Timelapse.FindByUser;
using Riverside.Elapsed.DraftTimelapse.FindByUser;
using Riverside.Elapsed.DraftTimelapse.Query;
using Riverside.Elapsed.DraftTimelapse.Legacy;
using Riverside.Elapsed.Comment.Create;
using Riverside.Elapsed.Admin.Stats;
using Riverside.Elapsed.Admin.List;
using Riverside.Elapsed.Admin.Update;
using Riverside.Elapsed.Admin.Search;
using Riverside.Elapsed.Admin.Export;
using Riverside.Elapsed.Admin.RecalculateDurations;
using Riverside.Elapsed.Admin.ProgramKey.List;
using Riverside.Elapsed.Admin.ProgramKey.Create;
using Riverside.Elapsed.Admin.ProgramKey.Rotate;
using Riverside.Elapsed.Admin.ProgramKey.Update;
using Riverside.Elapsed.Admin.ProgramKey.Revoke;
using Riverside.Elapsed.Developer.GetAllOwnedApps;
using Riverside.Elapsed.Developer.GetAllApps;
using Riverside.Elapsed.Developer.AppByClientId;
using Riverside.Elapsed.Developer.CreateApp;
using Riverside.Elapsed.Developer.UpdateApp;
using Riverside.Elapsed.Developer.RotateAppSecret;
using Riverside.Elapsed.Developer.GetOwnedOAuthGrants;
using Riverside.Elapsed.Developer.RevokeOAuthGrant;
using AppComment = Riverside.Elapsed.App.Models.Timelapses.Comment;
using AppTimelapse = Riverside.Elapsed.App.Models.Timelapses.Timelapse;
using AppUser = Riverside.Elapsed.App.Models.User.User;
using AppVisibility = Riverside.Elapsed.App.Models.Timelapses.Visibility;

namespace Riverside.Elapsed.App.Services.Api;

public static class ApiMappingExtensions
{
	private static readonly Uri FallbackUri = new("https://example.com", UriKind.Absolute);
	private static readonly Uri FallbackProfileUri = new("https://example.com", UriKind.Absolute);

	public static UserDetails MapUserDetails(MyselfGetResponseMember1_data_userMember1 user)
		=> new()
		{
			UserId = user.Id ?? string.Empty,
			CreatedAt = UnixMillisToDateTimeOffset((long?)user.CreatedAt),
			Handle = user.Handle ?? string.Empty,
			DisplayName = user.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(user.ProfilePictureUrl, UriKind.Absolute, out var profilePicture)
				? profilePicture
				: FallbackProfileUri,
			Bio = user.Bio ?? string.Empty,
			Urls = (user.Urls ?? []).Select(url => new Uri(url, UriKind.Absolute)).ToArray(),
			HackatimeId = user.HackatimeId?.String,
			SlackId = user.SlackId?.String,
		};

	public static UserDetails MapUserDetails(QueryByEmailGetResponseMember1_data_userMember1 user)
		=> new()
		{
			UserId = user.Id ?? string.Empty,
			CreatedAt = UnixMillisToDateTimeOffset((long?)user.CreatedAt),
			Handle = user.Handle ?? string.Empty,
			DisplayName = user.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(user.ProfilePictureUrl, UriKind.Absolute, out var profilePicture)
				? profilePicture
				: FallbackProfileUri,
			Bio = user.Bio ?? string.Empty,
			Urls = (user.Urls ?? []).Select(url => new Uri(url, UriKind.Absolute)).ToArray(),
			HackatimeId = user.HackatimeId?.String,
			SlackId = user.SlackId?.String,
		};

	public static UserDetails MapUserDetails(UpdatePatchResponseMember1_data_user user)
		=> new()
		{
			UserId = user.Id ?? string.Empty,
			CreatedAt = UnixMillisToDateTimeOffset((long?)user.CreatedAt),
			Handle = user.Handle ?? string.Empty,
			DisplayName = user.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(user.ProfilePictureUrl, UriKind.Absolute, out var profilePicture)
				? profilePicture
				: FallbackProfileUri,
			Bio = user.Bio ?? string.Empty,
			Urls = (user.Urls ?? []).Select(url => new Uri(url, UriKind.Absolute)).ToArray(),
			HackatimeId = user.HackatimeId?.String,
			SlackId = user.SlackId?.String,
		};

	public static UserDetails? MapUserDetails(UntypedNode? node)
	{
		if (node is not UntypedObject untyped)
		{
			return null;
		}

		var values = untyped.GetValue();
		return new UserDetails
		{
			UserId = GetString(values, "id"),
			CreatedAt = UnixMillisToDateTimeOffset(GetLong(values, "createdAt")),
			Handle = GetString(values, "handle"),
			DisplayName = GetString(values, "displayName"),
			ProfilePictureUrl = Uri.TryCreate(GetString(values, "profilePictureUrl"), UriKind.Absolute, out var profilePicture)
				? profilePicture
				: FallbackProfileUri,
			Bio = GetString(values, "bio"),
			Urls = GetStringArray(values, "urls")
				.Select(url => new Uri(url, UriKind.Absolute))
				.ToArray(),
			HackatimeId = GetStringOrNull(values, "hackatimeId"),
			SlackId = GetStringOrNull(values, "slackId"),
		};
	}

	public static AppUser MapUser(GetAllOwnedAppsPostResponseMember1_data_apps_createdByMember1 createdBy)
		=> new()
		{
			UserId = createdBy.Id ?? string.Empty,
			Handle = createdBy.Handle ?? string.Empty,
			DisplayName = createdBy.DisplayName ?? string.Empty,
			ProfilePictureUrl = FallbackProfileUri,
		};

	public static AppUser MapUser(GetAllAppsPostResponseMember1_data_apps_createdByMember1 createdBy)
		=> new()
		{
			UserId = createdBy.Id ?? string.Empty,
			Handle = createdBy.Handle ?? string.Empty,
			DisplayName = createdBy.DisplayName ?? string.Empty,
			ProfilePictureUrl = FallbackProfileUri,
		};

	public static AppUser MapUser(CreateAppPostResponseMember1_data_app_createdByMember1 createdBy)
		=> new()
		{
			UserId = createdBy.Id ?? string.Empty,
			Handle = createdBy.Handle ?? string.Empty,
			DisplayName = createdBy.DisplayName ?? string.Empty,
			ProfilePictureUrl = FallbackProfileUri,
		};

	public static AppUser MapUser(UpdateAppPostResponseMember1_data_app_createdByMember1 createdBy)
		=> new()
		{
			UserId = createdBy.Id ?? string.Empty,
			Handle = createdBy.Handle ?? string.Empty,
			DisplayName = createdBy.DisplayName ?? string.Empty,
			ProfilePictureUrl = FallbackProfileUri,
		};

	public static AppUser MapUser(ListPostResponseMember1_data_keys_createdBy createdBy)
		=> new()
		{
			UserId = createdBy.Id ?? string.Empty,
			Handle = createdBy.Handle ?? string.Empty,
			DisplayName = createdBy.DisplayName ?? string.Empty,
			ProfilePictureUrl = FallbackProfileUri,
		};

	public static AppUser MapUser(CreatePostResponseMember1_data_key_createdBy createdBy)
		=> new()
		{
			UserId = createdBy.Id ?? string.Empty,
			Handle = createdBy.Handle ?? string.Empty,
			DisplayName = createdBy.DisplayName ?? string.Empty,
			ProfilePictureUrl = FallbackProfileUri,
		};

	public static AppUser MapUser(WeeklyLeaderboardGetResponseMember1_data_leaderboard entry)
		=> new()
		{
			UserId = entry.Id ?? string.Empty,
			Handle = entry.Handle ?? string.Empty,
			DisplayName = entry.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(entry.Pfp, UriKind.Absolute, out var uri) ? uri : FallbackProfileUri,
		};

	public static AppUser MapUser(RecentTimelapsesGetResponseMember1_data_timelapses_owner owner)
		=> new()
		{
			UserId = owner.Id ?? string.Empty,
			Handle = owner.Handle ?? string.Empty,
			DisplayName = owner.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(owner.ProfilePictureUrl, UriKind.Absolute, out var uri) ? uri : FallbackProfileUri,
		};

	public static AppUser MapUser(RecentTimelapsesGetResponseMember1_data_timelapses_comments_author author)
		=> new()
		{
			UserId = author.Id ?? string.Empty,
			Handle = author.Handle ?? string.Empty,
			DisplayName = author.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(author.ProfilePictureUrl, UriKind.Absolute, out var uri) ? uri : FallbackProfileUri,
		};

	public static LeaderboardEntry MapLeaderboardEntry(WeeklyLeaderboardGetResponseMember1_data_leaderboard entry)
		=> new()
		{
			User = new AppUser
			{
				UserId = entry.Id ?? string.Empty,
				Handle = entry.Handle ?? string.Empty,
				DisplayName = entry.DisplayName ?? string.Empty,
				ProfilePictureUrl = Uri.TryCreate(entry.Pfp, UriKind.Absolute, out var uri)
					? uri
					: FallbackProfileUri,
				Bio = string.Empty,
				Urls = Array.Empty<Uri>(),
				HackatimeId = null,
				SlackId = null,
			},
			SecondsThisWeek = entry.SecondsThisWeek ?? 0,
		};

	public static AppUser MapTimelapseUser(RecentTimelapsesGetResponseMember1_data_timelapses_owner owner)
		=> new()
		{
			UserId = owner.Id ?? string.Empty,
			Handle = owner.Handle ?? string.Empty,
			DisplayName = owner.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(owner.ProfilePictureUrl, UriKind.Absolute, out var uri) ? uri : FallbackProfileUri,
			Bio = owner.Bio ?? string.Empty,
			Urls = (owner.Urls ?? []).Select(url => new Uri(url, UriKind.Absolute)).ToArray(),
			HackatimeId = owner.HackatimeId?.String,
			SlackId = owner.SlackId?.String,
		};

	public static AppUser MapTimelapseUser(RecentTimelapsesGetResponseMember1_data_timelapses_comments_author author)
		=> new()
		{
			UserId = author.Id ?? string.Empty,
			Handle = author.Handle ?? string.Empty,
			DisplayName = author.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(author.ProfilePictureUrl, UriKind.Absolute, out var uri) ? uri : FallbackProfileUri,
			Bio = author.Bio ?? string.Empty,
			Urls = (author.Urls ?? []).Select(url => new Uri(url, UriKind.Absolute)).ToArray(),
			HackatimeId = author.HackatimeId?.String,
			SlackId = author.SlackId?.String,
		};

	public static AppComment MapComment(RecentTimelapsesGetResponseMember1_data_timelapses_comments comment)
		=> new()
		{
			CommentId = comment.Id ?? string.Empty,
			Content = comment.Content ?? string.Empty,
			Author = MapTimelapseUser(comment.Author),
			CreatedAt = UnixMillisToDateTimeOffset((long?)comment.CreatedAt),
		};

	public static AppTimelapse MapTimelapse(RecentTimelapsesGetResponseMember1_data_timelapses timelapse)
		=> new()
		{
			TimelapseId = timelapse.Id ?? string.Empty,
			Name = timelapse.Name ?? string.Empty,
			Description = timelapse.Description ?? string.Empty,
			Visibility = MapVisibility(timelapse.Visibility?.ToString()),
			CreatedAt = UnixMillisToDateTimeOffset((long?)timelapse.CreatedAt),
			Owner = MapTimelapseUser(timelapse.Owner),
			Comments = (timelapse.Comments ?? []).Select(MapComment).ToArray(),
			PlaybackUrl = Uri.TryCreate(timelapse.PlaybackUrl?.String, UriKind.Absolute, out var playback) ? playback : null,
			ThumbnailUrl = Uri.TryCreate(timelapse.ThumbnailUrl?.String, UriKind.Absolute, out var thumbnail) ? thumbnail : null,
			DurationSeconds = timelapse.Duration ?? 0,
			HackatimeProject = timelapse.Private?.HackatimeProject?.String,
			SourceDraftId = timelapse.Private?.SourceDraftId?.String,
		};

	public static DeveloperApp MapDeveloperApp(GetAllOwnedAppsPostResponseMember1_data_apps app, Func<AppUser?, AppUser?> createdByResolver)
		=> new()
		{
			AppId = app.Id ?? Guid.Empty,
			Name = app.Name ?? string.Empty,
			Description = app.Description ?? string.Empty,
			HomepageUrl = Uri.TryCreate(app.HomepageUrl, UriKind.Absolute, out var homepage) ? homepage : FallbackUri,
			IconUrl = Uri.TryCreate(app.IconUrl, UriKind.Absolute, out var icon) ? icon : null,
			RedirectUris = (app.RedirectUris ?? []).Select(uri => new Uri(uri, UriKind.Absolute)).ToArray(),
			Scopes = app.Scopes?.ToArray() ?? Array.Empty<string>(),
			TrustLevel = MapTrustLevel(app.TrustLevel?.ToString()),
			ClientId = app.ClientId ?? string.Empty,
			CreatedAt = DateTimeOffset.TryParse(app.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			CreatedBy = createdByResolver(app.CreatedBy?.GetAllOwnedAppsPostResponseMember1DataAppsCreatedByMember1 is null
				? null
				: MapUser(app.CreatedBy.GetAllOwnedAppsPostResponseMember1DataAppsCreatedByMember1)),
		};

	public static DeveloperApp MapDeveloperApp(GetAllAppsPostResponseMember1_data_apps app, Func<AppUser?, AppUser?> createdByResolver)
		=> new()
		{
			AppId = app.Id ?? Guid.Empty,
			Name = app.Name ?? string.Empty,
			Description = app.Description ?? string.Empty,
			HomepageUrl = Uri.TryCreate(app.HomepageUrl, UriKind.Absolute, out var homepage) ? homepage : FallbackUri,
			IconUrl = Uri.TryCreate(app.IconUrl, UriKind.Absolute, out var icon) ? icon : null,
			RedirectUris = (app.RedirectUris ?? []).Select(uri => new Uri(uri, UriKind.Absolute)).ToArray(),
			Scopes = app.Scopes?.ToArray() ?? Array.Empty<string>(),
			TrustLevel = MapTrustLevel(app.TrustLevel?.ToString()),
			ClientId = app.ClientId ?? string.Empty,
			CreatedAt = DateTimeOffset.TryParse(app.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			CreatedBy = createdByResolver(app.CreatedBy?.GetAllAppsPostResponseMember1DataAppsCreatedByMember1 is null
				? null
				: MapUser(app.CreatedBy.GetAllAppsPostResponseMember1DataAppsCreatedByMember1)),
		};

	public static DeveloperApp MapDeveloperApp(CreateAppPostResponseMember1_data_app app, Func<AppUser?, AppUser?> createdByResolver)
		=> new()
		{
			AppId = app.Id ?? Guid.Empty,
			Name = app.Name ?? string.Empty,
			Description = app.Description ?? string.Empty,
			HomepageUrl = Uri.TryCreate(app.HomepageUrl, UriKind.Absolute, out var homepage) ? homepage : FallbackUri,
			IconUrl = Uri.TryCreate(app.IconUrl, UriKind.Absolute, out var icon) ? icon : null,
			RedirectUris = (app.RedirectUris ?? []).Select(uri => new Uri(uri, UriKind.Absolute)).ToArray(),
			Scopes = app.Scopes?.ToArray() ?? Array.Empty<string>(),
			TrustLevel = MapTrustLevel(app.TrustLevel?.ToString()),
			ClientId = app.ClientId ?? string.Empty,
			CreatedAt = DateTimeOffset.TryParse(app.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			CreatedBy = createdByResolver(app.CreatedBy?.CreateAppPostResponseMember1DataAppCreatedByMember1 is null
				? null
				: MapUser(app.CreatedBy.CreateAppPostResponseMember1DataAppCreatedByMember1)),
		};

	public static DeveloperApp MapDeveloperApp(UpdateAppPostResponseMember1_data_app app, Func<AppUser?, AppUser?> createdByResolver)
		=> new()
		{
			AppId = app.Id ?? Guid.Empty,
			Name = app.Name ?? string.Empty,
			Description = app.Description ?? string.Empty,
			HomepageUrl = Uri.TryCreate(app.HomepageUrl, UriKind.Absolute, out var homepage) ? homepage : FallbackUri,
			IconUrl = Uri.TryCreate(app.IconUrl, UriKind.Absolute, out var icon) ? icon : null,
			RedirectUris = (app.RedirectUris ?? []).Select(uri => new Uri(uri, UriKind.Absolute)).ToArray(),
			Scopes = app.Scopes?.ToArray() ?? Array.Empty<string>(),
			TrustLevel = MapTrustLevel(app.TrustLevel?.ToString()),
			ClientId = app.ClientId ?? string.Empty,
			CreatedAt = DateTimeOffset.TryParse(app.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			CreatedBy = createdByResolver(app.CreatedBy?.UpdateAppPostResponseMember1DataAppCreatedByMember1 is null
				? null
				: MapUser(app.CreatedBy.UpdateAppPostResponseMember1DataAppCreatedByMember1)),
		};

	public static ProgramKeyMetadata MapProgramKey(ListPostResponseMember1_data_keys key, Func<AppUser?, AppUser> createdByResolver)
		=> new()
		{
			KeyId = key.Id ?? Guid.Empty,
			Name = key.Name ?? string.Empty,
			KeyPrefix = key.KeyPrefix ?? string.Empty,
			Scopes = key.Scopes?.ToArray() ?? Array.Empty<string>(),
			CreatedBy = createdByResolver(key.CreatedBy is null ? null : MapUser(key.CreatedBy)),
			CreatedAt = DateTimeOffset.TryParse(key.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			LastUsedAt = DateTimeOffset.TryParse(key.LastUsedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var lastUsedAt)
				? lastUsedAt
				: null,
			RevokedAt = DateTimeOffset.TryParse(key.RevokedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var revokedAt)
				? revokedAt
				: null,
			ExpiresAt = DateTimeOffset.TryParse(key.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt)
				? expiresAt
				: DateTimeOffset.MinValue,
		};

	public static HackatimeProject MapHackatimeProject(HackatimeProjectsGetResponseMember1_data_projects project)
		=> new()
		{
			Name = project.Name ?? string.Empty,
			TotalSeconds = project.Time ?? 0,
		};

	public static Device MapDevice(GetDevicesGetResponseMember1_data_devices device)
		=> new()
		{
			DeviceId = device.Id ?? Guid.Empty,
			Name = device.Name ?? string.Empty,
		};

	public static Device MapDevice(RegisterDevicePostResponseMember1_data_device device)
		=> new()
		{
			DeviceId = device.Id ?? Guid.Empty,
			Name = device.Name ?? string.Empty,
		};

	public static AppUser MapUser(UserDetails details)
		=> new()
		{
			UserId = details.UserId,
			CreatedAt = details.CreatedAt,
			Handle = details.Handle,
			DisplayName = details.DisplayName,
			ProfilePictureUrl = details.ProfilePictureUrl,
			Bio = details.Bio,
			Urls = details.Urls,
			HackatimeId = details.HackatimeId,
			SlackId = details.SlackId,
		};

	public static OAuthGrant MapOAuthGrant(GetOwnedOAuthGrantsPostResponseMember1_data_grants grant)
		=> new()
		{
			GrantId = grant.Id ?? string.Empty,
			ServiceClientId = grant.ServiceClientId ?? string.Empty,
			ServiceName = grant.ServiceName ?? string.Empty,
			Scopes = grant.Scopes?.ToArray() ?? Array.Empty<string>(),
			CreatedAt = DateTimeOffset.TryParse(grant.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			LastUsedAt = DateTimeOffset.TryParse(grant.LastUsedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var lastUsedAt)
				? lastUsedAt
				: null,
		};

	public static AdminSearchResult MapAdminSearchResult(SearchPostResponseMember1_data_results result)
		=> new()
		{
			Entity = MapEntityType(result.Entity?.ToString()),
			Id = result.Id ?? string.Empty,
			DisplayText = result.DisplayText ?? string.Empty,
		};

	public static AdminStats MapAdminStats(StatsGetResponseMember1_data data)
		=> new()
		{
			TotalLoggedSeconds = data.TotalLoggedSeconds ?? 0,
			TotalProjects = data.TotalProjects ?? 0,
			TotalUsers = data.TotalUsers ?? 0,
		};

	public static AdminListResponse MapAdminList(Riverside.Elapsed.Admin.List.ListPostResponseMember1_data data)
		=> new()
		{
			Entity = MapEntityType(data.Entity?.ToString()),
			Rows = data.Rows?.ToArray() ?? Array.Empty<object>(),
			Total = data.Total ?? 0,
			Page = data.Page ?? 0,
			PageSize = data.PageSize ?? 0,
		};

	public static AdminUpdateResult MapAdminUpdateResult(Riverside.Elapsed.Admin.Update.UpdatePatchResponseMember1_data data)
		=> new()
		{
			Entity = MapEntityType(data.Entity?.ToString()),
			Row = data.Row ?? new object(),
		};

	public static AdminExport MapAdminExport(ExportPostResponseMember1_data data)
		=> new()
		{
			Data = data.Data ?? new object(),
		};

	public static ProgramKeyMetadata MapProgramKey(CreatePostResponseMember1_data_key key, Func<AppUser?, AppUser> createdByResolver)
		=> new()
		{
			KeyId = key.Id ?? Guid.Empty,
			Name = key.Name ?? string.Empty,
			KeyPrefix = key.KeyPrefix ?? string.Empty,
			Scopes = key.Scopes?.ToArray() ?? Array.Empty<string>(),
			CreatedBy = createdByResolver(key.CreatedBy is null ? null : MapUser(key.CreatedBy)),
			CreatedAt = DateTimeOffset.TryParse(key.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			LastUsedAt = DateTimeOffset.TryParse(key.LastUsedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var lastUsedAt)
				? lastUsedAt
				: null,
			RevokedAt = DateTimeOffset.TryParse(key.RevokedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var revokedAt)
				? revokedAt
				: null,
			ExpiresAt = DateTimeOffset.TryParse(key.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt)
				? expiresAt
				: DateTimeOffset.MinValue,
		};

	public static TrustLevel MapTrustLevel(string? trustLevel)
		=> trustLevel?.ToUpperInvariant() switch
		{
			"TRUSTED" => TrustLevel.Trusted,
			_ => TrustLevel.Untrusted,
		};

	public static AppVisibility MapVisibility(string? visibility)
		=> visibility?.ToUpperInvariant() switch
		{
			"PUBLIC" => AppVisibility.Public,
			"FAILED_PROCESSING" => AppVisibility.FailedProcessing,
			_ => AppVisibility.Unlisted,
		};

	public static EntityType MapEntityType(string? entity)
		=> entity?.ToUpperInvariant() switch
		{
			"USER" => EntityType.User,
			"TIMELAPSE" => EntityType.Timelapse,
			"COMMENT" => EntityType.Comment,
			"DRAFTTIMELAPSE" => EntityType.DraftTimelapse,
			"LEGACYTIMELAPSE" => EntityType.LegacyTimelapse,
			_ => EntityType.User,
		};

	private static string GetString(IDictionary<string, UntypedNode> values, string key)
		=> values.TryGetValue(key, out var node) && node is UntypedString untyped
			? untyped.GetValue() ?? string.Empty
			: string.Empty;

	private static string? GetStringOrNull(IDictionary<string, UntypedNode> values, string key)
	{
		if (!values.TryGetValue(key, out var node))
		{
			return null;
		}

		if (node is UntypedNull)
		{
			return null;
		}

		return node is UntypedString untyped ? untyped.GetValue() : null;
	}

	private static long? GetLong(IDictionary<string, UntypedNode> values, string key)
	{
		if (!values.TryGetValue(key, out var node))
		{
			return null;
		}

		if (node is UntypedInteger untypedInteger)
		{
			return untypedInteger.GetValue();
		}

		if (node is UntypedDouble untypedDouble)
		{
			return (long)untypedDouble.GetValue();
		}

		return null;
	}

	private static IReadOnlyList<string> GetStringArray(IDictionary<string, UntypedNode> values, string key)
	{
		if (!values.TryGetValue(key, out var node) || node is not UntypedArray array)
		{
			return Array.Empty<string>();
		}

		return array.GetValue()
			.OfType<UntypedString>()
			.Select(item => item.GetValue() ?? string.Empty)
			.ToArray();
	}

	private static DateTimeOffset UnixMillisToDateTimeOffset(long? millis)
		=> millis.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(millis.Value) : DateTimeOffset.MinValue;
}
