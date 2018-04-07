using System.Windows;
using System.Windows.Controls;

namespace Winder.App.Views
{
	

	public class FilePreviewPane : Image, IFileSystemPane
	{
		private readonly FileViewModel _fileViewModel;

		public FilePreviewPane(FileViewModel file) {
			// Basic display and interactivity settings
			HorizontalAlignment = HorizontalAlignment.Left; // within the column it occupies in GridMain
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			_fileViewModel = file;
			Source = _fileViewModel.LargeImage;
		}

		public string FileSystemItemName => _fileViewModel.Name;
	}
}