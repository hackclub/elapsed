namespace Riverside.Elapsed.App.Models.User.Local;

public sealed class KeyRelayResult
{
	public Guid DeviceId { get; set; }
	public byte[] DeviceKey { get; set; } = Array.Empty<byte>();
}
