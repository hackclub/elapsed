using Riverside.Elapsed.App.Services.Api;
using Riverside.Elapsed.App.Services.Auth;
using Riverside.Elapsed.App.Services.Build;

namespace Riverside.Elapsed.App.ViewModels;

/// <summary>
/// Backs the shell-level chrome on desktop platforms: the title bar with back arrow,
/// hamburger menu, brand wordmark, search box, "new timelapse session" split button,
/// and the signed-in user avatar.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly ILapseAuthService _authService;
	private readonly IApiUserService _userService;

	[ObservableProperty]
	private bool _isAuthenticated;

	[ObservableProperty]
	private string _searchText = string.Empty;

	[ObservableProperty]
	private string _avatarInitials = "?";

	[ObservableProperty]
	private Uri? _avatarUrl;

	[ObservableProperty]
	private string _signedInDisplayName = string.Empty;

	[ObservableProperty]
	private string _signedInHandle = string.Empty;

	private string? _signedInUserId;

	public ShellViewModel(ILapseAuthService authService, IApiUserService userService, INavigator navigator, IBuildInfoProvider buildInfoProvider)
	{
		_navigator = navigator;
		_authService = authService;
		_userService = userService;

		var info = buildInfoProvider.GetBuildInfo();
		BrandingPrimary = "Elapsed";
		BrandingSecondary = string.IsNullOrEmpty(info.DisplayVersion) || !info.DisplayVersion.Contains('-')
			? string.Empty
			: "Preview";

		SignInCommand = new AsyncRelayCommand(SignInAsync);
		OpenRecordingCommand = new AsyncRelayCommand(OpenRecordingAsync);
		RecordRegionCommand = new AsyncRelayCommand(OpenRecordingAsync);
		UploadExistingCommand = new AsyncRelayCommand(OpenRecordingAsync);
		LogoutCommand = new AsyncRelayCommand(LogoutAsync);
		BackCommand = new AsyncRelayCommand(BackAsync);
		OpenHomeCommand = new AsyncRelayCommand(OpenHomeAsync);
		OpenMyProfileCommand = new AsyncRelayCommand(OpenMyProfileAsync);

		IsAuthenticated = _authService.IsAuthenticated;
		_authService.LoggedIn += OnLoggedIn;
		_authService.LoggedOut += OnLoggedOut;

		if (IsAuthenticated)
		{
			_ = LoadProfileAsync();
		}
	}

	public string BrandingPrimary { get; }

	public string BrandingSecondary { get; }

	public bool HasBrandingSecondary => !string.IsNullOrEmpty(BrandingSecondary);

	public string SearchPlaceholder => "Search";

	public bool IsSearchEnabled => true;

	public bool IsWebPlatform => OperatingSystem.IsBrowser();

	/// <summary>The full desktop chrome (back button, hamburger, search, split button, avatar)
	/// is shown only on non-browser hosts; on the web the browser already provides chrome.</summary>
	public bool IsDesktopChromeVisible => !OperatingSystem.IsBrowser();

	public IAsyncRelayCommand SignInCommand { get; }

	public IAsyncRelayCommand OpenRecordingCommand { get; }

	public IAsyncRelayCommand RecordRegionCommand { get; }

	public IAsyncRelayCommand UploadExistingCommand { get; }

	public IAsyncRelayCommand LogoutCommand { get; }

	public IAsyncRelayCommand BackCommand { get; }

	public IAsyncRelayCommand OpenHomeCommand { get; }

	public IAsyncRelayCommand OpenMyProfileCommand { get; }

	private void OnLoggedIn(object? sender, EventArgs e)
	{
		IsAuthenticated = true;
		_ = LoadProfileAsync();
	}

	private async void OnLoggedOut(object? sender, EventArgs e)
	{
		IsAuthenticated = false;
		_signedInUserId = null;
		AvatarInitials = "?";
		AvatarUrl = null;
		SignedInDisplayName = string.Empty;
		SignedInHandle = string.Empty;
		await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
	}

	private async Task LoadProfileAsync()
	{
		var result = await _userService.GetMyselfAsync();
		if (result.IsSuccess && result.Value is { } user)
		{
			_signedInUserId = user.UserId;
			SignedInDisplayName = user.DisplayName;
			SignedInHandle = string.IsNullOrWhiteSpace(user.Handle) ? string.Empty : $"@{user.Handle}";
			AvatarUrl = user.ProfilePictureUrl;
			AvatarInitials = ComputeInitials(user.DisplayName);
		}
	}

	private static string ComputeInitials(string displayName)
	{
		if (string.IsNullOrWhiteSpace(displayName))
		{
			return "?";
		}

		var parts = displayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (parts.Length == 1)
		{
			return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();
		}

		return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
	}

	private async Task SignInAsync()
		=> await _navigator.NavigateViewModelAsync<LoginViewModel>(this);

	private async Task OpenRecordingAsync()
		=> await _navigator.NavigateViewModelAsync<RecordingViewModel>(this);

	private async Task OpenHomeAsync()
		=> await _navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.ClearBackStack);

	private async Task OpenMyProfileAsync()
	{
		if (string.IsNullOrWhiteSpace(_signedInUserId))
		{
			return;
		}

		await _navigator.NavigateViewModelAsync<UserProfileViewModel>(this, data: _signedInUserId);
	}

	private Task LogoutAsync()
		=> _authService.LogoutAsync();

	private async Task BackAsync()
		=> await _navigator.NavigateBackAsync(this);
}
