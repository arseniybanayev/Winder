using System;
using System.Windows;
using System.Windows.Controls;
using Winder.App.ViewModels;
using Winder.App.WindowsUtilities;

namespace Winder.App.Views
{
	public class FileInfoPane : Image, IFileSystemPane
	{
		private readonly FileViewModel _fileViewModel;

		public FileInfoPane(FileViewModel file) {
			// Basic display and interactivity settings
			HorizontalAlignment = HorizontalAlignment.Stretch; // within the column it occupies in GridMain
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			_fileViewModel = file;

			Source = FileSystemImages.GetThumbnail(file.Source);
		}

		private static readonly string[] ByteSuffixes = {"B", "KB", "MB", "GB", "TB", "PB", "EB"}; // Longs run out around EB

		public string FileSizeString {
			get {
				var byteCount = _fileViewModel.Source.Length;
				if (byteCount == 0)
					return "0" + ByteSuffixes[0];
				var bytes = Math.Abs(byteCount);
				var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
				var num = Math.Round(bytes / Math.Pow(1024, place), 1);
				return $"{Math.Sign(byteCount) * num}{ByteSuffixes[place]}";
			}
		}
	}
}