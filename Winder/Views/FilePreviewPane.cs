using System.Windows;
using System.Windows.Controls;
using Winder.ViewModels;

namespace Winder.Views
{
	public class FilePreviewPane : Image, IFileSystemPane
	{
		public FilePreviewPane(FileViewModel file) {
			// Basic display and interactivity settings
			HorizontalAlignment = HorizontalAlignment.Left; // within the column it occupies in GridMain
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			Source = file.LargeImage;
		}
	}
}