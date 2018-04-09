using System;
using System.Windows;
using System.Windows.Controls;
using Winder.Preview;

namespace Winder.App.Views
{
	public class FileInfoPane : Image, IFileSystemPane, IDisposable
	{
		private readonly FileViewModel _fileViewModel;
		private readonly PreviewHandlerControl _previewHandlerControl;

		private static readonly FileToIconConverter Icons = new FileToIconConverter();

		public FileInfoPane(FileViewModel file) {
			// Basic display and interactivity settings
			HorizontalAlignment = HorizontalAlignment.Stretch; // within the column it occupies in GridMain
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			_fileViewModel = file;

			Source = Icons.GetImage(file.FullName, 200);
		}

		public void Dispose() => _previewHandlerControl?.Unload();

		public string FileSystemItemName => _fileViewModel.Name;
	}
}