using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using Winder.App.WindowsUtilities;

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
			_icon = new Lazy<ImageSource>(() => FileSystemImages.GetIcon(SourceUntyped));
		}

		/// <summary>
		/// For files, gets the name of the file.
		/// For directories, gets the name of the last directory in the hierarchy if the hierarchy exists.
		/// Otherwise, the Name property gets the name of the directory.
		/// </summary>
		public string Name => SourceUntyped.Name;

		public string DisplayName {
			get {
				if (SourceUntyped is DirectoryInfo)
					return SourceUntyped.Name;
				if (SourceUntyped.Attributes.HasFlag(FileAttributes.Hidden))
					return SourceUntyped.Name;
				return Path.GetFileNameWithoutExtension(SourceUntyped.Name);
			}
		}
		
		/// <summary>
		/// Gets the full path of the directory or file.
		/// </summary>
		public string FullName => SourceUntyped.FullName;

		public string NameWithoutExtension => Path.GetFileNameWithoutExtension(SourceUntyped.FullName);

		public abstract bool IsDirectory { get; }
		public Visibility VisibleIfDirectory => IsDirectory ? Visibility.Visible : Visibility.Hidden;

		public ImageSource Icon => _icon.Value;
		private readonly Lazy<ImageSource> _icon;

		public FileSystemInfo SourceUntyped { get; }
	}
}