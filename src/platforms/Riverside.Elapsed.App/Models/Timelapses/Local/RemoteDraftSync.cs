#if false
namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public class RemoteDraftSync
{
	public string DraftTimelapseId { get; init; } = string.Empty;
	public string IvHex { get; init; } = string.Empty; // draft IV as hex string (not byte[] because json serialiser will get confused)
}

#endif
