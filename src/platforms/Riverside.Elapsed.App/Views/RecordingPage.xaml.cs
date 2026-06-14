using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls.Primitives;
using Riverside.Elapsed.App.Models.Recording;
using Riverside.Elapsed.App.ViewModels;

namespace Riverside.Elapsed.App.Views;

public sealed partial class RecordingPage : Page
{
	private const int MinCompactWidth = 380;
	private const int MinHeight = 520;

	private Border? _selectedCard;
	private DispatcherQueueTimer? _resizeDebounce;
	private double _pendingWidth;

	public RecordingPage()
	{
		this.InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		SetWindowMinSize(MinCompactWidth, MinHeight);

		if (DataContext is RecordingViewModel vm)
		{
			vm.RecordingStarted += OnRecordingStarted;
			vm.RecordingStopped += OnRecordingStopped;
			vm.FocusRequested += OnFocusRequested;
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
		{
			vm.SelectedSource = source;
		}

		if (_selectedCard is not null)
		{
			_selectedCard.BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Resources["ControlStrokeColorDefaultBrush"]
				?? Application.Current.Resources["ControlStrokeColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush;
		}

		card.BorderBrush = Application.Current.Resources["AccentFillColorDefaultBrush"] as Microsoft.UI.Xaml.Media.Brush;
		card.BorderThickness = new Thickness(2);
		if (_selectedCard is not null && _selectedCard != card)
		{
			_selectedCard.BorderThickness = new Thickness(1);
		}

		_selectedCard = card;
	}

	private void OnFocusRequested(object? sender, EventArgs e)
	{
		var window = App.MainWindow;
		if (window is null) return;

		window.Activate();
	}

	private void OnRecordingStarted(object? sender, EventArgs e)
	{
		PreviewColumn.Width = new GridLength(1, GridUnitType.Star);
	}

	private void OnRecordingStopped(object? sender, EventArgs e)
	{
		PreviewColumn.Width = new GridLength(0);
	}

	private void OnSourceScrollViewerSizeChanged(object sender, SizeChangedEventArgs e)
	{
		_pendingWidth = e.NewSize.Width;

		if (_resizeDebounce is null)
		{
			var dispatcher = DispatcherQueue.GetForCurrentThread();
			if (dispatcher is null) return;
			_resizeDebounce = dispatcher.CreateTimer();
			_resizeDebounce.Interval = TimeSpan.FromMilliseconds(200);
			_resizeDebounce.IsRepeating = false;
			_resizeDebounce.Tick += (_, _) => ApplyTileSize(_pendingWidth);
		}

		_resizeDebounce.Stop();
		_resizeDebounce.Start();
	}

	private void ApplyTileSize(double available)
	{
		const double spacing = 6;
		const double minTileWidth = 158;
		const double maxTileWidth = 280;
		const double aspectRatio = 148.0 / 158.0;

		int columns = Math.Max(1, (int)((available + spacing) / (minTileWidth + spacing)));
		double tileWidth = (available - spacing * (columns - 1)) / columns;
		tileWidth = Math.Clamp(tileWidth, minTileWidth, maxTileWidth);

		SourceGridLayout.MinItemWidth = tileWidth;
		SourceGridLayout.MinItemHeight = tileWidth * aspectRatio;
	}

	private static void SetWindowMinSize(int width, int height)
	{
		var window = App.MainWindow;
		if (window is null) return;

		var scale = window.Content?.XamlRoot?.RasterizationScale ?? 1.0;
		var scaledWidth = (int)(width * scale);
		var scaledHeight = (int)(height * scale);

		window.AppWindow.Resize(new Windows.Graphics.SizeInt32 { Width = scaledWidth, Height = scaledHeight });
	}

	private void OnSourceKindSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (DataContext is not RecordingViewModel vm) return;
		vm.SelectedSourceKind = SourceKindSegmented.SelectedIndex switch
		{
			1 => CaptureSourceKind.Window,
			2 => CaptureSourceKind.Camera,
			_ => CaptureSourceKind.Screen,
		};
	}

	private void ProfilePersonPicture_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
	}
}
