namespace Riverside.Elapsed.App.Models.Recording;

public sealed record CapturedFrame(byte[] Pixels, int Width, int Height, bool IsBottomUp = false);
