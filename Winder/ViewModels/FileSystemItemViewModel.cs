using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Winder.ViewModels
{
	public abstract class FileSystemItemViewModel
	{
		public static FileSystemItemViewModel Create(FileSystemInfo fileSystemInfo) {
			if (fileSystemInfo is DirectoryInfo directoryInfo)
				return new DirectoryViewModel(directoryInfo);
			if (fileSystemInfo is FileInfo fileInfo)
				return new FileViewModel(fileInfo);
			throw new NotSupportedException($"{nameof(FileSystemItemViewModel)} doesn't support {fileSystemInfo.GetType().Name}");
		}

		protected FileSystemItemViewModel(FileSystemInfo fileSystemInfo) {
			SourceUntyped = fileSystemInfo;
			_smallImage = new Lazy<ImageSource>(() => WindowsInterop.GetIcon(SourceUntyped.FullName, IsDirectory, true));
			_largeImage = new Lazy<ImageSource>(() => WindowsInterop.GetIcon(SourceUntyped.FullName, IsDirectory, false));
		}

		public string Name => SourceUntyped.Name;

		public string NameWithoutExtension => Path.GetFileNameWithoutExtension(SourceUntyped.FullName);

		public abstract bool IsDirectory { get; }
		public Visibility VisibleIfDirectory => IsDirectory ? Visibility.Visible : Visibility.Hidden;

		public ImageSource SmallImage => _smallImage.Value;
		private Lazy<ImageSource> _smallImage;

		public ImageSource LargeImage => _largeImage.Value;
		private Lazy<ImageSource> _largeImage;

		public FileSystemInfo SourceUntyped { get; private set; }
	}
}