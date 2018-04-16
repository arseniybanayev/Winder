using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Winder.App.Properties;
using Winder.Util;

namespace Winder.App.ViewModels
{
	public static class FileSystemManager
	{
		public static DirectoryViewModel GetDirectoryViewModel(NormalizedPath path) {
			lock (CachedDirectories) {
				if (_watcher == null)
					StartWatcher(path);
				return CachedDirectories.GetOrAdd(path, p => {
					var directory = new DirectoryViewModel(p);
					directory.UpdateCollection(collection => {
						foreach (var c in GetVisibleChildren(directory))
							collection.Add(c);
					});
					return directory;
				});
			}
		}

		private static IEnumerable<FileSystemItemViewModel> GetVisibleChildren(DirectoryViewModel directory) {
			try {
				var children = directory.Info.GetFileSystemInfos();
				var childrenToShow = Settings.Default.ShowHiddenFiles
					? children
					: children.Where(i => !i.IsReallyHidden());
				return childrenToShow.Select(c => c is FileInfo
					? new FileViewModel(c.FullName.ToNormalizedPath()) as FileSystemItemViewModel
					: new DirectoryViewModel(c.FullName.ToNormalizedPath()));
			} catch (UnauthorizedAccessException) {
				Log.Error($"Unauthorized access to {directory.Path}");
				return Enumerable.Empty<FileSystemItemViewModel>();
			}
		}

		private static FileSystemWatcher _watcher;
		private static readonly Dictionary<NormalizedPath, DirectoryViewModel> CachedDirectories = new Dictionary<NormalizedPath, DirectoryViewModel>();

		private static void StartWatcher(string path) {
			_watcher = new FileSystemWatcher {
				Path = path,
				NotifyFilter = NotifyFilters.DirectoryName | NotifyFilters.FileName
			};

			_watcher.Changed += Watcher_Changed;
			_watcher.Created += Watcher_Created;
			_watcher.Deleted += Watcher_Deleted;
			_watcher.Renamed += Watcher_Renamed;

			_watcher.EnableRaisingEvents = true; // Start watching
		}

		private static void Watcher_Changed(object sender, FileSystemEventArgs e) {
			//throw new NotImplementedException();
		}

		private static void Watcher_Created(object sender, FileSystemEventArgs e) {
			//var path = e.FullPath;
			//var directory = Path.GetDirectoryName()
		}

		private static void Watcher_Deleted(object sender, FileSystemEventArgs e) {
			//throw new NotImplementedException();
		}

		private static void Watcher_Renamed(object sender, RenamedEventArgs e) {
			//throw new NotImplementedException();
		}
	}
}