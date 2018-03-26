using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Winder.ViewModels;
using Winder.Views;
using System.Threading.Tasks;

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
				directoryPane.PreviewKeyDown += DirectoryListingPane_PreviewKeyDown;
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
				directoryPane.PreviewKeyDown -= DirectoryListingPane_PreviewKeyDown;
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

		private void PopTo(int targetPaneIndex) {
			for (var i = _panes.Count - 1; i > targetPaneIndex; i--)
				PopPane();
			Title = _panes[targetPaneIndex].Name;
		}

		private Tuple<int, IReadOnlyList<FileSystemItemViewModel>> GetDeepestSelection() {
			for (var i = _panes.Count - 1; i >= 0; i--) {
				if (!(_panes[i] is DirectoryListingPane directory))
					continue;
				if (directory.SelectedItems.Count == 0)
					continue;
				return Tuple.Create(i, directory.SelectedItems.Cast<FileSystemItemViewModel>().ToReadOnlyList());
			}

			return Tuple.Create(-1, Enumerable.Empty<FileSystemItemViewModel>().ToReadOnlyList());
		}

		private Tuple<int, IReadOnlyList<FileSystemItemViewModel>> GetLatestSelection(object sender) {
			if (!(sender is DirectoryListingPane directory))
				return Tuple.Create(-1, Enumerable.Empty<FileSystemItemViewModel>().ToReadOnlyList());
			return Tuple.Create(_panes.IndexOf(directory), directory.SelectedItems.Cast<FileSystemItemViewModel>().ToReadOnlyList());
		}

		private void DirectoryListingPane_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var latestSelection = GetLatestSelection(sender);
			if (latestSelection.Item1 == -1) {
				// Return to first pane
				PopTo(0);
				return;
			}

			// Remove panes to the right of the pane containing the deepest selection
			PopTo(latestSelection.Item1);
			
			if (latestSelection.Item2.Count == 1) {
				// Show a new pane for the selected item
				PushPane(latestSelection.Item2.Single());
			}
		}

		private void OpenDeepestSelection() {
			foreach (var file in GetDeepestSelection().Item2)
				Process.Start(file.SourceUntyped.FullName);
		}

		private void DirectoryListingPane_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			OpenDeepestSelection();
		}

		private void DirectoryListingPane_PreviewKeyDown(object sender, KeyEventArgs e) {
			var pane = sender;
		}

		private void DirectoryListingPane_KeyDown(object sender, KeyEventArgs e) {
			var pane = (DirectoryListingPane)sender;
			switch (e.Key) {
				case Key.Enter:
					// Enter is a shortcut for double-clicking the mouse
					OpenDeepestSelection();
					break;
				case Key.Left:
					if (_panes.IndexOf(pane) == 0)
						break;
					// If it's not the root pane, deselect everything (which should close deeper panes)
					// and focus on the selected item in the previous pane
					pane.SelectItem(-1);
					var previousPane = (DirectoryListingPane)_panes[_panes.IndexOf(pane) - 1];
					previousPane.FocusSelectedItem();
					break;
				case Key.Right:
					if (pane.SelectedItem is FileViewModel) {
						if (pane.SelectedIndex < pane.Items.Count - 1)
							pane.SelectItemAndFocus(pane.SelectedIndex + 1);
					} else {
						// Go into the next pane, which is guaranteed to be open
						// bc the selected file system item in this pane is a directory
						var nextPane = (DirectoryListingPane)_panes[_panes.IndexOf(pane) + 1];
						if (nextPane.Items.Count > 0)
							nextPane.SelectItemAndFocus(0);
					}
					break;
			}
		}
	}
}