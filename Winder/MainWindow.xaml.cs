using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Winder
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			WindowsInterop.HideWindowButtons(this);
		}

		public MainWindow()
		{
			InitializeComponent(); // Always needs to happen first

			// Triggers the font family and size to update to what is defined in the xaml window style
			StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata {
				DefaultValue = FindResource(typeof(Window))
			});
			
			// Set up the opening directory
			_currentDirectory = new DirectoryViewModel(@"C:\Users\arsen\Sheet Music");
			PushListBox(_currentDirectory);
		}

		private DirectoryViewModel _currentDirectory;
		private Stack<ListBox> _listBoxes = new Stack<ListBox>();

		private void PushListBox(DirectoryViewModel directory) {
			// Basic display and interactivity settings
			var listBox = new ListBox();
			listBox.SelectionMode = SelectionMode.Extended;
			listBox.HorizontalContentAlignment = HorizontalAlignment.Stretch;
			listBox.SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			// Items come from the supplied view model
			listBox.ItemsSource = _currentDirectory.Children;

			// Template for items
			var itemTemplate = XamlReader.Load(
				new FileStream("Resources/DirectoryListBoxItemTemplate.xaml", FileMode.Open)
				) as DataTemplate;
			listBox.ItemTemplate = itemTemplate;

			// Subscribe to events
			listBox.MouseDoubleClick += ListBox_Files_MouseDoubleClick;
			listBox.KeyDown += ListBox_Files_KeyDown;
			listBox.SelectionChanged += ListBox_Files_SelectionChanged;

			// Set column position in the main grid
			Grid.SetColumn(listBox, _listBoxes.Count);

			_listBoxes.Push(listBox);
			GridMain.Children.Add(listBox);
		}
		
		private IEnumerable<FileSystemInfoExtended> CurrentlySelectedFiles => _listBoxes.Peek().SelectedItems.Cast<FileSystemInfoExtended>();

		private void ListBox_Files_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var currentlySelected = CurrentlySelectedFiles.ToList();
			if (currentlySelected.Count == 1) {
				if (currentlySelected.Single().IsDirectory) {
					// Open directory in a new ListBox to the right
				} else {
					// Open file preview in a pane to the right
				}
			} else {
				// Close any open pane to the right
			}
		}

		private void OpenCurrentlySelectedFiles() {
			foreach (var file in CurrentlySelectedFiles)
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