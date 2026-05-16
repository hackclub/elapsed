using Riverside.Elapsed.App.ViewModels;

namespace Riverside.Elapsed.App.Views;

public sealed partial class RecordingPage : Page
{
	public RecordingPage()
	{
		this.InitializeComponent();
	}

	private async void OnPageLoaded(object sender, RoutedEventArgs e)
	{
		if (DataContext is RecordingViewModel vm && vm.StartCommand.CanExecute(null))
		{
			await vm.StartCommand.ExecuteAsync(null);
		}
	}
}
