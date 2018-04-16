using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
	public static class FileSystemImages
	{
		/// <summary>
		/// Returns an image containing a thumbnail (larger than icon) for the specified file/directory.
		/// </summary>
		public static ImageSource GetThumbnail(string path, bool isDirectory) {
			path = FileUtil.NormalizePath(path);

			// Executables and directories can have individual icons
			// Images are shown as full image previews, which are individual
			if (isDirectory || IsExecutable(path) || IsImage(path))
				return ThumbnailCacheByNormalizedPath.GetOrAdd(path, p => GetThumbnailUnsafe(p, isDirectory));
			
			// Every other type of file can be cached by extension
			return ThumbnailCacheByExtension.GetOrAdd(Path.GetExtension(path).ToLower(), _ => GetThumbnailUnsafe(path, false));
		}

		private static ImageSource GetThumbnailUnsafe(string normalizedPath, bool isDirectory) {
			try {
				// At this size, images are shown as full image previews
				if (!isDirectory && IsImage(normalizedPath)) {
					var bitmap = new WriteableBitmap(LoadBitmap(LoadJumbo(normalizedPath, false)));
					ThreadPool.QueueUserWorkItem(ImageThumbnailCallback, new ThumbnailInfo(bitmap, normalizedPath, IconSize.Thumbnail));
					return bitmap;
				}

				return LoadBitmap(LoadJumbo(normalizedPath, isDirectory));
			} catch (Exception e) {
				Log.Error($"Failed to get thumbnail for {normalizedPath}", e);
				return null;
			}
		}

		private static readonly ConcurrentDictionary<string, ImageSource> ThumbnailCacheByExtension = new ConcurrentDictionary<string, ImageSource>();
		private static readonly ConcurrentDictionary<string, ImageSource> ThumbnailCacheByNormalizedPath = new ConcurrentDictionary<string, ImageSource>();

		/// <summary>
		/// Returns an image containing an icon (smaller than thumbnail) for the specified file/directory.
		/// </summary>
		public static ImageSource GetIcon(string path, bool isDirectory) {
			path = FileUtil.NormalizePath(path);

			// Executables and directories can have individual icons
			if (isDirectory || IsExecutable(path))
				return IconCacheByFileSystemInfo.GetOrAdd(path, p => GetIconUnsafe(p, isDirectory));
			
			// Every other type of file can be cached by extension
			return IconCacheByExtension.GetOrAdd(Path.GetExtension(path).ToLower(), _ => GetIconUnsafe(path, false));
		}

		private static ImageSource GetIconUnsafe(string normalizedPath, bool isDirectory) {
			try {
				return LoadBitmap(GetLargeFileIcon(normalizedPath).ToBitmap());
			} catch (Exception e) {
				Log.Error($"Failed to get icon for {normalizedPath}", e);
				return null;
			}
		}

		private static readonly ConcurrentDictionary<string, ImageSource> IconCacheByExtension = new ConcurrentDictionary<string, ImageSource>();
		private static readonly ConcurrentDictionary<string, ImageSource> IconCacheByFileSystemInfo = new ConcurrentDictionary<string, ImageSource>();
		
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

		#region Win32 API

		[DllImport("gdi32.dll")]
		private static extern bool DeleteObject(IntPtr hObject);

		[StructLayout(LayoutKind.Sequential)]
		private struct Shfileinfo
		{
			public IntPtr hIcon;
			public IntPtr iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
		};

		private static uint ShgfiIcon = 0x100;
		private static uint ShgfiSysiconindex = 16384;
		private static uint ShgfiUsefileattributes = 16;

		/// <summary>
		/// Get Icons that are associated with files.
		/// To use it, use (System.Drawing.Icon myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon));
		/// hImgSmall = SHGetFileInfo(fName, 0, ref shinfo,(uint)Marshal.SizeOf(shinfo),Win32.SHGFI_ICON |Win32.SHGFI_SMALLICON);
		/// </summary>
		[DllImport("shell32.dll")]
		private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
			ref Shfileinfo psfi, uint cbSizeFileInfo, uint uFlags);

		/// <summary>
		/// Return large file icon of the specified file.
		/// </summary>
		private static Icon GetLargeFileIcon(string normalizedPath) {
			var shinfo = new Shfileinfo();

			var flags = ShgfiSysiconindex;
			if (normalizedPath.IndexOf(":") == -1)
				flags = flags | ShgfiUsefileattributes;
			else flags = flags | ShgfiIcon;

			var result = SHGetFileInfo(normalizedPath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
			return Icon.FromHandle(shinfo.hIcon);
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

			nPercentW = ((float)size.Width / (float)sourceWidth);
			nPercentH = ((float)size.Height / (float)sourceHeight);

			if (nPercentH < nPercentW)
				nPercent = nPercentH;
			else
				nPercent = nPercentW;

			var destWidth = (int)((sourceWidth * nPercent) - spacing * 4);
			var destHeight = (int)((sourceHeight * nPercent) - spacing * 4);

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

			nPercentW = ((float)size.Width / (float)sourceWidth);
			nPercentH = ((float)size.Height / (float)sourceHeight);

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
			var hBitmap = source.GetHbitmap();
			// http://social.msdn.microsoft.com/forums/en-US/wpf/thread/edcf2482-b931-4939-9415-15b3515ddac6/
			try {
				return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			} finally {
				DeleteObject(hBitmap);
			}
		}

		private static readonly HashSet<string> ImageExtensions = new HashSet<string>(new[] { ".jpg", ".jpeg", ".png", ".gif" });
		
		private static bool IsImage(string normalizedPath) {
			var ext = Path.GetExtension(normalizedPath).ToLower();
			return !string.IsNullOrWhiteSpace(ext) && ImageExtensions.Contains(ext) && File.Exists(normalizedPath);
		}

		private static readonly HashSet<string> ExecutableExtensions = new HashSet<string>(new[] { ".exe", ".lnk" });

		private static bool IsExecutable(string normalizedPath) {
			var ext = Path.GetExtension(normalizedPath).ToLower();
			return !string.IsNullOrWhiteSpace(ext) && ExecutableExtensions.Contains(ext) && File.Exists(normalizedPath);
		}

		private static readonly SysImageList ImgList = new SysImageList(SysImageListSize.Jumbo);

		private static Bitmap LoadJumbo(string normalizedPath, bool isDirectory) {
			ImgList.ImageListSize = IsVistaUp() ? SysImageListSize.Jumbo : SysImageListSize.ExtraLargeIcons;
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

		public static bool IsVistaUp() => Environment.OSVersion.Version.Major >= 6;
	}
}
 