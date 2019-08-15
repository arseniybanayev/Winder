using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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

			// Template and style for items
			var itemTemplate = XamlReader.Load(
				new FileStream("Resources/DirectoryListingItemTemplate.xaml", FileMode.Open)
				) as DataTemplate;
			ItemTemplate = itemTemplate;
			ItemContainerStyle = (Style)Application.Current.MainWindow?.Resources["DirectoryListingItemStyle"];
		}

		internal void SelectItem(int index) {
			SelectedIndex = index;
		}

		internal void SelectItemAndFocus(int index) {
			SelectedIndex = index;
			FocusSelectedItem();
		}

		internal void FocusItemAt(int index) {
			((ListBoxItem)ItemContainerGenerator.ContainerFromIndex(index)).Focus();
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
		
		/// <summary>
		/// Returns whether the first selected item is focused if at least one is selected.
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