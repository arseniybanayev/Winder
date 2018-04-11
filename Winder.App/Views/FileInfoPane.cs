using System.Windows;
using System.Windows.Controls;
using Winder.App.ViewModels;

namespace Winder.App.Views
{
	public class FileInfoPane : Image, IFileSystemPane
	{
		private readonly FileViewModel _fileViewModel;

		private static readonly WindowsUtilities.FileToIconConverter Icons = new WindowsUtilities.FileToIconConverter();

		public FileInfoPane(FileViewModel file) {
			// Basic display and interactivity settings
			HorizontalAlignment = HorizontalAlignment.Stretch; // within the column it occupies in GridMain
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			_fileViewModel = file;

			Source = Icons.GetImage(file.FullName, 200);
		}

		public string FileSystemItemName => _fileViewModel.Name;
	}
}