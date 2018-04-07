using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Microsoft.Win32;

namespace Winder
{
	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("8895b1c6-b41f-4c1c-a562-0d564250836f")]
	internal interface IPreviewHandler
	{
		void SetWindow(IntPtr hwnd, ref Rect rect);

		void SetRect(ref Rect rect);

		void DoPreview();
		void Unload();

		void SetFocus();
		void QueryFocus(out IntPtr phwnd);

		[PreserveSig]
		uint TranslateAccelerator(ref MSG pmsg);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("b7d14566-0509-4cce-a71f-0a554233bd9b")]
	internal interface IInitializeWithFile
	{
		void Initialize([MarshalAs(UnmanagedType.LPWStr)] string pszFilePath, uint grfMode);
	}

	[ComImport]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[Guid("b824b49d-22ac-4161-ac8a-9916e8fa3f7f")]
	internal interface IInitializeWithStream
	{
		void Initialize(IStream pstream, uint grfMode);
	}

	public static class Preview
	{
		private static IPreviewHandler _previewHandler;

		internal static IPreviewHandler CreatePreviewHandler(string fileName, out bool isInitialized) {
			const string clsid = "8895b1c6-b41f-4c1c-a562-0d564250836f";
			var g = new Guid(clsid);
			using (var hk = Registry.ClassesRoot.OpenSubKey($@".{Path.GetExtension(fileName)}\ShellEx\{g:B}")) {
				if (hk != null)
					g = new Guid(hk.GetValue("").ToString());
			}

			var a = Type.GetTypeFromCLSID(g, true);
			var o = Activator.CreateInstance(a);

			isInitialized = false;
			switch (o) {
				case IInitializeWithFile fileInit:
					fileInit.Initialize(fileName, 0);
					isInitialized = true;
					break;
				case IInitializeWithStream streamInit:
					var stream = new COMStream(File.Open(fileName, FileMode.Open));
					streamInit.Initialize(stream, 0);
					isInitialized = true;
					break;
			}

			return o as IPreviewHandler;
		}

		public static void AttachPreview(this IntPtr handler, Rect viewRect, string fileName) {
			if (_previewHandler == null)
				_previewHandler = CreatePreviewHandler(fileName, out var isInitialized);
			_previewHandler.SetWindow(handler, ref viewRect);
			_previewHandler.SetRect(ref viewRect);
			_previewHandler.DoPreview();
		}

		public static void InvalidateAttachedPreview(this IntPtr handler, Rect viewRect) {
			_previewHandler?.SetRect(ref viewRect);
		}
	}

	public class PreviewHandler : ContentPresenter
	{
		private IntPtr _mainWindowHandle = IntPtr.Zero;
		private Rect _actualRect;

		protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e) {
			if (e.Property == ContentControl.ContentProperty) {
				if (_mainWindowHandle != IntPtr.Zero)
					_mainWindowHandle.AttachPreview(_actualRect, e.NewValue.ToString());
				return;
			}

			if (e.Property == ActualHeightProperty || e.Property == ActualWidthProperty) {
				if (_mainWindowHandle == IntPtr.Zero) {
					var hwndSource = PresentationSource.FromVisual(Application.Current.MainWindow) as HwndSource;
					_mainWindowHandle = hwndSource.Handle;
				} else {
					var p0 = TranslatePoint(new Point(), Application.Current.MainWindow);
					var p1 = TranslatePoint(new Point(ActualWidth, ActualHeight), Application.Current.MainWindow);
					_actualRect = new Rect(p0, p1);

					// Invalidate attached preview
					_mainWindowHandle.InvalidateAttachedPreview(_actualRect);
				}
			}
		}
	}
	
	public sealed class COMStream : IStream, IDisposable
	{
		private Stream _stream;

		~COMStream() {
			if (_stream == null)
				return;

			_stream.Close();
			_stream.Dispose();
			_stream = null;
		}

		private COMStream() { }

		public COMStream(Stream sourceStream) {
			_stream = sourceStream;
		}

		#region IStream Members 

		public void Clone(out IStream ppstm) {
			throw new NotSupportedException();
		}

		public void Commit(int grfCommitFlags) {
			throw new NotSupportedException();
		}

		public void CopyTo(IStream pstm, long cb, IntPtr pcbRead, IntPtr pcbWritten) {
			throw new NotSupportedException();
		}

		public void LockRegion(long libOffset, long cb, int dwLockType) {
			throw new NotSupportedException();
		}

		[SecurityCritical]
		public void Read(byte[] pv, int cb, IntPtr pcbRead) {
			var count = _stream.Read(pv, 0, cb);
			if (pcbRead != IntPtr.Zero) {
				Marshal.WriteInt32(pcbRead, count);
			}
		}

		public void Revert() {
			throw new NotSupportedException();
		}

		[SecurityCritical]
		public void Seek(long dlibMove, int dwOrigin, IntPtr plibNewPosition) {
			var origin = (SeekOrigin)dwOrigin;
			var pos = _stream.Seek(dlibMove, origin);
			if (plibNewPosition != IntPtr.Zero) {
				Marshal.WriteInt64(plibNewPosition, pos);
			}
		}

		public void SetSize(long libNewSize) {
			_stream.SetLength(libNewSize);
		}

		public void Stat(out System.Runtime.InteropServices.ComTypes.STATSTG pstatstg, int grfStatFlag) {
			pstatstg = new System.Runtime.InteropServices.ComTypes.STATSTG();
			pstatstg.type = 2;
			pstatstg.cbSize = _stream.Length;
			pstatstg.grfMode = 0;
			if (_stream.CanRead && _stream.CanWrite) {
				pstatstg.grfMode |= 2;
			} else if (_stream.CanWrite && !_stream.CanRead) {
				pstatstg.grfMode |= 1;
			} else {
				throw new IOException();
			}
		}

		public void UnlockRegion(long libOffset, long cb, int dwLockType) {
			throw new NotSupportedException();
		}

		[SecurityCritical]
		public void Write(byte[] pv, int cb, IntPtr pcbWritten) {
			_stream.Write(pv, 0, cb);
			if (pcbWritten != IntPtr.Zero) {
				Marshal.WriteInt32(pcbWritten, cb);
			}
		}

		#endregion

		#region IDisposable Members 

		public void Dispose() {
			if (_stream == null)
				return;

			_stream.Close();
			_stream.Dispose();
			_stream = null;
		}

		#endregion
	}
}