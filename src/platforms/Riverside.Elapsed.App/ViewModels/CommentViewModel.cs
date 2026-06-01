#if false
namespace Riverside.Elapsed.App.ViewModels;

/// <summary>
/// View-model for a single comment row rendered on the video page.
/// </summary>
public sealed class CommentViewModel
{
	/// <summary>Gets the author's display name.</summary>
	public string AuthorName { get; init; } = string.Empty;

	/// <summary>Gets the author's <c>@handle</c> with leading at-sign.</summary>
	public string AuthorHandle { get; init; } = string.Empty;

	/// <summary>Gets the comment body.</summary>
	public string Body { get; init; } = string.Empty;

	/// <summary>Gets the author's profile picture URL.</summary>
	public Uri? AvatarUrl { get; init; }

	/// <summary>Gets the relative posted time (e.g. "3 days ago").</summary>
	public string PostedAgo { get; init; } = string.Empty;
}

#endif
