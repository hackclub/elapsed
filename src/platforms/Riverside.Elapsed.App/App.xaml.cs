using System.Diagnostics.CodeAnalysis;
using Riverside.Elapsed.App.Extensions;
using Riverside.Elapsed.App.Services.Api;
using Riverside.Elapsed.App.Services.Auth;
using Riverside.Elapsed.App.Services.Build;
using Riverside.Elapsed.App.Services.Storage;
using Riverside.Elapsed.App.ViewModels;
using Uno.Resizetizer;

namespace Riverside.Elapsed.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected Window? MainWindow { get; private set; }
	protected IHost? Host { get; private set; }

	// shell.xaml.cs reaches for this when wiring the custom title bar so it can call
	// Window.SetTitleBar(...) without having to plumb the window reference through
	// navigation. browser builds skip the call entirely.
	internal static Window? CurrentMainWindow { get; private set; }

	[SuppressMessage("Trimming", "IL2026", Justification = "Uno app builder usage is trim-safe for configured features.")]
	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		var builder = this.CreateBuilder(args)
			.UseToolkitNavigation()
			.Configure(host => host
#if DEBUG
				.UseEnvironment(Environments.Development)
#endif
				.UseLogging((context, logBuilder) =>
				{
					logBuilder
						.SetMinimumLevel(context.HostingEnvironment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning)
						.CoreLogLevel(LogLevel.Warning);
				}, enableUnoLogging: true)
				.UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
				.UseConfiguration(configure: (Uno.Extensions.Configuration.IConfigBuilder configBuilder) => configBuilder
					.EmbeddedSource<App>()
					.Section<AppConfig>())
				.UseLocalization()
				.ConfigureServices((context, services) =>
				{
#if DEBUG
					services.AddTransient<DelegatingHandler, DebugHttpHandler>();
#endif
					services.AddSingleton<ILocalJsonStore, LocalJsonStore>();
					services.AddSingleton<IAuthTokenStore, AuthTokenStore>();
					services.AddSingleton<ILapseAuthService, LapseAuthService>();
					services.AddSingleton<IBuildInfoProvider, BuildInfoProvider>();

#if HAS_MEDIA_RECORDING
					services.AddSingleton<Riverside.Elapsed.App.Services.Recording.IRecordingFacade>(_ =>
						OperatingSystem.IsWindows()
							? new Riverside.Elapsed.App.Services.Recording.WindowsRecordingFacade()
							: new Riverside.Elapsed.App.Services.Recording.NoOpRecordingFacade());
#else
					services.AddSingleton<Riverside.Elapsed.App.Services.Recording.IRecordingFacade, Riverside.Elapsed.App.Services.Recording.NoOpRecordingFacade>();
#endif

					services.AddScoped<IApiClientFacade, ApiClientFacade>();
					services.AddScoped<IApiUserService, ApiUserService>();
					services.AddScoped<IApiGlobalService, ApiGlobalService>();
					services.AddScoped<IApiDeveloperService, ApiDeveloperService>();

					services.AddDrafts();
				})
				.UseNavigation(RegisterRoutes)
				.UseSerialization(serialization => serialization.AddSingleton(Constants.SerializerOptions))
			);

		MainWindow = builder.Window;
		CurrentMainWindow = MainWindow;

#if DEBUG
		MainWindow.UseStudio();
#endif
		MainWindow.SetWindowIcon();

		Host = await builder.NavigateAsync<Shell>(initialNavigate: async (services, navigator) =>
		{
			// always open the main page. the main page shows a welcome banner for signed-out users;
			// the login page is a secondary screen reached via the sign-in button.
			var authService = services.GetRequiredService<ILapseAuthService>();
			await authService.TryRestoreSessionAsync();
			await navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.Nested);
		});
	}

	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
				new ViewMap<Shell, ShellViewModel>(),
			new ViewMap<LoginPage, LoginViewModel>(),
			new ViewMap<MainPage, MainViewModel>(),
			new ViewMap<VideoPage, VideoViewModel>(),
			new ViewMap<RecordingPage, RecordingViewModel>(),
			new ViewMap<UserProfilePage, UserProfileViewModel>()
		);

		routes.Register(
			new RouteMap(
				"",
				View: views.FindByViewModel<ShellViewModel>(),
				Nested:
				[
					new RouteMap("Login", View: views.FindByViewModel<LoginViewModel>()),
					new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault: true),
					new RouteMap("Video", View: views.FindByViewModel<VideoViewModel>()),
					new RouteMap("Recording", View: views.FindByViewModel<RecordingViewModel>()),
					new RouteMap("UserProfile", View: views.FindByViewModel<UserProfileViewModel>()),
				]
			)
		);
	}
}
