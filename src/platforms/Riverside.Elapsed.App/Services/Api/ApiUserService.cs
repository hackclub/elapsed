using Microsoft.Kiota.Abstractions;
using Riverside.Elapsed.App.Models.User;
using Riverside.Elapsed.App.Models.User.Local;
using Riverside.Elapsed.User.EmitHeartbeat;
using Riverside.Elapsed.User.GetDevices;
using Riverside.Elapsed.User.GetTotalTimelapseTime;
using Riverside.Elapsed.User.HackatimeProjects;
using Riverside.Elapsed.User.Myself;
using Riverside.Elapsed.User.Query;
using Riverside.Elapsed.User.QueryByEmail;
using Riverside.Elapsed.User.QueryKeyRelayRequest;
using Riverside.Elapsed.User.ReceiveKeyRelay;
using Riverside.Elapsed.User.RegisterDevice;
using Riverside.Elapsed.User.RequestKeyRelay;
using Riverside.Elapsed.User.ProvideKeyRelay;
using Riverside.Elapsed.User.DenyKeyRelay;
using Riverside.Elapsed.User.SignOut;
using Riverside.Elapsed.User.Update;

namespace Riverside.Elapsed.App.Services.Api;

public sealed class ApiUserService(IApiClientFacade client) : IApiUserService
{
	public async Task<ApiResult<UserDetails>> GetMyselfAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.Myself.GetAsMyselfGetResponseAsync(cancellationToken: cancellationToken), cancellationToken);
		if (response?.MyselfGetResponseMember1?.Data?.User?.MyselfGetResponseMember1DataUserMember1 is { } user)
		{
			return ApiResult<UserDetails>.Success(ApiMappingExtensions.MapUserDetails(user));
		}

		var error = response?.MyselfGetResponseMember2?.Message ?? "Failed to load user profile.";
		return ApiResult<UserDetails>.Failure(error);
	}

	public async Task<ApiResult<UserDetails>> QueryUserAsync(string? id = null, string? handle = null, long? hackatimeId = null, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.Query.GetAsQueryGetResponseAsync(request =>
		{
			request.QueryParameters = new QueryRequestBuilder.QueryRequestBuilderGetQueryParameters
			{
				Id = id,
				Handle = handle,
				HackatimeId = hackatimeId,
			};
		}, cancellationToken), cancellationToken);

		if (response?.QueryGetResponseMember1?.Data?.User is { } user)
		{
			var mapped = ApiMappingExtensions.MapUserDetails(user);
			if (mapped is not null)
			{
				return ApiResult<UserDetails>.Success(mapped);
			}
		}

		var error = response?.QueryGetResponseMember2?.Message ?? "Failed to query user profile.";
		return ApiResult<UserDetails>.Failure(error);
	}

	public async Task<ApiResult<UserDetails>> QueryUserByEmailAsync(string email, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.QueryByEmail.GetAsQueryByEmailGetResponseAsync(request =>
		{
			request.QueryParameters = new QueryByEmailRequestBuilder.QueryByEmailRequestBuilderGetQueryParameters
			{
				Email = email,
			};
		}, cancellationToken), cancellationToken);

		if (response?.QueryByEmailGetResponseMember1?.Data?.User?.QueryByEmailGetResponseMember1DataUserMember1 is { } user)
		{
			return ApiResult<UserDetails>.Success(ApiMappingExtensions.MapUserDetails(user));
		}

		var error = response?.QueryByEmailGetResponseMember2?.Message ?? "Failed to query user profile by email.";
		return ApiResult<UserDetails>.Failure(error);
	}

	public async Task<ApiResult<UserDetails>> UpdateUserAsync(string id, string? handle, string? displayName, string? bio, IReadOnlyList<Uri>? urls, CancellationToken cancellationToken = default)
	{
		var body = new UpdatePatchRequestBody
		{
			Id = id,
			Changes = new UpdatePatchRequestBody_changes
			{
				Handle = handle,
				DisplayName = displayName,
				Bio = bio,
				Urls = urls?.Select(url => url.ToString()).ToList(),
			},
		};

		var response = await client.SendAsync(x => x.User.Update.PatchAsUpdatePatchResponseAsync(body, cancellationToken: cancellationToken), cancellationToken);
		if (response?.UpdatePatchResponseMember1?.Data?.User is { } user)
		{
			return ApiResult<UserDetails>.Success(ApiMappingExtensions.MapUserDetails(user));
		}

		var error = response?.UpdatePatchResponseMember2?.Message ?? "Failed to update user profile.";
		return ApiResult<UserDetails>.Failure(error);
	}

	public async Task<ApiResult<IReadOnlyList<Device>>> GetDevicesAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.GetDevices.GetAsGetDevicesGetResponseAsync(cancellationToken: cancellationToken), cancellationToken);
		if (response?.GetDevicesGetResponseMember1?.Data?.Devices is { } devices)
		{
			return ApiResult<IReadOnlyList<Device>>.Success(devices.Select(ApiMappingExtensions.MapDevice).ToArray());
		}

		var error = response?.GetDevicesGetResponseMember2?.Message ?? "Failed to load devices.";
		return ApiResult<IReadOnlyList<Device>>.Failure(error);
	}

	public async Task<ApiResult<Device>> RegisterDeviceAsync(string name, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.RegisterDevice.PostAsRegisterDevicePostResponseAsync(new RegisterDevicePostRequestBody
		{
			Name = name,
		}, cancellationToken: cancellationToken), cancellationToken);

		if (response?.RegisterDevicePostResponseMember1?.Data?.Device is { } device)
		{
			return ApiResult<Device>.Success(ApiMappingExtensions.MapDevice(device));
		}

		var error = response?.RegisterDevicePostResponseMember2?.Message ?? "Failed to register device.";
		return ApiResult<Device>.Failure(error);
	}

	public async Task<ApiResult<double>> GetTotalTimelapseTimeAsync(string? userId, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.GetTotalTimelapseTime.GetAsGetTotalTimelapseTimeGetResponseAsync(request =>
		{
			request.QueryParameters = new GetTotalTimelapseTimeRequestBuilder.GetTotalTimelapseTimeRequestBuilderGetQueryParameters
			{
				Id = userId,
			};
		}, cancellationToken), cancellationToken);

		if (response?.GetTotalTimelapseTimeGetResponseMember1?.Data?.Time is { } time)
		{
			return ApiResult<double>.Success(time);
		}

		var error = response?.GetTotalTimelapseTimeGetResponseMember2?.Message ?? "Failed to load total timelapse time.";
		return ApiResult<double>.Failure(error);
	}

	public async Task<ApiResult<IReadOnlyList<Riverside.Elapsed.App.Models.Hackatime.HackatimeProject>>> GetHackatimeProjectsAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.HackatimeProjects.GetAsHackatimeProjectsGetResponseAsync(cancellationToken: cancellationToken), cancellationToken);
		if (response?.HackatimeProjectsGetResponseMember1?.Data?.Projects is { } projects)
		{
			return ApiResult<IReadOnlyList<Riverside.Elapsed.App.Models.Hackatime.HackatimeProject>>.Success(projects.Select(ApiMappingExtensions.MapHackatimeProject).ToArray());
		}

		var error = response?.HackatimeProjectsGetResponseMember2?.Message ?? "Failed to load Hackatime projects.";
		return ApiResult<IReadOnlyList<Riverside.Elapsed.App.Models.Hackatime.HackatimeProject>>.Failure(error);
	}

	public async Task<ApiResult<KeyRelayRequest?>> QueryKeyRelayRequestAsync(Guid callingDevice, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.QueryKeyRelayRequest.GetAsQueryKeyRelayRequestGetResponseAsync(request =>
		{
			request.QueryParameters = new QueryKeyRelayRequestRequestBuilder.QueryKeyRelayRequestRequestBuilderGetQueryParameters
			{
				CallingDevice = callingDevice,
			};
		}, cancellationToken), cancellationToken);

		if (response?.QueryKeyRelayRequestGetResponseMember1?.Data?.Request?.QueryKeyRelayRequestGetResponseMember1DataRequestMember1 is { } relayRequest)
		{
			return ApiResult<KeyRelayRequest?>.Success(new KeyRelayRequest
			{
				ExchangeId = relayRequest.ExchangeId ?? Guid.Empty,
				CallingDeviceId = relayRequest.CallingDevice ?? Guid.Empty,
			});
		}

		return ApiResult<KeyRelayRequest?>.Success(null);
	}

	public async Task<ApiResult<Guid?>> RequestKeyRelayAsync(Guid targetDevice, Guid callingDevice, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.RequestKeyRelay.PostAsRequestKeyRelayPostResponseAsync(new RequestKeyRelayPostRequestBody
		{
			TargetDevice = targetDevice,
			CallingDevice = callingDevice,
		}, cancellationToken: cancellationToken), cancellationToken);

		if (response?.RequestKeyRelayPostResponseMember1?.Data?.ExchangeId is { } exchangeId)
		{
			return ApiResult<Guid?>.Success(exchangeId);
		}

		var error = response?.RequestKeyRelayPostResponseMember2?.Message ?? "Failed to request key relay.";
		return ApiResult<Guid?>.Failure(error);
	}

	public async Task<ApiResult<KeyRelayResult?>> ReceiveKeyRelayAsync(Guid exchangeId, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.ReceiveKeyRelay.PostAsReceiveKeyRelayPostResponseAsync(new ReceiveKeyRelayPostRequestBody
		{
			ExchangeId = exchangeId,
		}, cancellationToken: cancellationToken), cancellationToken);

		if (response?.ReceiveKeyRelayPostResponseMember1?.Data?.Relay?.ReceiveKeyRelayPostResponseMember1DataRelayMember1 is { } relay)
		{
			return ApiResult<KeyRelayResult?>.Success(new KeyRelayResult
			{
				DeviceId = relay.DeviceId ?? Guid.Empty,
				DeviceKey = Convert.FromHexString(relay.DeviceKey ?? string.Empty),
			});
		}

		return ApiResult<KeyRelayResult?>.Success(null);
	}

	public async Task<ApiResult<bool>> ProvideKeyRelayAsync(Guid exchangeId, string deviceKeyHex, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.ProvideKeyRelay.PostAsProvideKeyRelayPostResponseAsync(new ProvideKeyRelayPostRequestBody
		{
			ExchangeId = exchangeId,
			DeviceKey = deviceKeyHex,
		}, cancellationToken: cancellationToken), cancellationToken);

		if (response?.ProvideKeyRelayPostResponseMember1 is not null)
		{
			return ApiResult<bool>.Success(true);
		}

		var error = response?.ProvideKeyRelayPostResponseMember2?.Message ?? "Failed to provide key relay.";
		return ApiResult<bool>.Failure(error);
	}

	public async Task<ApiResult<bool>> DenyKeyRelayAsync(Guid exchangeId, CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.DenyKeyRelay.PostAsDenyKeyRelayPostResponseAsync(new DenyKeyRelayPostRequestBody
		{
			ExchangeId = exchangeId,
		}, cancellationToken: cancellationToken), cancellationToken);

		if (response?.DenyKeyRelayPostResponseMember1 is not null)
		{
			return ApiResult<bool>.Success(true);
		}

		var error = response?.DenyKeyRelayPostResponseMember2?.Message ?? "Failed to deny key relay.";
		return ApiResult<bool>.Failure(error);
	}

	public async Task<ApiResult<bool>> EmitHeartbeatAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.EmitHeartbeat.PostAsEmitHeartbeatPostResponseAsync(new EmitHeartbeatPostRequestBody(), cancellationToken: cancellationToken), cancellationToken);
		if (response?.EmitHeartbeatPostResponseMember1 is not null)
		{
			return ApiResult<bool>.Success(true);
		}

		var error = response?.EmitHeartbeatPostResponseMember2?.Message ?? "Failed to emit heartbeat.";
		return ApiResult<bool>.Failure(error);
	}

	public async Task<ApiResult<bool>> SignOutAsync(CancellationToken cancellationToken = default)
	{
		var response = await client.SendAsync(x => x.User.SignOut.PostAsSignOutPostResponseAsync(new SignOutPostRequestBody(), cancellationToken: cancellationToken), cancellationToken);
		if (response?.SignOutPostResponseMember1 is not null)
		{
			return ApiResult<bool>.Success(true);
		}

		var error = response?.SignOutPostResponseMember2?.Message ?? "Failed to sign out.";
		return ApiResult<bool>.Failure(error);
	}

	public async Task<Riverside.Elapsed.App.Models.User.User?> HydrateUserAsync(Riverside.Elapsed.App.Models.User.User? user, CancellationToken cancellationToken = default)
	{
		if (user is null || string.IsNullOrWhiteSpace(user.UserId))
		{
			return null;
		}

		var query = await QueryUserAsync(id: user.UserId, cancellationToken: cancellationToken);
		return query.IsSuccess && query.Value is not null
			? ApiMappingExtensions.MapUser(query.Value)
			: user;
	}
}
