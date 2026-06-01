using Riverside.Elapsed.App.Models.Recording;
using Riverside.Elapsed.App.ViewModels;

namespace Riverside.Elapsed.App.Views;

public sealed partial class RecordingPage : Page
{
	private const int CompactWidth = 380;
	private const int CompactHeight = 520;
	private const int ExpandedWidth = 840;
	private const int ExpandedHeight = 520;

	private Border? _selectedCard;

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
			vm.FocusRequested += OnFocusRequested;

			ScreenRadio.Checked += (_, _) => vm.SelectedSourceKind = CaptureSourceKind.Screen;
			WindowRadio.Checked += (_, _) => vm.SelectedSourceKind = CaptureSourceKind.Window;
			CameraRadio.Checked += (_, _) => vm.SelectedSourceKind = CaptureSourceKind.Camera;
		}
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (DataContext is RecordingViewModel vm)
		{
			vm.RecordingStarted -= OnRecordingStarted;
			vm.RecordingStopped -= OnRecordingStopped;
			vm.FocusRequested -= OnFocusRequested;
		}
	}

	private void OnSourceCardPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		if (sender is not Border card)
			return;

		if (card.DataContext is CaptureSource source && DataContext is RecordingViewModel vm)
			vm.SelectedSource = source;

		if (_selectedCard is not null)
			_selectedCard.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Resources["ControlStrokeColorDefaultBrush"]
				?? Application.Current.Resources["ControlStrokeColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush;

		card.BorderBrush = Application.Current.Resources["AccentFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush;
		card.BorderThickness = new Thickness(2);
		if (_selectedCard is not null && _selectedCard != card)
			_selectedCard.BorderThickness = new Thickness(1);
		_selectedCard = card;
	}

	private void OnFocusRequested(object? sender, EventArgs e)
	{
		var window = App.CurrentMainWindow;
		if (window is null) return;

		window.Activate();
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
