using System;
using System.IO;
using System.Windows.Media;
using Winder.App.WindowsUtilities;
using Winder.Util;

namespace Winder.App.ViewModels
{
	public class FileViewModel : FileSystemItemViewModel
	{
		public FileViewModel(NormalizedPath path) : base(path) {
			_icon = new Lazy<ImageSource>(() => FileSystemImages.GetIcon(path, false));
			Info = new FileInfo(path);
		}

		/// <summary>
		/// Should be called by <see cref="FileSystemManager"/> to update the known <see cref="FileInfo"/>
		/// and notify any watchers that properties derived from the new info might have changed.
		/// </summary>
		public void UpdateInfo(FileInfo newInfo) {
			Info = newInfo;
		}

		public FileInfo Info { get; private set; }
		public override FileSystemInfo FileSystemInfo => Info;

		public override string DisplayName {
			get {
				if (Info.Attributes.HasFlag(FileAttributes.Hidden))
					return Info.Name;
				if (Info.Name.StartsWith("."))
					return Info.Name;
				return System.IO.Path.GetFileNameWithoutExtension(Info.Name);
			}
		}

		public string FileSize {
			get {
				var byteCount = Info.Length;
				return byteCount.ToByteSuffixString();
			}
		}
	}
}