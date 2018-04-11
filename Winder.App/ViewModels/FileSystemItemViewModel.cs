using System;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Winder.App.ViewModels
{
	public abstract class FileSystemItemViewModel
	{
		public static FileSystemItemViewModel Create(FileSystemInfo fileSystemInfo) {
			switch (fileSystemInfo) {
				case DirectoryInfo directoryInfo:
					return new DirectoryViewModel(directoryInfo);
				case FileInfo fileInfo:
					return new FileViewModel(fileInfo);
				default:
					throw new NotSupportedException($"{nameof(FileSystemItemViewModel)} doesn't support {fileSystemInfo.GetType().Name}");
			}
		}

		protected FileSystemItemViewModel(FileSystemInfo fileSystemInfo) {
			SourceUntyped = fileSystemInfo;
			_smallImage = new Lazy<ImageSource>(() => WindowsUtilities.WindowsInterop.GetIcon(SourceUntyped.FullName, IsDirectory, true));
			_largeImage = new Lazy<ImageSource>(() => WindowsUtilities.WindowsInterop.GetIcon(SourceUntyped.FullName, IsDirectory, false));
		}

		public string Name => SourceUntyped.Name;

		/// <summary>
		/// Gets the full path of the directory or file.
		/// </summary>
		public string FullName => SourceUntyped.FullName;

		public string NameWithoutExtension => Path.GetFileNameWithoutExtension(SourceUntyped.FullName);

		public abstract bool IsDirectory { get; }
		public Visibility VisibleIfDirectory => IsDirectory ? Visibility.Visible : Visibility.Hidden;

		public ImageSource SmallImage => _smallImage.Value;
		private readonly Lazy<ImageSource> _smallImage;

		public ImageSource LargeImage => _largeImage.Value;
		private readonly Lazy<ImageSource> _largeImage;

		public FileSystemInfo SourceUntyped { get; }
	}
}