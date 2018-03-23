using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Winder.ViewModels;
using Winder.Views;

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

		public MainWindow() {
			InitializeComponent(); // Always needs to happen first

			// Triggers the font family and size to update to what is defined in the xaml window style
			StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata {
				DefaultValue = FindResource(typeof(Window))
			});

			// Set up the opening directory
			_currentItem = FileSystemItemViewModel.Create(new DirectoryInfo(@"C:\Users\arsen\Sheet Music"));
			PushPane(_currentItem);
		}

		private FileSystemItemViewModel _currentItem;
		private List<IWinderPane> _panes = new List<IWinderPane>();

		private void PushPane(FileSystemItemViewModel item) {
			IWinderPane pane;
			if (item is DirectoryViewModel directory) {
				// Open directory listing pane
				var directoryPane = new DirectoryListingPane(directory);

				// Subscribe to events
				directoryPane.MouseDoubleClick += DirectoryListingPane_MouseDoubleClick;
				directoryPane.KeyDown += DirectoryListingPane_KeyDown;
				directoryPane.SelectionChanged += DirectoryListingPane_SelectionChanged;

				pane = directoryPane;
			} else if (item is FileViewModel file) {
				// Open file preview pane
				pane = new FilePreviewPane(file);
			} else {
				throw new NotSupportedException($"{item.GetType()} doesn't have a corresponding Pane");
			}

			// Add a grid splitter for resizing if this isn't the first pane
			if (_panes.Count > 0) {
				GridMain.ColumnDefinitions.Add(new ColumnDefinition {
					Width = new GridLength(0, GridUnitType.Auto) // GridSplitter's width is constant
				});
				var gridSplitter = new GridSplitter {
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Stretch,
					ShowsPreview = true,
					Width = 5 // constant width
				};
				Grid.SetColumn(gridSplitter, _panes.Count * 2 - 1);
				GridMain.Children.Add(gridSplitter);
			}

			// Set column position in the main grid
			GridMain.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(300, GridUnitType.Pixel) // Panes' widths are in pixels, but resizable
			});
			Grid.SetColumn((UIElement)pane, _panes.Count * 2);

			_panes.Add(pane); // Add to stack
			GridMain.Children.Add((UIElement)pane); // Add to main grid
		}

		private void PopPane() {
			var pane = _panes[_panes.Count - 1];

			// Pane-specific disposal
			if (pane is DirectoryListingPane directoryPane) {
				// Unsubscribe from events
				directoryPane.MouseDoubleClick -= DirectoryListingPane_MouseDoubleClick;
				directoryPane.KeyDown -= DirectoryListingPane_KeyDown;
				directoryPane.SelectionChanged -= DirectoryListingPane_SelectionChanged;
			} else if (pane is FilePreviewPane filePane) {

			} else {
				throw new NotSupportedException($"{pane.GetType()} was an unexpected Pane");
			}

			// Remove the pane
			_panes.RemoveAt(_panes.Count - 1);
			GridMain.ColumnDefinitions.RemoveAt(GridMain.ColumnDefinitions.Count - 1);
			GridMain.Children.RemoveAt(GridMain.Children.Count - 1);

			if (_panes.Count > 0) { // If there are still panes remaining, then there's also a GridSplitter to remove
				GridMain.ColumnDefinitions.RemoveAt(GridMain.ColumnDefinitions.Count - 1);
				GridMain.Children.RemoveAt(GridMain.Children.Count - 1);
			}
		}

		private IReadOnlyDictionary<int, IReadOnlyList<FileSystemItemViewModel>> GetGroupedCurrentlySelectedItems() {
			return _panes.ToDictionary(
				(p, i) => i,
				(p, i) => ((p as DirectoryListingPane)
						?.SelectedItems.Cast<FileSystemItemViewModel>()
						?? Enumerable.Empty<FileSystemItemViewModel>())
					.ToReadOnlyList());
		}

		private IReadOnlyList<FileSystemItemViewModel> GetCurrentlySelectedItems() {
			return GetGroupedCurrentlySelectedItems().SelectMany(kv => kv.Value).ToList();
		}

		private void DirectoryListingPane_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var currentlySelected = GetGroupedCurrentlySelectedItems();
			var totalSelectedItems = currentlySelected.Values.Sum(v => v.Count);
			if (totalSelectedItems == 1) {
				var indexOfPaneContainingSelection = currentlySelected.Single(p => p.Value.Count == 1).Key;
				//var indexOfNextPane = indexOfPaneContainingSelection + 1; // TODO this optimization

				// Clear all the list boxes after indexOfPaneContainingSelection // TODO indexOfNextPane
				for (var i = _panes.Count - 1; i > indexOfPaneContainingSelection; i--)
					PopPane();

				// Open a new list box or update the existing list box at indexOfNextPane
				PushPane(currentlySelected.Single(p => p.Value.Count == 1).Value.Single());
			} else {
				// Close any open pane to the right
				for (var i = _panes.Count - 1; i > 0; i--)
					PopPane();
			}
		}

		private void OpenCurrentlySelectedFiles() {
			foreach (var file in GetCurrentlySelectedItems())
				Process.Start(file.SourceUntyped.FullName);
		}

		private void DirectoryListingPane_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
			OpenCurrentlySelectedFiles();
		}

		private void DirectoryListingPane_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
			if (e.Key == System.Windows.Input.Key.Enter)
				OpenCurrentlySelectedFiles();
		}
	}
}