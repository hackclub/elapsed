namespace Riverside.Elapsed.App.Services.Recording;

/// <summary>
/// Describes the artefact produced by <see cref="IRecordingFacade.StopAsync(System.Threading.CancellationToken)"/>.
/// </summary>
/// <param name="FilePath">The local path of the captured media file, if any.</param>
/// <param name="Duration">The total active recording duration (excluding paused intervals).</param>
public sealed record FacadeRecordingResult(string? FilePath, TimeSpan Duration);
