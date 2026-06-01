using Riverside.Elapsed.App.Models.Recording;
using Riverside.Elapsed.App.ViewModels;

namespace Riverside.Elapsed.App.Views;

public sealed partial class RecordingPage : Page
{
	private const int CompactWidth = 340;
	private const int CompactHeight = 460;
	private const int ExpandedWidth = 800;
	private const int ExpandedHeight = 460;

	public RecordingPage()
	{
		this.InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		SetWindowSize(CompactWidth, CompactHeight);

		if (DataContext is RecordingViewModel vm)
		{
			vm.RecordingStarted += OnRecordingStarted;
			vm.RecordingStopped += OnRecordingStopped;

			ScreenRadio.Checked += (_, _) => vm.SelectedSourceKind = CaptureSourceKind.Screen;
			WindowRadio.Checked += (_, _) => vm.SelectedSourceKind = CaptureSourceKind.Window;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (DataContext is RecordingViewModel vm)
		{
			vm.RecordingStarted -= OnRecordingStarted;
			vm.RecordingStopped -= OnRecordingStopped;
		}
	}

	private void OnRecordingStarted(object? sender, EventArgs e)
	{
		SetWindowSize(ExpandedWidth, ExpandedHeight);
	}

	private void OnRecordingStopped(object? sender, EventArgs e)
	{
		SetWindowSize(CompactWidth, CompactHeight);
	}

	private static void SetWindowSize(int width, int height)
	{
		var window = App.CurrentMainWindow;
		if (window is null) return;

		var scale = window.Content?.XamlRoot?.RasterizationScale ?? 1.0;
		var scaledWidth = (int)(width * scale);
		var scaledHeight = (int)(height * scale);

		window.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = scaledWidth, Height = scaledHeight });
	}
}
