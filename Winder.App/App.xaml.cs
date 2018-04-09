using System.Windows;
using System.Windows.Threading;
using Winder.Util;

namespace Winder.App
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e) {
			base.OnStartup(e);

			Log.Add(new ConsoleLogger());
			Log.Add(new FileLogger());

			Current.DispatcherUnhandledException += HandleException;
		}

		private static void HandleException(object sender, DispatcherUnhandledExceptionEventArgs e) {
			Log.Error("Unhandled exception occurred in dispatcher", e.Exception);
			var errorMessage = $"An unhandled exception occurred: {e.Exception.Message}";
			MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			e.Handled = true;
		}
	}
}