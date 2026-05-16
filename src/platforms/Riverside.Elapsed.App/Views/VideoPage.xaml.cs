namespace Riverside.Elapsed.App.Views;

public sealed partial class VideoPage : Page
{
	public VideoPage()
	{
		this.InitializeComponent();
		Loaded += OnLoaded;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (DataContext is ViewModels.VideoViewModel vm)
		{
			await vm.InitializeAsync();
		}
	}
}
