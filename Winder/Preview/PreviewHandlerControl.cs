using System;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using System.Drawing;
using System.Collections.Generic;
using System.Windows.Forms;
using Winder.Preview.ComInterop;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Threading;
using Winder.Util;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Winder.Preview
{
	// The preview handler control is a Windows Forms control because
	// it needs an IntPtr handle for the view to be occupied by the preview,
	// but in WPF, individual views/controls are not windows with HWND handles
	// like they are in WinForms.
	public partial class PreviewHandlerControl : UserControl
	{
		private readonly FileInfo _file;
		private IPreviewHandler _currentPreviewHandler;
		private Stream _currentStream;

		public delegate void PreviewInitializedEventHandler(bool actuallyShowingPreview);
		public event PreviewInitializedEventHandler PreviewInitialized;

		public PreviewHandlerControl(FileInfo file) {
			var stopwatchConstructor = Stopwatch.StartNew();
			InitializeComponent();
			_file = file;

			var stopwatchTaskStart = Stopwatch.StartNew();
			Task.Run(() => {
				try {
					var stopatchReadingRegistry = Stopwatch.StartNew();
					var guid = GetPreviewHandlerGUID(_file.Extension);
					if (guid == Guid.Empty) {
						// TODO: Show default image because we have no preview handler
						_initializeTask.TrySetResult(false);
						return;
					}
					Log.Info($"Reading the registry took {stopatchReadingRegistry.ElapsedMilliseconds}ms");

					// Create the COM type using reflection
					var stopwatchInitializePreviewHandler = Stopwatch.StartNew();
					Type comType = Type.GetTypeFromCLSID(guid);
					_currentPreviewHandler = (IPreviewHandler)Activator.CreateInstance(comType);

					// Load it either as a file or a stream, as appropriate
					if (_currentPreviewHandler is IInitializeWithFile initWithFile) {
						initWithFile.Initialize(_file.FullName, 0);
					} else if (_currentPreviewHandler is IInitializeWithStream initWithStream) {
						_currentStream = File.Open(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
						var stream = new StreamWrapper(_currentStream);
						initWithStream.Initialize(stream, 0);
					} else {
						_initializeTask.TrySetResult(false);
						return;
					}

					Log.Info($"Initializing the preview handler took {stopwatchInitializePreviewHandler.ElapsedMilliseconds}ms");

					var rect = GetCurrentRect();

					// Run on UI thread
					System.Windows.Application.Current.Dispatcher.InvokeAsync(() => {
						var stopwatchDispatcherCode = Stopwatch.StartNew();
						if (_cancel.IsCancellationRequested) return;
						_currentPreviewHandler.SetWindow(this.Handle, ref rect);
						if (_cancel.IsCancellationRequested) return; 
						_currentPreviewHandler.DoPreview();
						Log.Info($"Dispatcher code took {stopwatchDispatcherCode.ElapsedMilliseconds}ms");
					}).Task.ContinueWith(t => {
						if (t.IsCompleted)
							_initializeTask.TrySetResult(true);
						else if (t.IsFaulted)
							_initializeTask.TrySetException(t.Exception);
					});
				} catch (Exception e) {
					Log.Error($"Error initializing {nameof(PreviewHandlerControl)} for '{file?.FullName}'", e);
					_initializeTask.TrySetException(e);
				}
			});
			Log.Info($"Constructor of {nameof(PreviewHandlerControl)} took {stopwatchConstructor.ElapsedMilliseconds}ms");
			Log.Info($"Starting the task in {nameof(PreviewHandlerControl)} took {stopwatchTaskStart.ElapsedMilliseconds}ms");
		}

		private TaskCompletionSource<bool> _initializeTask = new TaskCompletionSource<bool>();
		private CancellationTokenSource _cancel = new CancellationTokenSource();

		public Task GetInitializeTask() => _initializeTask.Task;

		private static Guid GetPreviewHandlerGUID(string fileExtension) {
			// open the registry key corresponding to the file extension
			RegistryKey ext = Registry.ClassesRoot.OpenSubKey(fileExtension);
			if (ext != null) {
				// open the key that indicates the GUID of the preview handler type
				RegistryKey test = ext.OpenSubKey("shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
				if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));

				// sometimes preview handlers are declared on key for the class
				string className = Convert.ToString(ext.GetValue(null));
				if (className != null) {
					test = Registry.ClassesRoot.OpenSubKey(className + "\\shellex\\{8895b1c6-b41f-4c1c-a562-0d564250836f}");
					if (test != null) return new Guid(Convert.ToString(test.GetValue(null)));
				}
			}

			return Guid.Empty;
		}

		private void OnResize(object sender, EventArgs e) {
			if (_currentPreviewHandler != null) {
				var rect = GetCurrentRect();
				_currentPreviewHandler.SetRect(ref rect);
			}
		}

		private RECT GetCurrentRect() {
			RECT rect;
			rect.top = 0;
			rect.bottom = this.Height;
			rect.left = 0;
			rect.right = this.Width;
			return rect;
		}

		public void Unload() {
			_cancel.Cancel();
			Task.Run(() => {
				GetInitializeTask().Wait();
				if (_currentPreviewHandler != null) {
					_currentPreviewHandler.Unload();
					Marshal.FinalReleaseComObject(_currentPreviewHandler);
					_currentPreviewHandler = null;
					GC.Collect();
				}

				_currentStream?.Close();
				_currentStream = null;
			});
		}
	}
}