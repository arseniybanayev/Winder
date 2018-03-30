using System.Windows.Controls;

namespace Winder.Views
{
	public class FavoritesPane : ListBox
	{
		public FavoritesPane(Favorites favorites) {
			ItemsSource = favorites.Children;
		}
	}
}