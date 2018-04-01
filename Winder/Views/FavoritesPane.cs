using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using Winder.ViewModels;

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
			BorderThickness = new Thickness(0);
			Background = new SolidColorBrush(Color.FromRgb(246, 246, 246));

			ItemsSource = favorites.Children;

			// Template for items
			var itemTemplate = XamlReader.Load(
				new FileStream("Resources/FavoritesItemTemplate.xaml", FileMode.Open)
				) as DataTemplate;
			ItemTemplate = itemTemplate;
		}

		public DirectoryViewModel SelectedDirectory => SelectedItems.OfType<DirectoryViewModel>().FirstOrDefault();
	}
}