using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using Winder.ViewModels;

namespace Winder.Views
{
	public class DirectoryListingPane : ListBox, IFileSystemPane
	{
		public string Name { get; }

		public DirectoryListingPane(DirectoryViewModel directory) {
			// Basic display and interactivity settings
			SelectionMode = SelectionMode.Extended;
			HorizontalAlignment = HorizontalAlignment.Stretch; // within the column it occupies in GridMain
			HorizontalContentAlignment = HorizontalAlignment.Stretch; // the items inside the ListBox
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			// Items come from the supplied view model
			ItemsSource = directory.Children;
			Name = directory.NameWithoutExtension;

			// Template for items
			var itemTemplate = XamlReader.Load(
				new FileStream("Resources/DirectoryListBoxItemTemplate.xaml", FileMode.Open)
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

		internal void FocusSelectedItem() {
			((ListBoxItem)ItemContainerGenerator.ContainerFromItem(SelectedItem)).Focus();
		}
	}
}