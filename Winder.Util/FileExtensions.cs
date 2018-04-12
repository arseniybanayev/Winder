using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winder.Util
{
	public static class FileExtensions
	{
		public static DriveInfo GetDriveInfo(this FileInfo file) {
			return file.Directory.GetDriveInfo();
		}

		public static DriveInfo GetDriveInfo(this DirectoryInfo directory) {
			return new DriveInfo(directory.Root.FullName);
		}

		private static readonly string[] ByteSuffixes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; // Longs run out around EB

		public static string ToByteSuffixString(this long byteCount) {
			if (byteCount == 0)
				return "0 " + ByteSuffixes[0];
			var bytes = Math.Abs(byteCount);
			var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
			var num = Math.Round(bytes / Math.Pow(1024, place), 1);
			return $"{Math.Sign(byteCount) * num} {ByteSuffixes[place]}";
		}
	}
}