namespace Riverside.Elapsed.App.Models.Recording;

public sealed class CaptureSource
{
	public required string Id { get; init; }
	public required string Name { get; init; }
	public string? Description { get; init; }
	public string? Resolution { get; set; }
	public required CaptureSourceKind Kind { get; init; }
	public Microsoft.UI.Xaml.Media.ImageSource? Thumbnail { get; set; }
	public Microsoft.UI.Xaml.Media.ImageSource? Icon { get; set; }
}
