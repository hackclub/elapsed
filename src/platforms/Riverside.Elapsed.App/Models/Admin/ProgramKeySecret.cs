namespace Riverside.Elapsed.App.Models.Admin;

public sealed class ProgramKeySecret
{
	public ProgramKeyMetadata Key { get; set; } = new();
	public string RawKey { get; set; } = string.Empty;
}
