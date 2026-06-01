#if false
namespace Riverside.Elapsed.App.Views;

public sealed partial class MainPage : Page
{
	public MainPage()
	{
		this.InitializeComponent();
		this.Loaded += OnLoaded;
	}

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		if (DataContext is ViewModels.MainViewModel vm)
		{
			await vm.InitializeAsync();
		}
	}
}

#endif
