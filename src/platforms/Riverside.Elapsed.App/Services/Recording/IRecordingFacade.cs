namespace Riverside.Elapsed.App.Services.Recording;

/// <summary>
/// Cross-platform recording abstraction consumed by <c>RecordingViewModel</c>. Implementations
/// wrap <c>Riverside.MediaRecording</c> on Windows and behave as no-ops elsewhere so the UI
/// surface remains consistent across all platform heads.
/// </summary>
public interface IRecordingFacade
{
	/// <summary>Gets the current recording lifecycle state.</summary>
	FacadeRecordingState State { get; }

	/// <summary>Gets the elapsed active recording duration.</summary>
	TimeSpan Duration { get; }

	/// <summary>Gets a human-readable name of the source the facade will capture (e.g. "Primary display").</summary>
	string? SourceName { get; }

	/// <summary>Gets a value indicating whether recording is supported on the current platform.</summary>
	bool IsSupported { get; }

	/// <summary>Raised when <see cref="State"/> or <see cref="Duration"/> change.</summary>
	event EventHandler? StateChanged;

	/// <summary>Starts a new recording session if one is not already active.</summary>
	Task StartAsync(CancellationToken cancellationToken = default);

	/// <summary>Pauses the active session.</summary>
	Task PauseAsync(CancellationToken cancellationToken = default);

	/// <summary>Resumes the active session if it is paused.</summary>
	Task ResumeAsync(CancellationToken cancellationToken = default);

	/// <summary>Stops the active session and finalises the output file.</summary>
	Task<FacadeRecordingResult> StopAsync(CancellationToken cancellationToken = default);
}
