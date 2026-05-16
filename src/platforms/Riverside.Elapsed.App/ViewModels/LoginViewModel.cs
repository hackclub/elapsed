using Riverside.Elapsed.App.Services.Auth;

namespace Riverside.Elapsed.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly ILapseAuthService _authService;

	[ObservableProperty]
	private bool _isWorking;

	[ObservableProperty]
	[NotifyPropertyChangedFor(nameof(HasMessage))]
	private string? _message;

	public bool HasMessage => !string.IsNullOrWhiteSpace(Message);

	public LoginViewModel(INavigator navigator, ILapseAuthService authService)
	{
		_navigator = navigator;
		_authService = authService;
		LoginCommand = new AsyncRelayCommand(LoginAsync, () => !IsWorking && IsLoginSupported);

		// surface a clear, non-blocking message up-front on the web preview \u2014 lapse only
		// allows a registered localhost redirect, so browser auth currently 500s and would
		// hang here forever otherwise.
		if (!IsLoginSupported)
		{
			Message = "Sign-in is not yet available on the web preview. Please use the desktop app to authenticate with Lapse.";
		}
	}

	public string Title => "Sign in";

	public string Description => IsLoginSupported
		? "Authenticate with Lapse to continue."
		: "The web preview cannot complete the Lapse authentication flow yet.";

	public bool IsLoginSupported => !OperatingSystem.IsBrowser();

	public IAsyncRelayCommand LoginCommand { get; }

	partial void OnIsWorkingChanged(bool value) => LoginCommand.NotifyCanExecuteChanged();

	private async Task LoginAsync()
	{
		if (!IsLoginSupported)
		{
			return;
		}

		IsWorking = true;
		Message = null;
		try
		{
			var result = await _authService.LoginAsync();
			if (result.IsSuccess)
			{
				await _navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.ClearBackStack);
				return;
			}

			if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
			{
				Message = result.ErrorMessage;
			}
		}
		finally
		{
			IsWorking = false;
		}
	}
}
