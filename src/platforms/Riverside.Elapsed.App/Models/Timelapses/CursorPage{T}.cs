namespace Riverside.Elapsed.App.Models.Timelapses;

public class CursorPage<T> // infinite scroll
{
	public IReadOnlyList<T> Items { get; set; } = Array.Empty<T>();
	public string? NextCursor { get; set; }
}
