using System.Collections.Generic;
using System.IO;
using Winder.Util;

namespace Winder.App.ViewModels
{
	public static class FileSystemManager
	{
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