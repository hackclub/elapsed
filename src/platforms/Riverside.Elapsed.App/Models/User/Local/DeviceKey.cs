#if false
namespace Riverside.Elapsed.App.Models.User.Local;

[ImplicitKeys(IsEnabled = false)]
public record DeviceKey(Guid DeviceId, byte[] Key, DateTimeOffset CreatedAt);

#endif
