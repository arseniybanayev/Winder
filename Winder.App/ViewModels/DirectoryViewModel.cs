using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Winder.App.Properties;
using Winder.Util;

namespace Winder.App.ViewModels
{
	public class DirectoryViewModel : FileSystemItemViewModel
	{
		private readonly Lazy<ObservableCollection<FileSystemItemViewModel>> _children;
		public ObservableCollection<FileSystemItemViewModel> Children => _children.Value;

		public DirectoryViewModel(DirectoryInfo directoryInfo) : base(directoryInfo) {
			_children = new Lazy<ObservableCollection<FileSystemItemViewModel>>(
				() => new ObservableCollection<FileSystemItemViewModel>(GetVisibleChildren(Source).Select(Create)));
		}

		public DirectoryInfo Source => (DirectoryInfo)SourceUntyped;

		private static IEnumerable<FileSystemInfo> GetVisibleChildren(DirectoryInfo directory) {
			try {
				var children = directory.GetFileSystemInfos();
				return Settings.Default.ShowHiddenFiles
					? children
					: children.Where(i => !i.IsReallyHidden());
			} catch (UnauthorizedAccessException) {
				Log.Error($"Unauthorized access to {directory.FullName}");
				return Enumerable.Empty<FileSystemInfo>();
			}
		}

		public override bool IsDirectory => true;
	}
}