using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Winder.Properties;
using Winder.ViewModels;

namespace Winder
{
	public class Favorites
	{
		private Lazy<ObservableCollection<DirectoryViewModel>> _children;
		public ObservableCollection<DirectoryViewModel> Children => _children.Value;

		private Favorites(IEnumerable<string> directoryPaths) {
			_children = new Lazy<ObservableCollection<DirectoryViewModel>>(() => {
				var viewModels = directoryPaths.Select(p => new DirectoryViewModel(new DirectoryInfo(p)));
				var collection = new ObservableCollection<DirectoryViewModel>(viewModels);
				collection.CollectionChanged += FavoritesCollectionChanged;
				return collection;
			});
		}

		private void FavoritesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
			var stringCollection = new StringCollection();
			stringCollection.AddRange(Children.Select(c => c.FullName).ToArray());
			Settings.Default.FavoritePaths = stringCollection;
			Settings.Default.Save();
		}

		public static string[] DefaultFavoriteDirectoryPaths = {
			@"C:\Users\arsen\Sheet Music",
			@"C:\Users\arsen\My Pictures"
		};

		public static Favorites Load() {
			var collectionFromSettings = Settings.Default.FavoritePaths;
			return new Favorites(collectionFromSettings?.Cast<string>() ?? DefaultFavoriteDirectoryPaths);
		}
	}
}