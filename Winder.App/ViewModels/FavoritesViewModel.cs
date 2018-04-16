﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Winder.Util;

namespace Winder.App.ViewModels
{
	public class FavoritesViewModel
	{
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		private readonly Lazy<ObservableCollection<DirectoryViewModel>> _favoriteDirectories;
		public ObservableCollection<DirectoryViewModel> FavoriteDirectories => _favoriteDirectories.Value;

		private FavoritesViewModel(IEnumerable<NormalizedPath> directories) {
			_favoriteDirectories = new Lazy<ObservableCollection<DirectoryViewModel>>(() => {
				var viewModels = directories.Select(dir => FileSystemManager.GetDirectoryViewModel(dir));
				var collection = new ObservableCollection<DirectoryViewModel>(viewModels);
				collection.CollectionChanged += (sender, e) => CollectionChanged?.Invoke(sender, e);
				return collection;
			});
		}
		
		public static FavoritesViewModel Load(IEnumerable<NormalizedPath> favoritePaths = null) {
			return favoritePaths != null
				? new FavoritesViewModel(favoritePaths)
				: new FavoritesViewModel(FileUtil.GetFavoritesFromUserLinks());
		}
	}
}