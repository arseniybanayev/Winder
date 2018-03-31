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
using Winder.Properties;
using System.Windows.Media;

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

			// Background color and other UI adjustments
			Background = new SolidColorBrush(Color.FromRgb(246, 246, 246));

			// Add a favorites directory
			var favorites = Favorites.Load();
			AddFavoritesPane(favorites);

			// Set up the opening directory
			_currentItem = FileSystemItemViewModel.Create(new DirectoryInfo(Settings.Default.NewWindowPath));
			PushFileSystemPane(_currentItem);
		}
		
		private void SetTitle(string titleSuffix) {
			TextBlockTitle.Text = string.IsNullOrWhiteSpace(titleSuffix)
				? "Winder"
				: $"{titleSuffix}";
		}

		#region Favorites Pane

		private void AddFavoritesPane(Favorites favorites) {
			var pane = new FavoritesPane(favorites);

			// Set column position in the main grid
			GridMain.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(180, GridUnitType.Pixel) // Panes' widths are in pixels, but resizable
			});
			Grid.SetColumn(pane, GridMain.ColumnDefinitions.Count - 1);
			GridMain.Children.Add(pane); // Add to main grid

			// Add a grid splitter to resize the favorites pane
			AddGridSplitter(width: 3);
		}

		#endregion

		#region File/Directory Pane Stack

		private FileSystemItemViewModel _currentItem;
		private List<IFileSystemPane> _panes = new List<IFileSystemPane>();

		private void PushFileSystemPane(FileSystemItemViewModel item) {
			IFileSystemPane pane;
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
			if (_panes.Count > 0)
				AddGridSplitter(width: 5);

			// Set column position in the main grid
			GridMain.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(300, GridUnitType.Pixel) // Panes' widths are in pixels, but resizable
			});
			Grid.SetColumn((UIElement)pane, GridMain.ColumnDefinitions.Count - 1);

			_panes.Add(pane); // Add to stack
			GridMain.Children.Add((UIElement)pane); // Add to main grid
		}

		/// <summary>
		/// Removes the deepest one
		/// </summary>
		private void PopFileSystemPane() {
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
		
		#endregion

		#region GridSplitters

		/// <summary>
		/// Must remove this grid splitter manually, there is no corresponding `RemoveGridSplitter`
		/// </summary>
		private void AddGridSplitter(double width) {
			GridMain.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(0, GridUnitType.Auto) // GridSplitter's width is constant
			});
			var gridSplitter = new GridSplitter {
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Stretch,
				ShowsPreview = true,
				Width = width // constant width
			};
			Grid.SetColumn(gridSplitter, GridMain.ColumnDefinitions.Count - 1);
			GridMain.Children.Add(gridSplitter);
		}

		#endregion

		#region Selection

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

		private Tuple<int, IReadOnlyList<FileSystemItemViewModel>> GetLatestSelectedFiles(DirectoryListingPane directory) {
			return Tuple.Create(_panes.IndexOf(directory), directory.SelectedItems.Cast<FileSystemItemViewModel>().ToReadOnlyList());
		}

		private void DirectoryListingPane_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var latestSelection = GetLatestSelectedFiles((DirectoryListingPane)sender);

			// Remove panes to the right of the pane containing the latest selection
			for (var i = _panes.Count - 1; i > latestSelection.Item1; i--)
				PopFileSystemPane();

			// Update the window title
			if (latestSelection.Item2.Count > 0)
				SetTitle(_panes[latestSelection.Item1].FileSystemItemName);
			else if (latestSelection.Item1 > 0)
				SetTitle(_panes[latestSelection.Item1 - 1].FileSystemItemName);
			else
				SetTitle(_panes[0].FileSystemItemName);

			if (latestSelection.Item2.Count == 1) {
				// Show a new pane for the selected item
				PushFileSystemPane(latestSelection.Item2.Single());
			}
		}

		private void OpenDeepestSelection() {
			foreach (var file in GetDeepestSelection().Item2)
				Process.Start(file.SourceUntyped.FullName);
		}

		private void DirectoryListingPane_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			OpenDeepestSelection();
		}

		#endregion

		#region Keyboard

		private void DirectoryListingPane_PreviewKeyDown(object sender, KeyEventArgs e) {
			var pane = (DirectoryListingPane)sender;
			switch (e.Key) {
				case Key.Left:
					OnLeftKeyDown(pane);
					e.Handled = true;
					break;
				case Key.Right:
					OnRightKeyDown(pane);
					e.Handled = true;
					break;
			}
		}

		private void OnLeftKeyDown(DirectoryListingPane pane) {
			if (_panes.IndexOf(pane) == 0)
				return;
			// If it's not the root pane, deselect everything (which should close deeper panes)
			// and focus on the selected item in the previous pane
			pane.SelectItem(-1);
			var previousPane = (DirectoryListingPane)_panes[_panes.IndexOf(pane) - 1];
			previousPane.FocusSelectedItem();
		}

		private void OnRightKeyDown(DirectoryListingPane pane) {
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
		}
		
		private void DirectoryListingPane_KeyDown(object sender, KeyEventArgs e) {
			var pane = (DirectoryListingPane)sender;
			switch (e.Key) {
				case Key.Enter:
					// Enter is a shortcut for double-clicking the mouse
					OpenDeepestSelection();
					break;
				case Key.Left:
					OnLeftKeyDown(pane);
					break;
				case Key.Right:
					OnRightKeyDown(pane);
					break;
			}
		}

		#endregion

	}
}