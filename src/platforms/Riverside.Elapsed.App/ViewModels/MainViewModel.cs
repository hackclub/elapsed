using System.Collections.ObjectModel;
using Riverside.Elapsed.App.Services.Api;
using Riverside.Elapsed.App.Services.Auth;
using Riverside.Elapsed.App.Services.Build;

namespace Riverside.Elapsed.App.ViewModels;

/// <summary>
/// Backs the home page — leaderboard, explore timelapses, and the welcome banner shown to
/// signed-out viewers.
/// </summary>
public partial class MainViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly ILapseAuthService _authService;
	private readonly IApiGlobalService _globalService;
	private readonly BuildInfo _buildInfo;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private string? _errorMessage;

	[ObservableProperty]
	private string _searchText = string.Empty;

	[ObservableProperty]
	private bool _isAuthenticated;

	public MainViewModel(
		INavigator navigator,
		ILapseAuthService authService,
		IApiGlobalService globalService,
		IBuildInfoProvider buildInfoProvider)
	{
		_navigator = navigator;
		_authService = authService;
		_globalService = globalService;
		_buildInfo = buildInfoProvider.GetBuildInfo();

		IsAuthenticated = _authService.IsAuthenticated;
		_authService.LoggedIn += (_, _) => IsAuthenticated = true;
		_authService.LoggedOut += (_, _) => IsAuthenticated = false;

		RefreshCommand = new AsyncRelayCommand(LoadAsync);
		LogoutCommand = new AsyncRelayCommand(LogoutAsync);
		OpenRecordingCommand = new AsyncRelayCommand(() => _navigator.NavigateViewModelAsync<RecordingViewModel>(this));
		OpenTimelapseCommand = new AsyncRelayCommand<TimelapseCardViewModel>(OpenTimelapseAsync);
		OpenProfileCommand = new AsyncRelayCommand<LeaderboardEntryViewModel>(OpenProfileAsync);
		SignInCommand = new AsyncRelayCommand(SignInAsync);
	}

	public ObservableCollection<LeaderboardEntryViewModel> LeaderboardEntries { get; } = [];

	public ObservableCollection<TimelapseCardViewModel> ExploreTimelapses { get; } = [];

	public bool IsWebPlatform => OperatingSystem.IsBrowser();

	public string FooterText => _buildInfo.FullFooterText;

	public string WebFooterText => _buildInfo.WebFooterText;

	public string GreetingTitle => "Welcome to Elapsed, Hack Club's timelapse tracking tool!";

	public string GreetingSubtitle => "Sign in to start tracking your own time with Elapsed";

	public IAsyncRelayCommand RefreshCommand { get; }

	public IAsyncRelayCommand LogoutCommand { get; }

	public IAsyncRelayCommand OpenRecordingCommand { get; }

	public IAsyncRelayCommand<TimelapseCardViewModel> OpenTimelapseCommand { get; }

	public IAsyncRelayCommand<LeaderboardEntryViewModel> OpenProfileCommand { get; }

	public IAsyncRelayCommand SignInCommand { get; }

	public async Task InitializeAsync()
	{
		if (LeaderboardEntries.Count == 0 && ExploreTimelapses.Count == 0)
		{
			await LoadAsync();
		}
	}

	private async Task LoadAsync()
	{
		IsLoading = true;
		ErrorMessage = null;

		try
		{
			var leaderboardTask = _globalService.GetWeeklyLeaderboardAsync();
			var recentTask = _globalService.GetRecentTimelapsesAsync();

			var leaderboardResult = await leaderboardTask;
			var recentResult = await recentTask;

			LeaderboardEntries.Clear();
			if (leaderboardResult.IsSuccess && leaderboardResult.Value is not null)
			{
				foreach (var entry in leaderboardResult.Value)
				{
					LeaderboardEntries.Add(LeaderboardEntryViewModel.FromModel(entry));
				}
			}

			ExploreTimelapses.Clear();
			if (recentResult.IsSuccess && recentResult.Value is not null)
			{
				foreach (var timelapse in recentResult.Value)
				{
					ExploreTimelapses.Add(TimelapseCardViewModel.FromModel(timelapse));
				}
			}

			if (!leaderboardResult.IsSuccess || !recentResult.IsSuccess)
			{
				ErrorMessage = leaderboardResult.ErrorMessage ?? recentResult.ErrorMessage ?? "Some content could not be loaded.";
			}
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task LogoutAsync()
	{
		await _authService.LogoutAsync();
		await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
	}

	private async Task OpenTimelapseAsync(TimelapseCardViewModel? card)
	{
		if (card is null || string.IsNullOrWhiteSpace(card.TimelapseId))
		{
			return;
		}

		await _navigator.NavigateViewModelAsync<VideoViewModel>(this, data: card);
	}

	private async Task OpenProfileAsync(LeaderboardEntryViewModel? entry)
	{
		if (entry is null || string.IsNullOrWhiteSpace(entry.UserId))
		{
			return;
		}

		await _navigator.NavigateViewModelAsync<UserProfileViewModel>(this, data: entry.UserId);
	}

	private async Task SignInAsync()
	{
		await _navigator.NavigateViewModelAsync<LoginViewModel>(this);
	}
}
