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
using Winder.Util;

namespace Winder
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow() {
			Log.Add(new ConsoleLogger());
			Log.Add(new FileLogger());

			InitializeComponent(); // Always needs to happen first

			// Triggers the font family and size to update to what is defined in the xaml window style
			StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata {
				DefaultValue = FindResource(typeof(Window))
			});

			// Background color and other UI adjustments
			CloseButton.FocusVisualStyle = null;
			MinimizeButton.FocusVisualStyle = null;
			MaximizeButton.FocusVisualStyle = null;

			// Add a favorites directory
			var favorites = Favorites.Load();
			AddFavoritesPane(favorites);

			// Set up the opening directory
			var newWindowPath = Settings.Default.NewWindowPath;
			if (string.IsNullOrWhiteSpace(newWindowPath) || !Directory.Exists(newWindowPath)) {
				newWindowPath = Environment.GetEnvironmentVariable("USERPROFILE");
				Settings.Default.NewWindowPath = newWindowPath;
				Settings.Default.Save();
			}

			PushFileSystemPane(FileSystemItemViewModel.Create(new DirectoryInfo(Settings.Default.NewWindowPath)));
		}

		private void SetTitle(string title) {
			var newTitle = string.IsNullOrWhiteSpace(title)
				? "Winder"
				: title;
			Log.Info($"Setting title to {newTitle}");
			TextBlockTitle.Text = newTitle;
		}

		private void SetStatus(string status) {
			var newStatus = status ?? "";
			Log.Info($"Setting status to {newStatus}");
			TextBlockStatus.Text = newStatus;
		}

		private void UpdateTitleAndStatus() {
			var directoryListing = _panes.OfType<DirectoryListingPane>().Last(); // At least the root will always be there

			SetTitle(directoryListing.ViewModel.Name);

			var selectedCount = directoryListing.SelectedItems.Count;
			if (selectedCount > 1) {
				// More than one item selected in the deepest directory listing
				SetStatus($"{selectedCount} of {directoryListing.ViewModel.Children.Count} selected, ? GB available");
			} else {
				// One item or no items selected
				SetStatus($"{directoryListing.ViewModel.Children.Count} items, ? GB available");
			}
		}

		#region Favorites Pane

		private void AddFavoritesPane(Favorites favorites) {
			Log.Info("Adding favorites pane");
			var pane = new FavoritesPane(favorites);
			pane.SelectionChanged += FavoritesPane_SelectionChanged;
			pane.PreviewKeyDown += FavoritesPane_PreviewKeyDown;

			// Set column position in the main grid
			GridMain.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(180, GridUnitType.Pixel) // Panes' widths are in pixels, but resizable
			});
			Grid.SetColumn(pane, GridMain.ColumnDefinitions.Count - 1);
			GridMain.Children.Add(pane); // Add to main grid

			// Add a grid splitter to resize the favorites pane
			AddGridSplitter(width: 3);
		}

		private void FavoritesPane_PreviewKeyDown(object sender, KeyEventArgs e) {
			switch (e.Key) {
				case Key.Left:
				case Key.Right:
				case Key.Down:
				case Key.Up:
					Log.Info($"Handling PreviewKeyDown for Key={e.Key} in favorites pane");
					e.Handled = true; // Disable keyboard navigation in favorites pane
					break;
			}
		}

		private void FavoritesPane_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (!(sender is FavoritesPane favorites)) // Shouldn't happen
				return;

			var selectedDirectory = favorites.SelectedDirectory;
			if (selectedDirectory == null)
				return;

			// Pop all panes and start with a new opening directory
			Log.Info($"Switching to new root directory from favorites: {selectedDirectory.FullName}");
			favorites.SelectedIndex = -1; // Make it seem like you can't select in Favorites
			PopFileSystemPanesUntil(-1);
			PushFileSystemPane(selectedDirectory);
		}

		#endregion

		#region File/Directory Pane Stack
		
		private readonly List<IFileSystemPane> _panes = new List<IFileSystemPane>();

		private void PushFileSystemPane(FileSystemItemViewModel item) {
			IFileSystemPane pane;
			switch (item) {
				case DirectoryViewModel directory:
					// Open directory listing pane
					var directoryPane = new DirectoryListingPane(directory);

					// Subscribe to events
					directoryPane.MouseDoubleClick += DirectoryListingPane_MouseDoubleClick;
					directoryPane.PreviewKeyDown += DirectoryListingPane_PreviewKeyDown;
					directoryPane.KeyDown += DirectoryListingPane_KeyDown;
					directoryPane.SelectionChanged += DirectoryListingPane_SelectionChanged;

					pane = directoryPane;
					break;
				case FileViewModel file:
					// Open file preview pane
					pane = new FilePreviewPane(file);
					break;
				default:
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

			Log.Info($"Pushing pane at grid column {GridMain.ColumnDefinitions.Count - 1} for {item.FullName}");

			_panes.Add(pane); // Add to stack
			GridMain.Children.Add((UIElement)pane); // Add to main grid
		}

		/// <summary>
		/// Removes the deepest one
		/// </summary>
		private void PopFileSystemPane() {
			var pane = _panes[_panes.Count - 1];

			// Pane-specific disposal
			switch (pane) {
				case DirectoryListingPane directoryPane:
					// Unsubscribe from events
					directoryPane.MouseDoubleClick -= DirectoryListingPane_MouseDoubleClick;
					directoryPane.PreviewKeyDown -= DirectoryListingPane_PreviewKeyDown;
					directoryPane.KeyDown -= DirectoryListingPane_KeyDown;
					directoryPane.SelectionChanged -= DirectoryListingPane_SelectionChanged;
					break;
				case FilePreviewPane filePane:
					break;
				default:
					throw new NotSupportedException($"{pane.GetType()} was an unexpected Pane");
			}

			// Remove the pane
			Log.Info($"Removing pane #{_panes.Count - 1} from grid column {GridMain.ColumnDefinitions.Count - 1}");
			_panes.RemoveAt(_panes.Count - 1);
			GridMain.ColumnDefinitions.RemoveAt(GridMain.ColumnDefinitions.Count - 1);
			GridMain.Children.RemoveAt(GridMain.Children.Count - 1);

			if (_panes.Count > 0) { // If there are still panes remaining, then there's also a GridSplitter to remove
				Log.Info($"Removing grid splitter from grid column {GridMain.ColumnDefinitions.Count - 1}");
				GridMain.ColumnDefinitions.RemoveAt(GridMain.ColumnDefinitions.Count - 1);
				GridMain.Children.RemoveAt(GridMain.Children.Count - 1);
			}
		}

		private void PopFileSystemPanesUntil(int indexOfPaneToKeep) {
			for (var i = _panes.Count - 1; i > indexOfPaneToKeep; i--)
				PopFileSystemPane();
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
			Log.Info($"Adding grid splitter at grid column {GridMain.ColumnDefinitions.Count - 1}");
			GridMain.Children.Add(gridSplitter);
		}

		#endregion

		#region Directory Listing Selection

		private Tuple<int, IReadOnlyList<FileSystemItemViewModel>> GetLatestSelectedFiles(DirectoryListingPane directory) {
			return Tuple.Create(_panes.IndexOf(directory), directory.SelectedItems.Cast<FileSystemItemViewModel>().ToReadOnlyList());
		}

		private void DirectoryListingPane_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var latestSelection = GetLatestSelectedFiles((DirectoryListingPane)sender);
			Log.Info($"Selection at pane #{latestSelection.Item1} changed to {latestSelection.Item2.ToStringDelimited(f => f.Name)}");

			// Remove panes to the right of the pane containing the latest selection
			PopFileSystemPanesUntil(latestSelection.Item1);

			// Show a new pane for the selected item, if there is one
			if (latestSelection.Item2.Count == 1)
				PushFileSystemPane(latestSelection.Item2.Single());

			UpdateTitleAndStatus();
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
			if (_panes.IndexOf(pane) == 0) {
				// In the root directory pane, try to move up (which should close deeper panes if open)
				// and focus on the newly selected item
				if (pane.SelectedIndex > 0)
					pane.SelectItemAndFocus(pane.SelectedIndex - 1);
			} else {
				// If it's not the root pane, deselect everything in this pane (which should close deeper panes)
				// and focus on the selected item in the previous pane
				pane.SelectItem(-1);
				var previousPane = (DirectoryListingPane)_panes[_panes.IndexOf(pane) - 1];
				previousPane.FocusSelectedItem();
			}
		}

		private void OnRightKeyDown(DirectoryListingPane pane) {
			if (pane.SelectedItem is FileViewModel) {
				// If a file is selected in this pane, try to move down (which should change the deeper panes)
				// and focus on the newly selected item
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

		#region Title Bar Interactions

		private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e) {
			if (e.ChangedButton == MouseButton.Left) {
				if (e.ClickCount == 2) {
					ToggleMaximized();
				} else {
					Application.Current.MainWindow?.DragMove();
				}
			}
		}

		private void ToggleMaximized() {
			WindowState = WindowState == WindowState.Maximized
				? WindowState.Normal
				: WindowState.Maximized;
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
		}

		private void MaximizeButton_Click(object sender, RoutedEventArgs e) {
			ToggleMaximized();
		}

		private void MinimizeButton_Click(object sender, RoutedEventArgs e) {
			WindowState = WindowState.Minimized;
		}

		#endregion

		#region Status Bar Interactions

		private void StatusBar_MouseDown(object sender, MouseButtonEventArgs e) {
			Application.Current.MainWindow?.DragMove();
		}

		#endregion

	}
}