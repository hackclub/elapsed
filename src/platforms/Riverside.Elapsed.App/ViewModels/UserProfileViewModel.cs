using System.Collections.ObjectModel;
using Riverside.Elapsed.App.Models.User;
using Riverside.Elapsed.App.Services.Api;

namespace Riverside.Elapsed.App.ViewModels;

/// <summary>
/// Backs the user profile page: avatar, bio, links, action buttons, and the user's own
/// recent timelapses grid.
/// </summary>
public partial class UserProfileViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly IApiUserService _userService;
	private readonly IApiGlobalService _globalService;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private string? _errorMessage;

	[ObservableProperty]
	private string _displayName = string.Empty;

	[ObservableProperty]
	private string _handleText = string.Empty;

	[ObservableProperty]
	private string _bio = string.Empty;

	[ObservableProperty]
	private string _joinedText = string.Empty;

	[ObservableProperty]
	private Uri? _profilePictureUrl;

	[ObservableProperty]
	private string _primaryWebsiteText = string.Empty;

	[ObservableProperty]
	private Uri? _primaryWebsiteUrl;

	[ObservableProperty]
	private bool _hasWebsite;

	[ObservableProperty]
	private bool _hasHackatime;

	[ObservableProperty]
	private bool _hasSlack;

	private string? _userId;
	private string? _hackatimeId;
	private string? _slackId;

	public UserProfileViewModel(INavigator navigator, IApiUserService userService, IApiGlobalService globalService)
	{
		_navigator = navigator;
		_userService = userService;
		_globalService = globalService;
		Timelapses = [];

		BackCommand = new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
		OpenWebCommand = new AsyncRelayCommand(OpenWebAsync);
		OpenHackatimeCommand = new AsyncRelayCommand(OpenHackatimeAsync, () => HasHackatime);
		OpenSlackCommand = new AsyncRelayCommand(OpenSlackAsync, () => HasSlack);
		OpenWebsiteCommand = new AsyncRelayCommand(OpenWebsiteAsync, () => HasWebsite);
		OpenTimelapseCommand = new AsyncRelayCommand<TimelapseCardViewModel>(OpenTimelapseAsync);
	}

	public ObservableCollection<TimelapseCardViewModel> Timelapses { get; }

	public IAsyncRelayCommand BackCommand { get; }

	public IAsyncRelayCommand OpenWebCommand { get; }

	public IAsyncRelayCommand OpenHackatimeCommand { get; }

	public IAsyncRelayCommand OpenSlackCommand { get; }

	public IAsyncRelayCommand OpenWebsiteCommand { get; }

	public IAsyncRelayCommand<TimelapseCardViewModel> OpenTimelapseCommand { get; }

	public async Task OnNavigatedToAsync(string userId)
	{
		_userId = userId;
		await LoadAsync();
	}

	private async Task LoadAsync()
	{
		if (string.IsNullOrWhiteSpace(_userId))
		{
			ErrorMessage = "Missing user.";
			return;
		}

		IsLoading = true;
		ErrorMessage = null;
		try
		{
			var userResult = await _userService.QueryUserAsync(id: _userId);
			if (!userResult.IsSuccess || userResult.Value is null)
			{
				ErrorMessage = userResult.ErrorMessage ?? "Could not load user.";
				return;
			}

			ApplyUser(userResult.Value);

			// for v0 we populate the user's timelapse grid from the global "recent" feed,
			// filtered to this owner; the dedicated user-timelapses endpoint will follow.
			var recent = await _globalService.GetRecentTimelapsesAsync();
			Timelapses.Clear();
			if (recent.IsSuccess && recent.Value is not null)
			{
				foreach (var timelapse in recent.Value)
				{
					if (string.Equals(timelapse.Owner.UserId, _userId, StringComparison.Ordinal))
					{
						Timelapses.Add(TimelapseCardViewModel.FromModel(timelapse));
					}
				}
			}
		}
		finally
		{
			IsLoading = false;
		}
	}

	private void ApplyUser(UserDetails user)
	{
		DisplayName = user.DisplayName;
		HandleText = string.IsNullOrWhiteSpace(user.Handle) ? string.Empty : $"@{user.Handle}";
		Bio = user.Bio;
		ProfilePictureUrl = user.ProfilePictureUrl;
		JoinedText = user.CreatedAt == default ? string.Empty : $"Joined {user.CreatedAt:MMMM yyyy}";

		_hackatimeId = user.HackatimeId;
		_slackId = user.SlackId;
		HasHackatime = !string.IsNullOrWhiteSpace(_hackatimeId);
		HasSlack = !string.IsNullOrWhiteSpace(_slackId);

		var primary = user.Urls?.FirstOrDefault();
		PrimaryWebsiteUrl = primary;
		HasWebsite = primary is not null;
		PrimaryWebsiteText = primary?.Host ?? string.Empty;

		OpenHackatimeCommand.NotifyCanExecuteChanged();
		OpenSlackCommand.NotifyCanExecuteChanged();
		OpenWebsiteCommand.NotifyCanExecuteChanged();
	}

	private async Task OpenWebAsync()
	{
		if (string.IsNullOrWhiteSpace(_userId))
		{
			return;
		}

		await LaunchAsync($"https://lapse.hackclub.com/users/{_userId}");
	}

	private Task OpenHackatimeAsync()
		=> string.IsNullOrWhiteSpace(_hackatimeId)
			? Task.CompletedTask
			: LaunchAsync($"https://hackatime.hackclub.com/users/{_hackatimeId}");

	private Task OpenSlackAsync()
		=> string.IsNullOrWhiteSpace(_slackId)
			? Task.CompletedTask
			: LaunchAsync($"https://hackclub.slack.com/team/{_slackId}");

	private Task OpenWebsiteAsync()
		=> PrimaryWebsiteUrl is null ? Task.CompletedTask : LaunchAsync(PrimaryWebsiteUrl.ToString());

	private async Task OpenTimelapseAsync(TimelapseCardViewModel? card)
	{
		if (card is null)
		{
			return;
		}

		await _navigator.NavigateViewModelAsync<VideoViewModel>(this, data: card);
	}

	private static async Task LaunchAsync(string url)
	{
		try
		{
			await Windows.System.Launcher.LaunchUriAsync(new Uri(url));
		}
		catch
		{
			// silently ignored; launcher unavailable on the current host.
		}
	}

	partial void OnHasHackatimeChanged(bool value) => OpenHackatimeCommand.NotifyCanExecuteChanged();

	partial void OnHasSlackChanged(bool value) => OpenSlackCommand.NotifyCanExecuteChanged();

	partial void OnHasWebsiteChanged(bool value) => OpenWebsiteCommand.NotifyCanExecuteChanged();
}
