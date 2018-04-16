using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Winder.Util;

namespace Winder.App.WindowsUtilities
{
	/// <summary>
	/// Helper functions to retrieve icons and thumbnails for items in the file system
	/// using Windows shell extensions and interop.
	/// </summary>
	public static class FileSystemImages
	{
		/// <summary>
		/// Returns an image containing a thumbnail (larger than icon) for the specified file/directory.
		/// </summary>
		public static ImageSource GetThumbnail(NormalizedPath path, bool isDirectory) {
			var sw = Stopwatch.StartNew();
			try {
				// Executables and directories can have individual icons
				// Images are shown as full image previews, which are individual
				if (isDirectory || IsExecutable(path) || IsImage(path))
					return ThumbnailCacheByPath.GetOrAdd(path, p => GetThumbnailUnsafe(p, isDirectory));

				// Every other type of file can be cached by extension
				return ThumbnailCacheByExtension.GetOrAdd(path.Extension.ToLower(), _ => GetThumbnailUnsafe(path, false));
			} finally {
				Log.Info($"{nameof(GetThumbnail)} took {sw.ElapsedMilliseconds / 1000.0}s for {path}");
			}
		}

		/// <summary>
		/// Returns an image containing an icon (smaller than thumbnail) for the specified file/directory.
		/// </summary>
		public static ImageSource GetIcon(NormalizedPath path, bool isDirectory) {
			var sw = Stopwatch.StartNew();
			try {
				// Executables and directories can have individual icons
				if (isDirectory || IsExecutable(path))
					return IconCacheByPath.GetOrAdd(path, p => GetIconUnsafe(p, isDirectory));

				// Every other type of file can be cached by extension
				return IconCacheByExtension.GetOrAdd(path.Extension.ToLower(), _ => GetIconUnsafe(path, false));
			} finally {
				Log.Info($"{nameof(GetIcon)} took {sw.ElapsedMilliseconds / 1000.0}s for {path}");
			}
		}

		#region Implementation

		private static ImageSource GetThumbnailUnsafe(NormalizedPath path, bool isDirectory) {
			var sw = Stopwatch.StartNew();
			try {
				// At this size, images are shown as full image previews
				if (!isDirectory && IsImage(path)) {
					var bitmap = new WriteableBitmap(LoadBitmap(LoadJumbo(path, false)));
					ThreadPool.QueueUserWorkItem(ImageThumbnailCallback, new ThumbnailInfo(bitmap, path, IconSize.Thumbnail));
					return bitmap;
				}

				return LoadBitmap(LoadJumbo(path, isDirectory));
			} catch (Exception e) {
				Log.Error($"Failed to get thumbnail for {path}", e);
				return null;
			} finally {
				Log.Info($"{nameof(GetThumbnailUnsafe)} took {sw.ElapsedMilliseconds / 1000.0}s for {path}");
			}
		}

		private static ImageSource GetIconUnsafe(NormalizedPath path, bool isDirectory) {
			var sw = Stopwatch.StartNew();
			try {
				var largeFileIcon = GetLargeFileIcon(path, isDirectory);
				var sw2 = Stopwatch.StartNew();
				var bitmap = largeFileIcon.ToBitmap();
				Log.Info($"{nameof(largeFileIcon.ToBitmap)} took {sw2.ElapsedMilliseconds / 1000.0}s for {path}");
				return LoadBitmap(bitmap);
			} catch (Exception e) {
				Log.Error($"Failed to get icon for {path}", e);
				return null;
			} finally {
				Log.Info($"{nameof(GetIconUnsafe)} took {sw.ElapsedMilliseconds / 1000.0}s for {path}");
			}
		}

		private static readonly ConcurrentDictionary<string, ImageSource> ThumbnailCacheByExtension = new ConcurrentDictionary<string, ImageSource>();
		private static readonly ConcurrentDictionary<NormalizedPath, ImageSource> ThumbnailCacheByPath = new ConcurrentDictionary<NormalizedPath, ImageSource>();

		private static readonly ConcurrentDictionary<string, ImageSource> IconCacheByExtension = new ConcurrentDictionary<string, ImageSource>();
		private static readonly ConcurrentDictionary<NormalizedPath, ImageSource> IconCacheByPath = new ConcurrentDictionary<NormalizedPath, ImageSource>();
		
		private enum IconSize
		{
			Small,
			Large,
			ExtraLarge,
			Jumbo,
			Thumbnail
		}

		private class ThumbnailInfo
		{
			public readonly IconSize Iconsize;
			public readonly string NormalizedPath;
			public readonly WriteableBitmap Bitmap;

			public ThumbnailInfo(WriteableBitmap b, string normalizedPath, IconSize size) {
				Bitmap = b;
				NormalizedPath = normalizedPath;
				Iconsize = size;
			}
		}

		#region Win32 API / Shell Interop

		[DllImport("gdi32.dll")]
		private static extern bool DeleteObject(IntPtr hObject);

		[StructLayout(LayoutKind.Sequential)]
		private struct SHFILEINFO
		{
			public IntPtr hIcon;
			public IntPtr iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
		};

		private const uint SHGFI_ICON = 0x100;
		private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
		private const uint FileIconFlags = SHGFI_USEFILEATTRIBUTES | SHGFI_ICON;

		private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
		private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
		
		[DllImport("shell32.dll")]
		private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

		/// <summary>
		/// Return large file icon of the specified file.
		/// </summary>
		private static Icon GetLargeFileIcon(NormalizedPath path, bool isDirectory) {
			var sw = Stopwatch.StartNew();
			try {
				// https://www.codeguru.com/cpp/com-tech/shell/article.php/c4511/Tuning-SHGetFileInfo-for-Optimum-Performance.htm
				var shinfo = new SHFILEINFO();
				var attr = isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
				var result = SHGetFileInfo(path, attr, ref shinfo, (uint)Marshal.SizeOf(shinfo), FileIconFlags);
				return Icon.FromHandle(shinfo.hIcon);
			} finally {
				Log.Info($"{nameof(GetLargeFileIcon)} took {sw.ElapsedMilliseconds / 1000.0}s for {path}");
			}
		}

		#endregion

		private static void CopyBitmap(BitmapSource source, WriteableBitmap target, bool dispatcher) {
			var width = source.PixelWidth;
			var height = source.PixelHeight;
			var stride = width * ((source.Format.BitsPerPixel + 7) / 8);

			var bits = new byte[height * stride];
			source.CopyPixels(bits, stride, 0);
			source = null;

			if (dispatcher) {
				target.Dispatcher.BeginInvoke(DispatcherPriority.Background,
					new ThreadStart(delegate {
						//UI Thread
						var delta = target.Height - height;
						var newWidth = width > target.Width ? (int)target.Width : width;
						var newHeight = height > target.Height ? (int)target.Height : height;
						var outRect = new Int32Rect(0, (int)(delta >= 0 ? delta : 0) / 2, newWidth, newWidth);
						try {
							target.WritePixels(outRect, bits, stride, 0);
						} catch (Exception e) {
							System.Diagnostics.Debugger.Break();
						}
					}));
			} else {
				var delta = target.Height - height;
				var newWidth = width > target.Width ? (int)target.Width : width;
				var newHeight = height > target.Height ? (int)target.Height : height;
				var outRect = new Int32Rect(0, (int)(delta >= 0 ? delta : 0) / 2, newWidth, newWidth);
				try {
					target.WritePixels(outRect, bits, stride, 0);
				} catch (Exception e) {
					System.Diagnostics.Debugger.Break();
				}
			}
		}

		private static System.Drawing.Size GetDefaultSize(IconSize size) {
			switch (size) {
				case IconSize.Jumbo: return new System.Drawing.Size(256, 256);
				case IconSize.Thumbnail: return new System.Drawing.Size(256, 256);
				case IconSize.ExtraLarge: return new System.Drawing.Size(48, 48);
				case IconSize.Large: return new System.Drawing.Size(32, 32);
				default: return new System.Drawing.Size(16, 16);
			}

		}

		//http://blog.paranoidferret.com/?p=11 , modified a little.
		private static Bitmap ResizeImage(Bitmap imgToResize, System.Drawing.Size size, int spacing) {
			var sourceWidth = imgToResize.Width;
			var sourceHeight = imgToResize.Height;

			float nPercent = 0;
			float nPercentW = 0;
			float nPercentH = 0;

			nPercentW = (float)size.Width / (float)sourceWidth;
			nPercentH = (float)size.Height / (float)sourceHeight;

			if (nPercentH < nPercentW)
				nPercent = nPercentH;
			else
				nPercent = nPercentW;

			var destWidth = (int)(sourceWidth * nPercent - spacing * 4);
			var destHeight = (int)(sourceHeight * nPercent - spacing * 4);

			var leftOffset = (int)((size.Width - destWidth) / 2);
			var topOffset = (int)((size.Height - destHeight) / 2);


			var b = new Bitmap(size.Width, size.Height);
			var g = Graphics.FromImage((Image)b);
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
			g.DrawLines(Pens.Silver, new PointF[] {
				new PointF(leftOffset - spacing, topOffset + destHeight + spacing), //BottomLeft
				new PointF(leftOffset - spacing, topOffset - spacing), //TopLeft
				new PointF(leftOffset + destWidth + spacing, topOffset - spacing)
			}); //TopRight

			g.DrawLines(Pens.Gray, new PointF[] {
				new PointF(leftOffset + destWidth + spacing, topOffset - spacing), //TopRight
				new PointF(leftOffset + destWidth + spacing, topOffset + destHeight + spacing), //BottomRight
				new PointF(leftOffset - spacing, topOffset + destHeight + spacing),
			}); //BottomLeft

			g.DrawImage(imgToResize, leftOffset, topOffset, destWidth, destHeight);
			g.Dispose();

			return b;
		}

		private static Bitmap ResizeJumbo(Bitmap imgToResize, System.Drawing.Size size, int spacing) {
			var sourceWidth = imgToResize.Width;
			var sourceHeight = imgToResize.Height;

			float nPercent = 0;
			float nPercentW = 0;
			float nPercentH = 0;

			nPercentW = (float)size.Width / (float)sourceWidth;
			nPercentH = (float)size.Height / (float)sourceHeight;

			if (nPercentH < nPercentW)
				nPercent = nPercentH;
			else
				nPercent = nPercentW;

			var destWidth = 80;
			var destHeight = 80;

			var leftOffset = (int)((size.Width - destWidth) / 2);
			var topOffset = (int)((size.Height - destHeight) / 2);


			var b = new Bitmap(size.Width, size.Height);
			var g = Graphics.FromImage((Image)b);
			g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
			g.DrawLines(Pens.Silver, new PointF[] {
				new PointF(0 + spacing, size.Height - spacing), //BottomLeft
				new PointF(0 + spacing, 0 + spacing), //TopLeft
				new PointF(size.Width - spacing, 0 + spacing)
			}); //TopRight

			g.DrawLines(Pens.Gray, new PointF[] {
				new PointF(size.Width - spacing, 0 + spacing), //TopRight
				new PointF(size.Width - spacing, size.Height - spacing), //BottomRight
				new PointF(0 + spacing, size.Height - spacing)
			}); //BottomLeft

			g.DrawImage(imgToResize, leftOffset, topOffset, destWidth, destHeight);
			g.Dispose();

			return b;
		}

		private static BitmapSource LoadBitmap(Bitmap source) {
			// https://stackoverflow.com/a/1546121/4589192
			var sw = Stopwatch.StartNew();
			try {
				var hBitmap = source.GetHbitmap();
				var sw2 = Stopwatch.StartNew();
				try {
					return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
				} finally {
					Log.Info($"{nameof(Imaging.CreateBitmapSourceFromHBitmap)} took {sw2.ElapsedMilliseconds / 1000.0}s");
					var sw3 = Stopwatch.StartNew();
					DeleteObject(hBitmap);
					Log.Info($"{nameof(DeleteObject)} took {sw3.ElapsedMilliseconds / 1000.0}s");
				}
			} finally {
				Log.Info($"{nameof(LoadBitmap)} took {sw.ElapsedMilliseconds / 1000.0}s");
			}
		}

		private static readonly HashSet<string> ImageExtensions = new HashSet<string>(new[] { ".jpg", ".jpeg", ".png", ".gif" });
		
		private static bool IsImage(NormalizedPath path) {
			var ext = path.Extension.ToLower();
			return !string.IsNullOrWhiteSpace(ext) && ImageExtensions.Contains(ext) && File.Exists(path);
		}

		private static readonly HashSet<string> ExecutableExtensions = new HashSet<string>(new[] { ".exe", ".lnk" });

		private static bool IsExecutable(NormalizedPath path) {
			var ext = path.Extension.ToLower();
			return !string.IsNullOrWhiteSpace(ext) && ExecutableExtensions.Contains(ext) && File.Exists(path);
		}

		private static readonly SysImageList ImgList = new SysImageList(SysImageListSize.Jumbo);

		private static Bitmap LoadJumbo(string normalizedPath, bool isDirectory) {
			ImgList.ImageListSize = IsRunningVista.Value ? SysImageListSize.Jumbo : SysImageListSize.ExtraLargeIcons;
			var icon = ImgList.Icon(ImgList.IconIndex(normalizedPath, isDirectory));
			var bitmap = icon.ToBitmap();
			icon.Dispose();

			var empty = System.Drawing.Color.FromArgb(0, 0, 0, 0);

			if (bitmap.Width < 256)
				bitmap = ResizeImage(bitmap, new System.Drawing.Size(256, 256), 0);
			else if (bitmap.GetPixel(100, 100) == empty && bitmap.GetPixel(200, 200) == empty && bitmap.GetPixel(200, 200) == empty) {
				ImgList.ImageListSize = SysImageListSize.LargeIcons;
				bitmap = ResizeJumbo(ImgList.Icon(ImgList.IconIndex(normalizedPath)).ToBitmap(), new System.Drawing.Size(200, 200), 5);
			}

			return bitmap;
		}

		private static readonly Lazy<bool> IsRunningVista = new Lazy<bool>(() => Environment.OSVersion.Version.Major >= 6);

		private static void ImageThumbnailCallback(object state) {
			//Non UIThread
			var input = state as ThumbnailInfo;
			var writeBitmap = input.Bitmap;
			var size = input.Iconsize;

			try {
				var origBitmap = new Bitmap(input.NormalizedPath);
				var inputBitmap = ResizeImage(origBitmap, GetDefaultSize(size), 5);
				var inputBitmapSource = LoadBitmap(inputBitmap);
				origBitmap.Dispose();
				inputBitmap.Dispose();

				CopyBitmap(inputBitmapSource, writeBitmap, true);
			} catch { }
		}

		#endregion

	}
}
 