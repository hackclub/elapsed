namespace Riverside.Elapsed.App.Services.Recording;

internal static class BmpEncoder
{
	public static byte[] Encode(byte[] bgraPixels, int width, int height, bool topDown = false)
	{
		int imageSize = width * height * 4;
		int fileSize = 54 + imageSize;
		var bmp = new byte[fileSize];

		bmp[0] = (byte)'B';
		bmp[1] = (byte)'M';
		BitConverter.TryWriteBytes(bmp.AsSpan(2), fileSize);
		BitConverter.TryWriteBytes(bmp.AsSpan(10), 54);
		BitConverter.TryWriteBytes(bmp.AsSpan(14), 40);
		BitConverter.TryWriteBytes(bmp.AsSpan(18), width);
		BitConverter.TryWriteBytes(bmp.AsSpan(22), topDown ? -height : height);
		BitConverter.TryWriteBytes(bmp.AsSpan(26), (short)1);
		BitConverter.TryWriteBytes(bmp.AsSpan(28), (short)32);
		BitConverter.TryWriteBytes(bmp.AsSpan(34), (uint)imageSize);

		bgraPixels.AsSpan(0, imageSize).CopyTo(bmp.AsSpan(54));
		return bmp;
	}
}
