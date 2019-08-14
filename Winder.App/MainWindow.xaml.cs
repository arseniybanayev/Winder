using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Winder.App.Properties;
using Winder.App.ViewModels;
using Winder.App.Views;
using Winder.Util;

namespace Winder.App
{
	public partial class MainWindow : Window
	{
		public MainWindow() {
			InitializeComponent(); // Always needs to happen first
			
			// Triggers the font family and size to update to what is defined in the xaml window style
			StyleProperty.OverrideMetadata(typeof(Window), new FrameworkPropertyMetadata {
				DefaultValue = FindResource(typeof(Window))
			});

			// Set up the favorites section
			ListBoxFavorites.ItemsSource = FavoritesViewModel.Default.FavoriteDirectories;

			// Set up the opening directory
			var newWindowPath = Settings.Default.NewWindowPath.ToNormalizedPath();
			if (string.IsNullOrWhiteSpace(newWindowPath) || !Directory.Exists(newWindowPath)) {
				newWindowPath = FileUtil.GetUserProfilePath();
				Settings.Default.NewWindowPath = newWindowPath;
				Settings.Default.Save();
			}

			// Show the opening directory
			PushPane(DirectoryViewModel.Get(Settings.Default.NewWindowPath.ToNormalizedPath()));
			UpdateTitleAndStatus();
		}

		private void SetTitle(DirectoryViewModel directory) {
			var newTitle = string.IsNullOrWhiteSpace(directory.DisplayName)
				? "Winder"
				: directory.DisplayName;
			TextBlockTitle.Text = newTitle;
			ImageTitle.Source = directory.Icon;
		}

		private void SetStatus(string status) {
			var newStatus = status ?? "";
			TextBlockStatus.Text = newStatus;
		}

		private void UpdateTitleAndStatus() {
			var directoryListing = _panes.OfType<DirectoryListingPane>().Last(); // At least the root will always be there

			SetTitle(directoryListing.ViewModel);

			var selectedCount = directoryListing.SelectedItems.Count;
			var availableFreeSpace = directoryListing.ViewModel.Info.GetDriveInfo().AvailableFreeSpace;
			var availableFreeSpaceString = availableFreeSpace.ToByteSuffixString();
			if (selectedCount > 1) {
				// More than one item selected in the deepest directory listing
				SetStatus($"{selectedCount} of {directoryListing.ViewModel.Children.Count} selected, {availableFreeSpaceString} available");
			} else {
				// One item or no items selected
				SetStatus($"{directoryListing.ViewModel.Children.Count} items, {availableFreeSpaceString} available");
			}
		}

		#region Favorites Pane
		
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
			UpdateTitleAndStatus();
		}
		
		private void ListBoxFavorites_Drop(object sender, DragEventArgs e) {
			// Handling a drop of a file system item from a directory listing pane
			if (e.Data.GetDataPresent("Path") && e.Data.GetDataPresent("IsDirectory")) {
				DropFileSystemItemOntoFavorites(e.Data, e.GetPosition(ListBoxFavorites));
			}
		}

		private void DropFileSystemItemOntoFavorites(IDataObject data, Point point) {
			// We only want to handle directory drops (files can't be Favorites)
			if (!(data.GetData("IsDirectory") is bool isDirectory) || !isDirectory)
				return;

			var path = (string)data.GetData("Path");
			var directoryViewModel = DirectoryViewModel.Get(path.ToNormalizedPath());

			// Add to the end of the list if we can't determine where the directory was dropped
			if (!(ListBoxFavorites.InputHitTest(point) is UIElement element)) {
				FavoritesViewModel.Default.Add(directoryViewModel);
				return;
			}

			// Get the ListBoxItem onto which the dragged directory was dropped
			var prop = DependencyProperty.UnsetValue;
			while (prop == DependencyProperty.UnsetValue) {
				prop = ListBoxFavorites.ItemContainerGenerator.ItemFromContainer(element);
				if (prop == DependencyProperty.UnsetValue)
					element = VisualTreeHelper.GetParent(element) as UIElement;

				// If we can't find the ListBoxItem along the visual tree, break and go to the 'else' block below
				if (element == null || ReferenceEquals(element, ListBoxFavorites))
					break;
			}

			// Found the ListBoxItem successfully, so add the dropped directory right above it
			if (prop != DependencyProperty.UnsetValue && element is ListBoxItem listBoxItem) {
				FavoritesViewModel.Default.Add(directoryViewModel, ListBoxFavorites.ItemContainerGenerator.IndexFromContainer(listBoxItem));
			} else {
				// Add to the end of the list if we can't find a ListBoxItem along the visual tree
				FavoritesViewModel.Default.Add(directoryViewModel);
			}
		}

		#endregion

		#region File/Directory Panes

		private readonly List<IFileSystemPane> _panes = new List<IFileSystemPane>();

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

					// Add a breadcrumb
					if (_panes.Count > 0) {
						StackPanelBreadcrumbs.Children.Add(new TextBlock {
							Text = ">",
							Height = 18,
							VerticalAlignment = VerticalAlignment.Center,
							Margin = new Thickness(4, 0, 4, 0)
						});
					}
					var breadcrumbIcon = new Image {
						Source = directory.Icon,
						Width = 18, Height = 18, // Smaller than the icons in the title bar and directory listings
						VerticalAlignment = VerticalAlignment.Center
					};
					StackPanelBreadcrumbs.Children.Add(breadcrumbIcon);
					var breadcrumbTextBlock = new TextBlock {
						Text = directory.DisplayName, // TODO: Use same ellipsis as in directory listing item
						Height = 18,
						Margin = new Thickness(4, 0, 0, 0),
						VerticalAlignment = VerticalAlignment.Center
					};
					StackPanelBreadcrumbs.Children.Add(breadcrumbTextBlock);
					
					break;
				case FileViewModel file:
					// Open file preview pane
					pane = new FileInfoPane(file);
					break;
				default:
					throw new NotSupportedException($"{item.GetType()} doesn't have a corresponding Pane");
			}

			// Add a grid splitter for resizing if this isn't the first pane
			if (_panes.Count > 0)
				AddGridSplitter(width: 5);

			// Add the pane
			GridMain.ColumnDefinitions.Add(new ColumnDefinition {
				Width = new GridLength(260, GridUnitType.Pixel) // Panes' widths are in pixels, but resizable
			});
			Grid.SetColumn((UIElement)pane, GridMain.ColumnDefinitions.Count - 1); // Set column position in the main grid
			_panes.Add(pane); // Add to stack
			GridMain.Children.Add((UIElement)pane); // Add to main grid

			// Scroll to the end horizontally
			ScrollViewerMain.ScrollToRightEnd();
		}

		private void PopPane() {
			var pane = _panes[_panes.Count - 1];

			// Pane-specific disposal
			switch (pane) {
				case DirectoryListingPane directoryPane:
					// Unsubscribe from events
					directoryPane.MouseDoubleClick -= DirectoryListingPane_MouseDoubleClick;
					directoryPane.PreviewKeyDown -= DirectoryListingPane_PreviewKeyDown;
					directoryPane.KeyDown -= DirectoryListingPane_KeyDown;
					directoryPane.SelectionChanged -= DirectoryListingPane_SelectionChanged;

					// Remove a breadcrumb
					if (_panes.Count > 1)
						StackPanelBreadcrumbs.Children.RemoveAt(StackPanelBreadcrumbs.Children.Count - 1);
					StackPanelBreadcrumbs.Children.RemoveAt(StackPanelBreadcrumbs.Children.Count - 1);
					StackPanelBreadcrumbs.Children.RemoveAt(StackPanelBreadcrumbs.Children.Count - 1);

					break;
				case FileInfoPane _:
					break;
				default:
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

		private void PopPanesUntil(int indexOfPaneToKeep) {
			for (var i = _panes.Count - 1; i > indexOfPaneToKeep; i--)
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
			return Tuple.Create(_panes.IndexOf(directory), directory.SelectedItems.Cast<FileSystemItemViewModel>().ToReadOnlyList());
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
			for (var i = _panes.Count - 1; i >= 0; i--) {
				if (!(_panes[i] is DirectoryListingPane directory))
					continue;
				if (directory.SelectedItems.Count == 0)
					continue;
				return Tuple.Create(i, directory.SelectedItems.Cast<FileSystemItemViewModel>().ToReadOnlyList());
			}

			return Tuple.Create(-1, Enumerable.Empty<FileSystemItemViewModel>().ToReadOnlyList());
		}

		#endregion

		#region Directory Listing Context Menu

		private void DirectoryListingPane_Item_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs args) {
			var listBoxItem = (ListBoxItem)sender;
			
			// Select only the item that was right-clicked
			listBoxItem.GetParentOfType<DirectoryListingPane>().SelectedItem = null;
			listBoxItem.IsSelected = true;

			// Create and place a context menu for the selected item
			var selectedItem = (FileSystemItemViewModel)listBoxItem.Content;
			var contextMenu = BuildNewContextMenu(selectedItem);
			contextMenu.PlacementTarget = listBoxItem;
			contextMenu.IsOpen = true;
		}

		private static ContextMenu BuildNewContextMenu(FileSystemItemViewModel item) {
			var contextMenu = new ContextMenu();
			contextMenu.Items.Add(new MenuItem {
				Header = "Open",
				CommandParameter = item,
				Command = new DelegateCommand<FileSystemItemViewModel>(i => i.Open())
			});
			var openWith = new MenuItem {
				Header = "Open With"
			};
			openWith.Items.Add(new MenuItem {
				Header = "App 1..."
			});
			contextMenu.Items.Add(openWith);

			// Move to trash
			contextMenu.Items.Add(new MenuItem {
				Header = "Move to Trash",
				CommandParameter = item,
				Command = new DelegateCommand<FileSystemItemViewModel>(i => i.MoveToTrash())
			});

			// Others
			contextMenu.Items.Add(new Separator());
			contextMenu.Items.Add(new MenuItem {
				Header = "Get Info"
			});
			contextMenu.Items.Add(new MenuItem {
				Header = "Rename"
			});
			contextMenu.Items.Add(new MenuItem {
				Header = $"Compress \"{item.DisplayName}\""
			});
			contextMenu.Items.Add(new MenuItem {
				Header = "Duplicate"
			});
			contextMenu.Items.Add(new MenuItem {
				Header = "Make Alias"
			});

			return contextMenu;
		}

		#endregion

		#region Directory Listing Drag/Drop

		private Point _startPoint;
		private bool _isDragging;

		private void DirectoryListingPane_Item_PreviewMouseLeftButtonDown(object sender, MouseEventArgs e) {
			_startPoint = e.GetPosition(null);
		}

		private void DirectoryListingPane_Item_PreviewMouseMove(object sender, MouseEventArgs e) {
			if (e.LeftButton != MouseButtonState.Pressed || _isDragging)
				return;

			var position = e.GetPosition(null);
			if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
			    Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance) {
				BeginDrag((ListBoxItem)sender);
			}
		}

		private void BeginDrag(ListBoxItem listBoxItem) {
			_isDragging = true;

			var selectedItem = (FileSystemItemViewModel)listBoxItem.Content;
			var data = new DataObject();
			data.SetData("Path", selectedItem.Path.ToString());
			data.SetData("IsDirectory", selectedItem.IsDirectory);

			DragDrop.DoDragDrop(listBoxItem, data, DragDropEffects.Copy);

			_isDragging = false;
		}

		#endregion

		#region Open and Preview

		private void DirectoryListingPane_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			OpenSelected();
		}

		private void OpenSelected() {
			var selectedFiles = GetDeepestSelection().Item2;
			foreach (var file in selectedFiles)
				file.Open();
		}

		private FilePreviewWindow _previewWindow;

		private void TogglePreview() {
			if (_previewWindow != null) {
				_previewWindow.Close();
				return;
			}

			var allSelectedItems = GetDeepestSelection().Item2;
			var selectedFiles = allSelectedItems.OfType<FileViewModel>().ToList();
			Log.Info($"Opening preview for {selectedFiles.Count} files (out of {allSelectedItems.Count} total selected items):");
			foreach (var file in selectedFiles.Take(1)) {
				Log.Info($"  {file.Path}");
				_previewWindow = new FilePreviewWindow(file.Info);
				_previewWindow.ContentRendered += PreviewWindow_ContentRendered;
				_previewWindow.Show();
			}
		}

		private void PreviewWindow_ContentRendered(object sender, EventArgs e) {
			_previewWindow.KeyDown += PreviewWindow_KeyDown;
		}

		private void PreviewWindow_KeyDown(object sender, KeyEventArgs e) {
			if (_previewWindow == null || !_previewWindow.IsLoaded)
				return;
			switch (e.Key) {
				case Key.Space:
					ClosePreview();
					break;
			}
		}

		private void ClosePreview() {
			if (_previewWindow == null)
				return;
			_previewWindow.ContentRendered -= PreviewWindow_ContentRendered;
			_previewWindow.KeyDown -= PreviewWindow_KeyDown;
			_previewWindow.Close();
			_previewWindow = null;
		}

		#endregion

		#region Keyboard

		private void DirectoryListingPane_PreviewKeyDown(object sender, KeyEventArgs e) {
			// KeyDown is enough for most purposes, but for some reason the event gets captured
			// in some cases, so we handle it first using PreviewKeyDown. See:
			// https://stackoverflow.com/questions/49480579/arrow-keys-causing-undesirable-horizontal-scrolling-in-wpf-listbox

			var pane = (DirectoryListingPane)sender;
			switch (e.Key) {
				case Key.Space:
					// Space opens the preview pane
					TogglePreview();
					break;
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
		
		private void DirectoryListingPane_KeyDown(object sender, KeyEventArgs e) {
			var pane = (DirectoryListingPane)sender;
			switch (e.Key) {
				case Key.Enter:
					// Enter is a shortcut for double-clicking the mouse
					OpenSelected();
					break;
				case Key.Left:
					OnLeftKeyDown(pane);
					break;
				case Key.Right:
					OnRightKeyDown(pane);
					break;
			}
		}

		private void OnLeftKeyDown(DirectoryListingPane pane) {
			if (_panes.IndexOf(pane) == 0) {
				// In the root directory pane, try to move up (which should close deeper panes if open)
				// and focus on the newly selected item
				// (Note: this is commented out because Finder doesn't do this)
				// if (pane.SelectedIndex > 0)
				//	pane.SelectItemAndFocus(pane.SelectedIndex - 1);
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

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			ClosePreview();
		}
	}
}