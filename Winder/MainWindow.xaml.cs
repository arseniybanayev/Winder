using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
			ListBox_Files.ItemsSource = _currentDirectory.GetFileSystemInfos()
				.Select(FileSystemInfoExtended.Create);

			// Called when anything changes about the selection
			ListBox_Files.SelectionChanged += ListBox_Files_SelectionChanged;
		}

		private void ListBox_Files_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			Console.WriteLine("Selected changed");
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			WindowsInterop.HideWindowButtons(this);
		}

		private void OpenCurrentlySelectedFiles() {
			foreach (var file in ListBox_Files.SelectedItems.Cast<FileSystemInfoExtended>())
				Process.Start(file.SourceUntyped.FullName);
		}

		private void ListBox_Files_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			OpenCurrentlySelectedFiles();
		}

		private void ListBox_Files_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key == System.Windows.Input.Key.Enter)
				OpenCurrentlySelectedFiles();
		}
	}
}