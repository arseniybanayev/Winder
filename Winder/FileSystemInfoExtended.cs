using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace Winder
{
	public abstract class FileSystemInfoExtended
	{
		internal FileSystemInfo SourceUntyped { get; }

		protected FileSystemInfoExtended(FileSystemInfo source)
		{
			SourceUntyped = source;
			_iconImage = new Lazy<ImageSource>(() => WindowsInterop.GetIcon(SourceUntyped.FullName, IsDirectory, true));
		}

		public static FileSystemInfoExtended Create(FileSystemInfo source)
		{
			if (source is DirectoryInfo directoryInfo)
				return new DirectoryInfoExtended(directoryInfo);
			if (source is FileInfo fileInfo)
				return new FileInfoExtended(fileInfo);
			throw new NotSupportedException($"{nameof(FileSystemInfoExtended)} doesn't support {source.GetType().Name}");
		}

		public string Name => SourceUntyped.Name;

		public string NameWithoutExtension => Path.GetFileNameWithoutExtension(SourceUntyped.FullName);

		public abstract bool IsDirectory { get; }
		public Visibility VisibleIfDirectory => IsDirectory ? Visibility.Visible : Visibility.Hidden;

		public ImageSource IconImage => _iconImage.Value;

		private Lazy<ImageSource> _iconImage;
	}

	public class DirectoryInfoExtended : FileSystemInfoExtended
	{
		public DirectoryInfoExtended(string path) : this(new DirectoryInfo(path)) { }
		public DirectoryInfoExtended(DirectoryInfo source) : base(source) { }

		public DirectoryInfo Source => (DirectoryInfo)SourceUntyped;

		public override bool IsDirectory => true;

		internal IEnumerable<FileSystemInfoExtended> GetChildren() => Source.GetFileSystemInfos().Select(FileSystemInfoExtended.Create);
	}

	public class FileInfoExtended : FileSystemInfoExtended
	{
		public FileInfoExtended(string path) : this(new FileInfo(path)) { }
		public FileInfoExtended(FileInfo source) : base(source) { }

		public FileInfo Source => (FileInfo)SourceUntyped;

		public override bool IsDirectory => false;
	}
}