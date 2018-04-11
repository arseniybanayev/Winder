using System.IO;
using System.Windows;
using System.Windows.Interop;
using Winder.App.Preview;

namespace Winder.App
{
	/// <summary>
	/// Interaction logic for FilePreviewWindow.xaml
	/// </summary>
	public partial class FilePreviewWindow : Window
	{
		private readonly PreviewHandlerControl _previewHandlerControl;

		public FilePreviewWindow(FileInfo file) {
			InitializeComponent();

			// The preview handler control is a Windows Forms control because
			// it needs an IntPtr handle for the view to be occupied by the preview,
			// but in WPF, individual views/controls are not windows with HWND handles
			// like they are in WinForms.
			var handle = new WindowInteropHelper(this).EnsureHandle();
			_previewHandlerControl = new PreviewHandlerControl(file, handle);
			Content = _previewHandlerControl;
			_previewHandlerControl.LoadPreview();
		}
	}
}