using System.Collections.ObjectModel;
using Riverside.Elapsed.App.Services.Api;
using TimelapseModel = Riverside.Elapsed.App.Models.Timelapses.Timelapse;

namespace Riverside.Elapsed.App.ViewModels;

/// <summary>
/// Backs the video page: player, metadata, comments list, and the right-rail of related
/// timelapses.
/// </summary>
public partial class VideoViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly IApiGlobalService _globalService;

	[ObservableProperty]
	private string _title = "Timelapse";

	[ObservableProperty]
	private string _subtitle = string.Empty;

	[ObservableProperty]
	private string _description = string.Empty;

	[ObservableProperty]
	private Uri? _playbackUrl;

	[ObservableProperty]
	private string? _activeTimelapseId;

	[ObservableProperty]
	private string _newCommentText = string.Empty;

	[ObservableProperty]
	private string _commentsHeader = "0 comments on this timelapse";

	public VideoViewModel(INavigator navigator, IApiGlobalService globalService)
	{
		_navigator = navigator;
		_globalService = globalService;
		RelatedTimelapses = [];
		Comments = [];
		BackCommand = new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
		OpenTimelapseCommand = new AsyncRelayCommand<TimelapseCardViewModel>(OpenTimelapseAsync);
		PostCommentCommand = new AsyncRelayCommand(PostCommentAsync, () => !string.IsNullOrWhiteSpace(NewCommentText));
		OpenOwnerCommand = new AsyncRelayCommand(OpenOwnerAsync);
	}

	public ObservableCollection<TimelapseCardViewModel> RelatedTimelapses { get; }

	public ObservableCollection<CommentViewModel> Comments { get; }

	public IAsyncRelayCommand BackCommand { get; }

	public IAsyncRelayCommand<TimelapseCardViewModel> OpenTimelapseCommand { get; }

	public IAsyncRelayCommand PostCommentCommand { get; }

	public IAsyncRelayCommand OpenOwnerCommand { get; }

	public string? OwnerUserId { get; private set; }

	public async Task OnNavigatedToAsync(TimelapseCardViewModel card)
	{
		ActiveTimelapseId = card?.TimelapseId;
		await InitializeAsync();
	}

	public async Task InitializeAsync()
	{
		var timelapseId = ActiveTimelapseId;
		var recent = await _globalService.GetRecentTimelapsesAsync();
		if (!recent.IsSuccess || recent.Value is null)
		{
			return;
		}

		RelatedTimelapses.Clear();
		TimelapseModel? selectedSource = null;
		TimelapseCardViewModel? selectedCard = null;
		foreach (var timelapse in recent.Value)
		{
			var card = TimelapseCardViewModel.FromModel(timelapse);
			RelatedTimelapses.Add(card);
			if (string.Equals(card.TimelapseId, timelapseId, StringComparison.Ordinal))
			{
				selectedSource = timelapse;
				selectedCard = card;
			}
		}

		if (selectedCard is null)
		{
			selectedCard = RelatedTimelapses.FirstOrDefault();
			selectedSource = selectedCard is null ? null : recent.Value.FirstOrDefault(t => t.TimelapseId == selectedCard.TimelapseId);
		}

		if (selectedCard is not null)
		{
			ApplySelectedTimelapse(selectedCard, selectedSource);
		}
	}

	private Task OpenTimelapseAsync(TimelapseCardViewModel? card)
	{
		if (card is null)
		{
			return Task.CompletedTask;
		}

		// resolve original Timelapse from cached related list (we don't re-fetch for v0).
		ApplySelectedTimelapse(card, source: null);
		return Task.CompletedTask;
	}

	private Task PostCommentAsync()
	{
		// for v0 we display posted comments client-side only; backend wiring will follow.
		if (string.IsNullOrWhiteSpace(NewCommentText))
		{
			return Task.CompletedTask;
		}

		Comments.Insert(0, new CommentViewModel
		{
			AuthorName = "You",
			AuthorHandle = "@me",
			Body = NewCommentText.Trim(),
			PostedAgo = "just now",
		});
		NewCommentText = string.Empty;
		CommentsHeader = FormatCommentsHeader(Comments.Count);
		return Task.CompletedTask;
	}

	private async Task OpenOwnerAsync()
	{
		if (string.IsNullOrWhiteSpace(OwnerUserId))
		{
			return;
		}

		await _navigator.NavigateViewModelAsync<UserProfileViewModel>(this, data: OwnerUserId);
	}

	private void ApplySelectedTimelapse(TimelapseCardViewModel card, TimelapseModel? source)
	{
		ActiveTimelapseId = card.TimelapseId;
		Title = card.Title;
		Subtitle = card.OwnerText;
		Description = card.Description;
		PlaybackUrl = card.PlaybackUrl;
		OwnerUserId = card.OwnerUserId;

		Comments.Clear();
		if (source?.Comments is not null)
		{
			foreach (var c in source.Comments)
			{
				Comments.Add(new CommentViewModel
				{
					AuthorName = c.Author.DisplayName,
					AuthorHandle = string.IsNullOrWhiteSpace(c.Author.Handle) ? string.Empty : $"@{c.Author.Handle}",
					Body = c.Content,
					AvatarUrl = c.Author.ProfilePictureUrl,
					PostedAgo = FormatAge(DateTimeOffset.Now - c.CreatedAt),
				});
			}
		}

		CommentsHeader = FormatCommentsHeader(Comments.Count);
	}

	private static string FormatCommentsHeader(int count)
		=> count == 1 ? "1 comment on this timelapse" : $"{count} comments on this timelapse";

	private static string FormatAge(TimeSpan age)
	{
		if (age.TotalDays >= 1)
		{
			var d = (int)age.TotalDays;
			return d == 1 ? "1 day ago" : $"{d} days ago";
		}
		if (age.TotalHours >= 1)
		{
			var h = (int)age.TotalHours;
			return h == 1 ? "1 hour ago" : $"{h} hours ago";
		}
		var m = Math.Max(1, (int)age.TotalMinutes);
		return m == 1 ? "1 minute ago" : $"{m} minutes ago";
	}

	partial void OnNewCommentTextChanged(string value)
		=> PostCommentCommand.NotifyCanExecuteChanged();
}
