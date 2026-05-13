namespace Riverside.Elapsed.App.Models.User;

public sealed class Myself : User
{
	public IReadOnlyList<Device> Devices { get; set; } = Array.Empty<Device>();
	public bool NeedsReauth { get; set; }
	public PermissionLevel PermissionLevel { get; set; }
}
