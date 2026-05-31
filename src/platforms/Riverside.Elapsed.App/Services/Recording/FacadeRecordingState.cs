namespace Riverside.Elapsed.App.Services.Recording;

/// <summary>
/// Indicates the lifecycle state of an <see cref="IRecordingFacade"/>.
/// </summary>
public enum FacadeRecordingState
{
	/// <summary>The facade is ready, no session is active.</summary>
	Idle,
	/// <summary>A recording session is currently capturing frames.</summary>
	Recording,
	/// <summary>A recording session is paused.</summary>
	Paused,
	/// <summary>A recording session was stopped; the output file is finalised.</summary>
	Stopped,
	/// <summary>Recording is unsupported on the current platform.</summary>
	Unsupported,
}
