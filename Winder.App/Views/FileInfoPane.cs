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

		public string FileSystemItemName => _fileViewModel.Name;
	}
}