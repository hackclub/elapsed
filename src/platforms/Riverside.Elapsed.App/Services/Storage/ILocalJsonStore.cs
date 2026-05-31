namespace Riverside.Elapsed.App.Services.Storage;

public interface ILocalJsonStore
{
	Task<T?> ReadAsync<T>(string relativePath, CancellationToken ct = default);
	Task WriteAsync<T>(string relativePath, T value, CancellationToken ct = default);
	Task<bool> ExistsAsync(string relativePath, CancellationToken ct = default);
	Task DeleteAsync(string relativePath, CancellationToken ct = default);
}
