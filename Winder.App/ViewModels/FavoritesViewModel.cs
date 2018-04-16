using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Winder.App.Properties;
using Winder.Util;

namespace Winder.App.ViewModels
{
	public class FavoritesViewModel
	{
		private static readonly Lazy<FavoritesViewModel> DefaultLazy = new Lazy<FavoritesViewModel>(() => {
			var favoritePaths = Settings.Default.FavoritePaths?.Cast<string>().ToList();
			var normalizedFavoritePaths = favoritePaths?.Select(p => p.ToNormalizedPath()).ToList();
			return normalizedFavoritePaths != null
				? new FavoritesViewModel(normalizedFavoritePaths)
				: new FavoritesViewModel(FileUtil.GetFavoritesFromUserLinks());
		});

		public static FavoritesViewModel Default => DefaultLazy.Value;

		private readonly Lazy<ObservableCollection<DirectoryViewModel>> _favoriteDirectories;
		public ObservableCollection<DirectoryViewModel> FavoriteDirectories => _favoriteDirectories.Value;

		private FavoritesViewModel(IEnumerable<NormalizedPath> directories) {
			_favoriteDirectories = new Lazy<ObservableCollection<DirectoryViewModel>>(() => {
				var viewModels = directories.Select(FileSystemManager.GetDirectoryViewModel);
				var collection = new ObservableCollection<DirectoryViewModel>(viewModels);
				collection.CollectionChanged += OnCollectionChanged;
				return collection;
			});
		}

		private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs) {
			var stringCollection = new StringCollection();
			stringCollection.AddRange(FavoriteDirectories.Select(d => d.Path.Value).ToArray());
			Settings.Default.FavoritePaths = stringCollection;
			Settings.Default.Save();
		}
	}
}