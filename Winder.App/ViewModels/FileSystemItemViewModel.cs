using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Winder.Util;

namespace Winder.App.ViewModels
{
	public abstract class FileSystemItemViewModel
	{
		protected FileSystemItemViewModel(NormalizedPath path) {
			Path = path;
		}

		public NormalizedPath Path { get; }

		public abstract FileSystemInfo FileSystemInfo { get; }
		public bool IsDirectory => FileSystemInfo is DirectoryInfo;

		public Visibility VisibleIfDirectory => IsDirectory ? Visibility.Visible : Visibility.Hidden;
		public abstract string DisplayName { get; }

		public void Open() {
			Log.Info($"Opening {Path}");
			Process.Start(Path);
		}

		public void MoveToTrash() {
			Log.Info($"Moving to trash (recycle bin): {Path}");
			FileUtil.SendToRecycleBin(Path);
		}
		
		public ImageSource Icon => _icon.Value;
		protected Lazy<ImageSource> _icon;
	}
}