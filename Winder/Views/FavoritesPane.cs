using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Winder.Views
{
	public class FavoritesPane : ListBox
	{
		public FavoritesPane(Favorites favorites) {
			// Basic display and interactivity settings
			SelectionMode = SelectionMode.Single;
			HorizontalAlignment = HorizontalAlignment.Stretch; // within the column it occupies in GridMain
			HorizontalContentAlignment = HorizontalAlignment.Stretch; // the items inside the ListBox
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			ItemsSource = favorites.Children;

			// Template for items
			var itemTemplate = XamlReader.Load(
				new FileStream("Resources/FavoritesItemTemplate.xaml", FileMode.Open)
				) as DataTemplate;
			ItemTemplate = itemTemplate;
		}
	}
}