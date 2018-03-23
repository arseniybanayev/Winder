using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace Winder
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private DirectoryInfo _currentDirectory;

		public MainWindow()
		{
			InitializeComponent(); // Always needs to happen first

			// Triggers the font family and size to update to what is defined in the xaml window style
			StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata {
				DefaultValue = FindResource(typeof(Window))
			});

			// TODO: Make a model? Controller?
			_currentDirectory = new DirectoryInfo(@"C:\Users\arsen\Sheet Music");
			ListBoxFiles.ItemsSource = _currentDirectory.GetFileSystemInfos()
				.Select(FileSystemInfoExtended.Create);

			// Called when anything changes about the selection
			ListBoxFiles.SelectionChanged += ListBoxFiles_SelectionChanged;
		}

		private void ListBoxFiles_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			Console.WriteLine("Selected changed");
		}

		#region Disable the default Window buttons (close, minimize)
		
		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			var hwnd = new WindowInteropHelper(this).Handle;
			SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
		}

		#endregion

	}
}