using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Winder.Properties;
using Winder.Util;
using Winder.ViewModels;

namespace Winder
{
	public class Favorites
	{
		private readonly Lazy<ObservableCollection<DirectoryViewModel>> _children;
		public ObservableCollection<DirectoryViewModel> Children => _children.Value;

		private Favorites(IEnumerable<DirectoryInfo> directories) {
			_children = new Lazy<ObservableCollection<DirectoryViewModel>>(() => {
				var viewModels = directories.Select(dir => new DirectoryViewModel(dir));
				var collection = new ObservableCollection<DirectoryViewModel>(viewModels);
				collection.CollectionChanged += (o, e) => SaveFavoritePaths();
				return collection;
			});
		}

		private void SaveFavoritePaths() {
			var stringCollection = new StringCollection();
			stringCollection.AddRange(Children.Select(c => c.FullName).ToArray());
			Settings.Default.FavoritePaths = stringCollection;
			Settings.Default.Save();
		}

		public static Favorites Load() {
			var collectionFromSettings = Settings.Default.FavoritePaths;
			if (collectionFromSettings != null)
				return new Favorites(collectionFromSettings.Cast<string>().Select(p => new DirectoryInfo(p)));
			var favorites = new Favorites(GetFavoritesFromUserLinks());
			favorites.SaveFavoritePaths();
			return favorites;
		}

		// NOT VERY FAST so don't run too many times
		private static IEnumerable<DirectoryInfo> GetFavoritesFromUserLinks() {
			var userProfilePath = Environment.GetEnvironmentVariable("USERPROFILE");
			if (userProfilePath == null || !Directory.Exists(userProfilePath)) {
				Log.Info("Could not get directory from system environment variable USERPROFILE");
				return Enumerable.Empty<DirectoryInfo>();
			}

			var path = Path.Combine(userProfilePath, "Links");
			if (!Directory.Exists(path)) {
				Log.Info($"Could not find Links directory in user profile path {userProfilePath}");
				return Enumerable.Empty<DirectoryInfo>();
			}

			var links = new DirectoryInfo(path).GetFiles().Where(f => f.Extension.ToLower() == ".lnk");
			return links.Select(lnk => FileUtil.GetShortcutTarget(lnk.FullName))
				.Where(tgt => !string.IsNullOrWhiteSpace(tgt)) // Things like "Recent Places"
				.Where(tgt => File.GetAttributes(tgt).HasFlag(FileAttributes.Directory)) // Slow
				.Select(tgt => new DirectoryInfo(tgt));
		}
	}
}