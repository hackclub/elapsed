using Riverside.Elapsed.App.Models.User;
using Riverside.Elapsed.App.Models.User.Local;

namespace Riverside.Elapsed.App.Services.Api;

public interface IApiUserService
{
	Task<ApiResult<UserDetails>> GetMyselfAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<UserDetails>> QueryUserAsync(string? id = null, string? handle = null, long? hackatimeId = null, CancellationToken cancellationToken = default);
	Task<ApiResult<UserDetails>> QueryUserByEmailAsync(string email, CancellationToken cancellationToken = default);
	Task<ApiResult<UserDetails>> UpdateUserAsync(string id, string? handle, string? displayName, string? bio, IReadOnlyList<Uri>? urls, CancellationToken cancellationToken = default);
	Task<ApiResult<IReadOnlyList<Device>>> GetDevicesAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<Device>> RegisterDeviceAsync(string name, CancellationToken cancellationToken = default);
	Task<ApiResult<double>> GetTotalTimelapseTimeAsync(string? userId, CancellationToken cancellationToken = default);
	Task<ApiResult<IReadOnlyList<Riverside.Elapsed.App.Models.Hackatime.HackatimeProject>>> GetHackatimeProjectsAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<KeyRelayRequest?>> QueryKeyRelayRequestAsync(Guid callingDevice, CancellationToken cancellationToken = default);
	Task<ApiResult<Guid?>> RequestKeyRelayAsync(Guid targetDevice, Guid callingDevice, CancellationToken cancellationToken = default);
	Task<ApiResult<KeyRelayResult?>> ReceiveKeyRelayAsync(Guid exchangeId, CancellationToken cancellationToken = default);
	Task<ApiResult<bool>> ProvideKeyRelayAsync(Guid exchangeId, string deviceKeyHex, CancellationToken cancellationToken = default);
	Task<ApiResult<bool>> DenyKeyRelayAsync(Guid exchangeId, CancellationToken cancellationToken = default);
	Task<ApiResult<bool>> EmitHeartbeatAsync(CancellationToken cancellationToken = default);
	Task<ApiResult<bool>> SignOutAsync(CancellationToken cancellationToken = default);
	Task<Riverside.Elapsed.App.Models.User.User?> HydrateUserAsync(Riverside.Elapsed.App.Models.User.User? user, CancellationToken cancellationToken = default);
}
