#if false
namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public class DraftPipelineState
{
	public enum Phase
	{
		LocalOnly,
		CreatingRemoteDraft,
		Encrypting,
		Uploading,
		ReadyToPublish,
		Publishing,
		Published,
		Error,
	}

	public Phase CurrentPhase { get; init; } = Phase.LocalOnly;
	public double Progress { get; init; }
	public string? LastError { get; init; }
}

#endif
