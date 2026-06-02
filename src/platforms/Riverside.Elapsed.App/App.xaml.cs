using System.Diagnostics.CodeAnalysis;
using Riverside.Elapsed.App.Services.Recording;
using Riverside.Elapsed.App.Services.Upload;
using Riverside.Elapsed.App.ViewModels;
using Uno.Resizetizer;

namespace Riverside.Elapsed.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	public static Window? MainWindow { get; private set; }

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
					// .Section<AppConfig>()
					)
				.UseLocalization()
				.ConfigureServices((context, services) => { })
				//.UseNavigation(RegisterRoutes)
				.UseSerialization(serialization => serialization.AddSingleton(Constants.SerializerOptions))
			);

		MainWindow = builder.Window;

#if DEBUG
		MainWindow.UseStudio();
#endif
		MainWindow.SetWindowIcon();

		ICaptureSourceProvider sourceProvider;

		if (OperatingSystem.IsWindows())
			sourceProvider = new WindowsCaptureSourceProvider();
		else if (OperatingSystem.IsMacOS())
			sourceProvider = new MacCaptureSourceProvider();
		else if (OperatingSystem.IsLinux())
			sourceProvider = new LinuxCaptureSourceProvider();
		else
			sourceProvider = new NoOpCaptureSourceProvider();

		IRecordingFacade recording = new TimelapseRecordingFacade(sourceProvider);
		/*
		IRecordingFacade recording = new NoOpRecordingFacade();
		ICaptureSourceProvider sourceProvider = new NoOpCaptureSourceProvider();
		*/

		var lapse = new LapseService();

		MainWindow.Content = new RecordingPage
		{
			//var authService = services.GetRequiredService<ILapseAuthService>();
			//await authService.TryRestoreSessionAsync();
			//await navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.Nested);
			DataContext = new RecordingViewModel(recording, sourceProvider, lapse)
		};
		MainWindow.Activate();
	}

	/*
	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap<Shell, ShellViewModel>(),
			//new ViewMap<LoginPage, LoginViewModel>(),
			//new ViewMap<MainPage, MainViewModel>()
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
					//new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault: true),
					//new RouteMap("Video", View: views.FindByViewModel<PlayerViewModel>()),
					//new RouteMap("Recording", View: views.FindByViewModel<RecordingViewModel>()),
					//new RouteMap("UserProfile", View: views.FindByViewModel<UserProfileViewModel>()),
				]
			)
		);
	} */
}
