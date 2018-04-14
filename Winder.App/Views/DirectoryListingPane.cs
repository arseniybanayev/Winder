using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using Winder.App.ViewModels;

namespace Winder.App.Views
{
	public class DirectoryListingPane : ListBox, IFileSystemPane
	{
		public readonly DirectoryViewModel ViewModel;

		public DirectoryListingPane(DirectoryViewModel directory) {
			// Basic display and interactivity settings
			SelectionMode = SelectionMode.Extended;
			HorizontalAlignment = HorizontalAlignment.Stretch; // within the column it occupies in GridMain
			HorizontalContentAlignment = HorizontalAlignment.Stretch; // the items inside the ListBox
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			// Items come from the supplied view model
			ViewModel = directory;
			ItemsSource = ViewModel.Children;

			// Template for items
			var itemTemplate = XamlReader.Load(
				new FileStream("Resources/DirectoryListingItemTemplate.xaml", FileMode.Open)
				) as DataTemplate;
			ItemTemplate = itemTemplate;
		}

		internal void SelectItem(int index) {
			SelectedIndex = index;
		}

		internal void SelectItemAndFocus(int index) {
			SelectedIndex = index;
			FocusSelectedItem();
		}

		/// <summary>
		/// Focuses the first selected item if multiple are selected.
		/// Does nothing if no item is selected
		/// </summary>
		internal void FocusSelectedItem() {
			if (SelectedItem == null)
				return;
			((ListBoxItem)ItemContainerGenerator.ContainerFromItem(SelectedItem)).Focus();
		}

		///// <summary>
		///// Opens the context menu on the first selected item if multiple are selected.
		///// Does nothing if no item is selected.
		///// </summary>
		//internal void ToggleContextMenuOnSelectedItem() {
		//	if (SelectedItem == null)
		//		return;
		//	var contextMenu = FindResource("DirectoryListingItemContextMenu") as ContextMenu;
		//	contextMenu.PlacementTarget = ((ListBoxItem)ItemContainerGenerator.ContainerFromItem(SelectedItem));
		//	contextMenu.IsOpen = true;
		//}

		/// <summary>
		/// Returns whether the first selected item is focused if multiple are selected.
		/// Returns false if no item is selected.
		/// </summary>
		internal bool SelectedItemIsFocused {
			get {
				if (SelectedItem == null)
					return false;
				return ((ListBoxItem)ItemContainerGenerator.ContainerFromItem(SelectedItem)).IsFocused;
			}
		}
	}
}