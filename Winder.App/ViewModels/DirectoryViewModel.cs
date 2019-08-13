using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Media;
using Winder.App.Properties;
using Winder.App.WindowsUtilities;
using Winder.Util;

namespace Winder.App.ViewModels
{
	public class DirectoryViewModel : FileSystemItemViewModel
	{
		/// <summary>
		/// Gets a new <see cref="DirectoryViewModel"/> for the supplied path.
		/// 
		/// The children are found, and new <see cref="FileSystemItemViewModel"/>s are
		/// created, the first time that <see cref="Children"/> is called.
		/// 
		/// TODO: Improve performance of <see cref="Children"/>. Perhaps
		/// aggressively preload the children, or manage it with a queue of tasks, etc.
		/// 
		/// TODO: Reflect changes made to the file system outside of Winder. Perhaps
		/// use a <see cref="FileSystemWatcher"/>.
		/// </summary>
		public static DirectoryViewModel Get(NormalizedPath path) {
			return Cache.GetOrAdd(path, p => new DirectoryViewModel(p));
		}

		private DirectoryViewModel(NormalizedPath path) : base(path) {
			_icon = new Lazy<ImageSource>(() => FileSystemImages.GetIcon(path, true));
			Info = new DirectoryInfo(path);
			_children = new Lazy<ObservableCollection<FileSystemItemViewModel>>(
				() => new ObservableCollection<FileSystemItemViewModel>(GetVisibleChildren(Info)));
		}

		private static IEnumerable<FileSystemItemViewModel> GetVisibleChildren(DirectoryInfo info) {
			try {
				//var children = Directory.EnumerateFileSystemEntries(path.Value);
				var children = info.EnumerateFileSystemInfos();
				var childrenToShow = Settings.Default.ShowHiddenFiles
					? children
					: children.Where(i => !i.IsReallyHidden());
				return childrenToShow.Select(c => c is FileInfo
					? new FileViewModel(c.FullName.ToNormalizedPath()) as FileSystemItemViewModel
					: DirectoryViewModel.Get(c.FullName.ToNormalizedPath()));
			} catch (UnauthorizedAccessException) {
				Log.Error($"Unauthorized access to {info.FullName.ToNormalizedPath()}");
				return Enumerable.Empty<FileSystemItemViewModel>();
			}
		}
		
		private readonly Lazy<ObservableCollection<FileSystemItemViewModel>> _children;
		public ObservableCollection<FileSystemItemViewModel> Children => _children.Value;
		
		public DirectoryInfo Info { get; private set; }
		public override FileSystemInfo FileSystemInfo => Info;

		public override string DisplayName => Info.Name;

		private static readonly ConcurrentDictionary<NormalizedPath, DirectoryViewModel> Cache
			= new ConcurrentDictionary<NormalizedPath, DirectoryViewModel>();
	}
}