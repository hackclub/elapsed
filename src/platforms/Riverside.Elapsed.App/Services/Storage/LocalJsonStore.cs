namespace Riverside.Elapsed.App.Services.Storage;

public sealed class LocalJsonStore(ISerializer serializer) : ILocalJsonStore
{
	// TODO: inject something better later
	private static string BasePath
		=> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Riverside", "Elapsed");

	private static string FullPath(string relativePath)
		=> Path.Combine(BasePath, relativePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar));

	public Task<bool> ExistsAsync(string relativePath, CancellationToken ct = default)
		=> Task.FromResult(File.Exists(FullPath(relativePath)));

	public Task DeleteAsync(string relativePath, CancellationToken ct = default)
	{
		var path = FullPath(relativePath);
		if (File.Exists(path))
			File.Delete(path);
		return Task.CompletedTask;
	}

	public async Task<T?> ReadAsync<T>(string relativePath, CancellationToken ct = default)
	{
		var path = FullPath(relativePath);
		if (!File.Exists(path))
			return default;

		await using var stream = File.OpenRead(path);
		return (T?)serializer.FromStream(stream, typeof(T));
	}

	public async Task WriteAsync<T>(string relativePath, T value, CancellationToken ct = default)
	{
		var path = FullPath(relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);

		var tmp = path + ".tmp";
		var bak = path + ".bak";

		await using (var stream = File.Create(tmp))
		{
			serializer.ToStream(stream, value!, typeof(T));
			await stream.FlushAsync(ct).ConfigureAwait(false);
		}

		// TODO: ensure compatibility with other systems
		if (File.Exists(path))
		{
			try
			{
				File.Replace(tmp, path, bak, ignoreMetadataErrors: true);
				if (File.Exists(bak))
				{
					File.Delete(bak);
				}
				return;
			}
			catch (PlatformNotSupportedException) { }
			catch (IOException) { }
			File.Delete(path);
		}

		File.Move(tmp, path);
	}
}
