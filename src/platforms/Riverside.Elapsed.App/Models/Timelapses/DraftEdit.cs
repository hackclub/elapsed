namespace Riverside.Elapsed.App.Models.Timelapses;

public sealed class DraftEdit
{
	public double BeginSeconds { get; set; }
	public double EndSeconds { get; set; }
	public EditKind Kind { get; set; }
}
