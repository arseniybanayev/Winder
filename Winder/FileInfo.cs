using System;
using System.IO;

namespace Winder
{
	public abstract class FileSystemInfoExtended
	{
		protected FileSystemInfo SourceUntyped { get; }

		protected FileSystemInfoExtended(FileSystemInfo source)
		{
			SourceUntyped = source;
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

		public abstract bool IsDirectory { get; }
	}

	public class DirectoryInfoExtended : FileSystemInfoExtended
	{
		public DirectoryInfoExtended(DirectoryInfo source) : base(source) { }

		public DirectoryInfo Source => (DirectoryInfo)SourceUntyped;

		public override bool IsDirectory => true;
	}

	public class FileInfoExtended : FileSystemInfoExtended
	{
		public FileInfoExtended(FileInfo source) : base(source) { }

		public FileInfo Source => (FileInfo)SourceUntyped;

		public override bool IsDirectory => false;
	}
}