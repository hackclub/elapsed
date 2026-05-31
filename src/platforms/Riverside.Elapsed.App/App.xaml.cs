using System.Diagnostics.CodeAnalysis;
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
				.UseConfiguration(configure: (IConfigBuilder configBuilder) => configBuilder
					.EmbeddedSource<App>()
					.Section<AppConfig>())
				.UseLocalization()
				.ConfigureServices((context, services) => { })
				.UseNavigation(RegisterRoutes)
				.UseSerialization(serialization => serialization.AddSingleton(Constants.SerializerOptions))
			);

		MainWindow = builder.Window;

#if DEBUG
		MainWindow.UseStudio();
#endif
		MainWindow.SetWindowIcon();

		Host = await builder.NavigateAsync<Shell>(initialNavigate: async (services, navigator) =>
		{
			//var authService = services.GetRequiredService<ILapseAuthService>();
			//await authService.TryRestoreSessionAsync();
			await navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.Nested);
		});
	}

	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<Shell, ShellViewModel>(),
			//new ViewMap<LoginPage, LoginViewModel>(),
			new ViewMap<MainPage, MainViewModel>()
			//new ViewMap<VideoPage, PlayerViewModel>(),
			//new ViewMap<RecordingPage, RecordingViewModel>(),
			//new ViewMap<UserProfilePage, UserProfileViewModel>()
		);

		routes.Register(
			new RouteMap(
				"",
				View: views.FindByViewModel<ShellViewModel>(),
				Nested:
				[
					//new RouteMap("Login", View: views.FindByViewModel<LoginViewModel>()),
					new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault: true),
					//new RouteMap("Video", View: views.FindByViewModel<PlayerViewModel>()),
					//new RouteMap("Recording", View: views.FindByViewModel<RecordingViewModel>()),
					//new RouteMap("UserProfile", View: views.FindByViewModel<UserProfileViewModel>()),
				]
			)
		);
	}
}
