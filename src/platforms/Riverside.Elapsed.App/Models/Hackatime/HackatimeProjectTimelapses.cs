namespace Riverside.Elapsed.App.Models.Hackatime;

public sealed class HackatimeProjectTimelapses
{
	public double Count { get; set; }
	public IReadOnlyList<Timelapses.Timelapse> Timelapses { get; set; } = Array.Empty<Timelapses.Timelapse>();
}
