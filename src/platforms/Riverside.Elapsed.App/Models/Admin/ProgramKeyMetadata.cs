namespace Riverside.Elapsed.App.Models.Admin;

public sealed class ProgramKeyMetadata
{
	public Guid KeyId { get; set; }
	public string Name { get; set; } = string.Empty;
	public string KeyPrefix { get; set; } = string.Empty;
	public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
	public User.User CreatedBy { get; set; } = new();
	public DateTimeOffset CreatedAt { get; set; }
	public DateTimeOffset? LastUsedAt { get; set; }
	public DateTimeOffset? RevokedAt { get; set; }
	public DateTimeOffset ExpiresAt { get; set; }
}
