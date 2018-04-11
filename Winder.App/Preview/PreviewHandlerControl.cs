using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Winder.App.Preview.ComInterop;
using Winder.Util;

namespace Winder.App.Preview
{
	/// <summary>
	/// Given a <see cref="FileInfo"/>, this control is responsible for loading
	/// the file preview into its visible area. It's a Windows Forms control (see
	/// remarks).
	/// </summary>
	public class PreviewHandlerControl : UserControl
	{
		/// <summary>
		/// This is just a way to ensure the loading happens atomically and allow
		/// most of the loading to happen in a background thread.
		/// </summary>
		private Lazy<IPreviewHandler> _previewHandlerLazy;

		private bool _loadingPreview;
		private Stream _currentStream;

		public PreviewHandlerControl(FileInfo file, IntPtr windowHandle) {
			_previewHandlerLazy = new Lazy<IPreviewHandler>(() => {
				try {
					var guid = GetPreviewHandlerGUID(file.Extension);
					if (guid == Guid.Empty) {
						Log.Error($"Did not find a preview handler for file extension '{file.Extension}': file='{file.FullName}'");
						PreviewLoaded?.Invoke(false);
						return null;
					}

					// Create the COM type using reflection
					Log.Info($"Found preview handler for file extension '{file.Extension}': guid={guid}, file='{file.FullName}'");
					var comType = Type.GetTypeFromCLSID(guid);
					var previewHandler = (IPreviewHandler)Activator.CreateInstance(comType);

					// Load it either as a file or a stream, as appropriate
					switch (previewHandler) {
						case IInitializeWithFile initWithFile:
							initWithFile.Initialize(file.FullName, 0);
							break;
						case IInitializeWithStream initWithStream:
							_currentStream = File.Open(file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
							var stream = new StreamWrapper(_currentStream);
							initWithStream.Initialize(stream, 0);
							break;
						default:
							Log.Error($"Preview handler for file is neither {nameof(IInitializeWithFile)} nor {nameof(IInitializeWithStream)}: '{file.FullName}'");
							PreviewLoaded?.Invoke(false);
							return null;
					}

					Application.Current.Dispatcher.Invoke(() => {
						var rect = GetCurrentRect();
						previewHandler.SetWindow(windowHandle, ref rect);
						previewHandler.DoPreview();
						previewHandler.SetRect(ref rect);
					});

					PreviewLoaded?.Invoke(true);
					return previewHandler;
				} catch (Exception e) {
					Log.Error($"Error initializing {nameof(PreviewHandlerControl)} for '{file?.FullName}'", e);
					PreviewLoaded?.Invoke(false);
					return null;
				}
			});
		}

		public delegate void PreviewLoadedHandler(bool success);
		public event PreviewLoadedHandler PreviewLoaded;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			base.OnPropertyChanged(e);
			if (e.Property != ActualHeightProperty && e.Property != ActualWidthProperty)
				return;
			if (ActualHeight <= 0 || ActualWidth <= 0)
				return;
			if ((_previewHandlerLazy?.IsValueCreated ?? false) && _previewHandlerLazy.Value != null) {
				var rect = GetCurrentRect();
				_previewHandlerLazy.Value.SetRect(ref rect);
			}
		}

		/// <summary>
		/// Starts a task in the background to load the preview handler, and then
		/// signals the UI thread to draw the preview.
		/// </summary>
		public void LoadPreview() {
			_loadingPreview = true;
			Task.Run(() => _previewHandlerLazy.Value);
		}

		public void Unload() {
			if (!_loadingPreview)
				return;
			Task.Run(() => {
				var previewHandler = _previewHandlerLazy.Value;
				if (previewHandler == null)
					return;

				previewHandler.Unload();
				Marshal.FinalReleaseComObject(_previewHandlerLazy);
				_previewHandlerLazy = null;
				GC.Collect();

				_currentStream?.Close();
				_currentStream = null;
			});
		}

		private static Guid GetPreviewHandlerGUID(string fileExtension) {
			// open the registry key corresponding to the file extension
			var ext = Registry.ClassesRoot.OpenSubKey(fileExtension);
			if (ext == null) return Guid.Empty;

			// open the key that indicates the GUID of the preview handler type
			var test = ext.OpenSubKey("shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
			if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));

			// sometimes preview handlers are declared on key for the class
			var className = Convert.ToString(ext.GetValue(null));
			test = Registry.ClassesRoot.OpenSubKey(className + "\\shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
			return test != null
				? new Guid(Convert.ToString(test.GetValue(null)))
				: Guid.Empty;
		}

		private RECT GetCurrentRect() {
			var dpiMatrix = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
			var dpiWidthFactor = dpiMatrix.M11;
			var dpiHeightFactor = dpiMatrix.M22;

			RECT rect;
			rect.top = 0;
			rect.bottom = (int)(ActualHeight * dpiHeightFactor);
			rect.left = 0;
			rect.right = (int)(ActualWidth * dpiWidthFactor);
			return rect;
		}
	}
}