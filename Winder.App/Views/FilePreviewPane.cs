using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using Winder.Preview;

namespace Winder.App.Views
{
	public class FilePreviewPane : UserControl, IFileSystemPane
	{
		private readonly FileViewModel _fileViewModel;
		private readonly PreviewHandlerControl _previewHandlerControl;

		public FilePreviewPane(FileViewModel file) {
			// Basic display and interactivity settings
			HorizontalAlignment = HorizontalAlignment.Stretch; // within the column it occupies in GridMain
			SetValue(ScrollViewer.HorizontalScrollBarVisibilityProperty, ScrollBarVisibility.Disabled);

			_fileViewModel = file;

			// The preview handler control is a Windows Forms control because
			// it needs an IntPtr handle for the view to be occupied by the preview,
			// but in WPF, individual views/controls are not windows with HWND handles
			// like they are in WinForms.
			var winFormsHost = new WindowsFormsHost();
			winFormsHost.Child = _previewHandlerControl = new PreviewHandlerControl(_fileViewModel.Source);
			AddChild(winFormsHost);
			_previewHandlerControl.GetInitializeTask();
		}

		public void UnloadPreviewHandler() => _previewHandlerControl.Unload();

		public string FileSystemItemName => _fileViewModel.Name;
	}
}