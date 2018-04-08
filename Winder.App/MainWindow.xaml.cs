using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Winder.App.Properties;
using Winder.App.Views;
using Winder.Util;

namespace Winder.App
{
	public partial class MainWindow : Window
	{
		public MainWindow() {
			InitializeComponent(); // Always needs to happen first

			//var previewHost = new PreviewHost(new FileInfo(@"C:\Users\arsen\Sheet Music\(VA) NJC.pdf"));
			//previewHost.HorizontalAlignment = HorizontalAlignment.Stretch;
			//previewHost.Background = new SolidColorBrush(Color.FromRgb(100, 100, 100));
			//Content = previewHost;

			// Triggers the font family and size to update to what is defined in the xaml window style
			StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata {
				DefaultValue = FindResource(typeof(Window))
			});

			// Set up the favorites pane
			var favorites = FavoritesViewModel.Load(Settings.Default.FavoritePaths?.Cast<string>());
			favorites.CollectionChanged += Favorites_CollectionChanged;
			ListBoxFavorites.ItemsSource = favorites.FavoriteDirectories;

			// Set up the opening directory
			var newWindowPath = Settings.Default.NewWindowPath;
			if (string.IsNullOrWhiteSpace(newWindowPath) || !Directory.Exists(newWindowPath)) {
				newWindowPath = FileUtil.GetUserProfilePath();
				Settings.Default.NewWindowPath = newWindowPath;
				Settings.Default.Save();
			}

			// Open the opening directory
			PushPane(FileSystemItemViewModel.Create(new DirectoryInfo(Settings.Default.NewWindowPath)));
		}

		private void SetTitle(string title) {
			var newTitle = string.IsNullOrWhiteSpace(title)
				? "Winder"
				: title;
			TextBlockTitle.Text = newTitle;
		}

		private void SetStatus(string status) {
			var newStatus = status ?? "";
			TextBlockStatus.Text = newStatus;
		}

		private void UpdateTitleAndStatus() {
			var directoryListing = _filePanes.OfType<DirectoryListingPane>().Last(); // At least the root will always be there

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

		private static void Favorites_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			var stringCollection = new StringCollection();
			stringCollection.AddRange(((FavoritesViewModel)sender).FavoriteDirectories.Select(c => c.FullName).ToArray());
			Settings.Default.FavoritePaths = stringCollection;
			Settings.Default.Save();
		}

		private void ListBoxFavorites_PreviewKeyDown(object sender, KeyEventArgs e) {
			switch (e.Key) {
				case Key.Left:
				case Key.Right:
				case Key.Down:
				case Key.Up:
					e.Handled = true; // Disable keyboard navigation in favorites pane
					break;
			}
		}

		private void ListBoxFavorites_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (!(sender is ListBox favorites)) // Shouldn't happen
				return;

			var selectedDirectory = favorites.SelectedItems.OfType<DirectoryViewModel>().FirstOrDefault();
			if (selectedDirectory == null)
				return;

			// Pop all panes and start with a new opening directory
			favorites.SelectedIndex = -1; // Make it seem like you can't select in Favorites
			PopPanesUntil(-1);
			PushPane(selectedDirectory);
		}

		#endregion

		#region File/Directory Panes

		private readonly List<IFileSystemPane> _filePanes = new List<IFileSystemPane>();

		private void PushPane(FileSystemItemViewModel item) {
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
			if (_filePanes.Count > 0)
				AddGridSplitter(width: 5);

			// Add the pane
			GridMain.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(300, GridUnitType.Pixel) // Panes' widths are in pixels, but resizable
			});
			Grid.SetColumn((UIElement)pane, GridMain.ColumnDefinitions.Count - 1); // Set column position in the main grid
			_filePanes.Add(pane); // Add to stack
			GridMain.Children.Add((UIElement)pane); // Add to main grid

			// Scroll to the end horizontally
			ScrollViewerMain.ScrollToRightEnd();
		}

		private void PopPane() {
			var pane = _filePanes[_filePanes.Count - 1];

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
					filePane.UnloadPreviewHandler();
					break;
				default:
					throw new NotSupportedException($"{pane.GetType()} was an unexpected Pane");
			}

			// Remove the pane
			_filePanes.RemoveAt(_filePanes.Count - 1);
			GridMain.ColumnDefinitions.RemoveAt(GridMain.ColumnDefinitions.Count - 1);
			GridMain.Children.RemoveAt(GridMain.Children.Count - 1);

			if (_filePanes.Count > 0) { // If there are still panes remaining, then there's also a GridSplitter to remove
				GridMain.ColumnDefinitions.RemoveAt(GridMain.ColumnDefinitions.Count - 1);
				GridMain.Children.RemoveAt(GridMain.Children.Count - 1);
			}
		}

		private void PopPanesUntil(int indexOfPaneToKeep) {
			for (var i = _filePanes.Count - 1; i > indexOfPaneToKeep; i--)
				PopPane();
		}

		#endregion

		#region Grid Splitters

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

		#region Directory Listing Selection

		private Tuple<int, IReadOnlyList<FileSystemItemViewModel>> GetLatestSelectedFiles(DirectoryListingPane directory) {
			return Tuple.Create(_filePanes.IndexOf(directory), directory.SelectedItems.Cast<FileSystemItemViewModel>().ToReadOnlyList());
		}

		private void DirectoryListingPane_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			var latestSelection = GetLatestSelectedFiles((DirectoryListingPane)sender);

			// Remove panes to the right of the pane containing the latest selection
			PopPanesUntil(latestSelection.Item1);

			// Show a new pane for the selected item, if there is one
			if (latestSelection.Item2.Count == 1)
				PushPane(latestSelection.Item2.Single());

			UpdateTitleAndStatus();
		}

		private Tuple<int, IReadOnlyList<FileSystemItemViewModel>> GetDeepestSelection() {
			for (var i = _filePanes.Count - 1; i >= 0; i--) {
				if (!(_filePanes[i] is DirectoryListingPane directory))
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
			if (_filePanes.IndexOf(pane) == 0) {
				// In the root directory pane, try to move up (which should close deeper panes if open)
				// and focus on the newly selected item
				if (pane.SelectedIndex > 0)
					pane.SelectItemAndFocus(pane.SelectedIndex - 1);
			} else {
				// If it's not the root pane, deselect everything in this pane (which should close deeper panes)
				// and focus on the selected item in the previous pane
				pane.SelectItem(-1);
				var previousPane = (DirectoryListingPane)_filePanes[_filePanes.IndexOf(pane) - 1];
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
				var nextPane = (DirectoryListingPane)_filePanes[_filePanes.IndexOf(pane) + 1];
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