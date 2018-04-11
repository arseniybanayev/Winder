using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Winder.App.WindowsUtilities
{
	// Adapted from https://www.codeproject.com/Articles/32059/WPF-Filename-To-Icon-Converter
	[ValueConversion(typeof(string), typeof(ImageSource))]
	public class FileToIconConverter : IMultiValueConverter
	{
		private const string ImageFilter = ".jpg,.jpeg,.png,.gif";
		private const string ExeFilter = ".exe,.lnk";

		public int DefaultSize { get; set; }

		public enum IconSize
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
			public readonly WriteableBitmap Bitmap;
			public readonly string FullPath;

			public ThumbnailInfo(WriteableBitmap b, string path, IconSize size) {
				Bitmap = b;
				FullPath = path;
				Iconsize = size;
			}
		}

		#region Win32api

		[DllImport("gdi32.dll")]
		public static extern bool DeleteObject(IntPtr hObject);

		[StructLayout(LayoutKind.Sequential)]
		internal struct Shfileinfo
		{
			public IntPtr hIcon;
			public IntPtr iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
		};

		internal const uint ShgfiIcon = 0x100;
		internal const uint ShgfiTypename = 0x400;
		internal const uint ShgfiLargeicon = 0x0; // 'Large icon
		internal const uint ShgfiSmallicon = 0x1; // 'Small icon
		internal const uint ShgfiSysiconindex = 16384;
		internal const uint ShgfiUsefileattributes = 16;

		/// <summary>
		/// Get Icons that are associated with files.
		/// To use it, use (System.Drawing.Icon myIcon = System.Drawing.Icon.FromHandle(shinfo.hIcon));
		/// hImgSmall = SHGetFileInfo(fName, 0, ref shinfo,(uint)Marshal.SizeOf(shinfo),Win32.SHGFI_ICON |Win32.SHGFI_SMALLICON);
		/// </summary>
		[DllImport("shell32.dll")]
		internal static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
			ref Shfileinfo psfi, uint cbSizeFileInfo, uint uFlags);

		/// <summary>
		/// Return large file icon of the specified file.
		/// </summary>
		internal static Icon GetFileIcon(string fileName, IconSize size) {
			var shinfo = new Shfileinfo();

			var flags = ShgfiSysiconindex;
			if (fileName.IndexOf(":") == -1)
				flags = flags | ShgfiUsefileattributes;
			if (size == IconSize.Small)
				flags = flags | ShgfiIcon | ShgfiSmallicon;
			else flags = flags | ShgfiIcon;

			SHGetFileInfo(fileName, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
			return Icon.FromHandle(shinfo.hIcon);
		}

		#endregion

		#region Static Tools

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
			//Memory Leak fixes, for more info : http://social.msdn.microsoft.com/forums/en-US/wpf/thread/edcf2482-b931-4939-9415-15b3515ddac6/
			try {
				return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero, Int32Rect.Empty,
					BitmapSizeOptions.FromEmptyOptions());
			} finally {
				DeleteObject(hBitmap);
			}

		}

		private static bool IsImage(string fileName) {
			var ext = Path.GetExtension(fileName).ToLower();
			if (ext == "")
				return false;
			return (ImageFilter.IndexOf(ext) != -1 && File.Exists(fileName));
		}

		private static bool IsExecutable(string fileName) {
			var ext = Path.GetExtension(fileName).ToLower();
			if (ext == "")
				return false;
			return (ExeFilter.IndexOf(ext) != -1 && File.Exists(fileName));
		}

		private static bool IsFolder(string path) {
			return path.EndsWith("\\") || Directory.Exists(path);
		}

		private static string ReturnKey(string fileName, IconSize size) {
			var key = Path.GetExtension(fileName).ToLower();

			if (IsExecutable(fileName))
				key = fileName.ToLower();
			if (IsImage(fileName) && size == IconSize.Thumbnail)
				key = fileName.ToLower();
			if (IsFolder(fileName))
				key = fileName.ToLower();

			switch (size) {
				case IconSize.Thumbnail:
					key += IsImage(fileName) ? "+T" : "+J";
					break;
				case IconSize.Jumbo:
					key += "+J";
					break;
				case IconSize.ExtraLarge:
					key += "+XL";
					break;
				case IconSize.Large:
					key += "+L";
					break;
				case IconSize.Small:
					key += "+S";
					break;
			}
			return key;
		}

		#endregion

		#region Static Cache

		private static readonly Dictionary<string, ImageSource> IconDic = new Dictionary<string, ImageSource>();
		private static readonly SysImageList ImgList = new SysImageList(SysImageListSize.Jumbo);

		private static Bitmap LoadJumbo(string lookup) {
			ImgList.ImageListSize = IsVistaUp() ? SysImageListSize.Jumbo : SysImageListSize.ExtraLargeIcons;
			var icon = ImgList.Icon(ImgList.IconIndex(lookup, IsFolder(lookup)));
			var bitmap = icon.ToBitmap();
			icon.Dispose();

			var empty = System.Drawing.Color.FromArgb(0, 0, 0, 0);

			if (bitmap.Width < 256)
				bitmap = ResizeImage(bitmap, new System.Drawing.Size(256, 256), 0);
			else if (bitmap.GetPixel(100, 100) == empty && bitmap.GetPixel(200, 200) == empty && bitmap.GetPixel(200, 200) == empty) {
				ImgList.ImageListSize = SysImageListSize.LargeIcons;
				bitmap = ResizeJumbo(ImgList.Icon(ImgList.IconIndex(lookup)).ToBitmap(), new System.Drawing.Size(200, 200), 5);
			}

			return bitmap;
		}

		#endregion

		#region Instance Cache

		private static readonly Dictionary<string, ImageSource> ThumbDic = new Dictionary<string, ImageSource>();

		public void ClearInstanceCache() {
			ThumbDic.Clear();
			//System.GC.Collect();
		}

		private void PollIconCallback(object state) {
			var input = state as ThumbnailInfo;
			var fileName = input.FullPath;
			var writeBitmap = input.Bitmap;
			var size = input.Iconsize;

			var origBitmap = GetFileIcon(fileName, size).ToBitmap();
			var inputBitmap = origBitmap;
			if (size == IconSize.Jumbo || size == IconSize.Thumbnail)
				inputBitmap = ResizeJumbo(origBitmap, GetDefaultSize(size), 5);
			else inputBitmap = ResizeImage(origBitmap, GetDefaultSize(size), 0);

			var inputBitmapSource = LoadBitmap(inputBitmap);
			origBitmap.Dispose();
			inputBitmap.Dispose();

			CopyBitmap(inputBitmapSource, writeBitmap, true);
		}

		private void PollThumbnailCallback(object state) {
			//Non UIThread
			var input = state as ThumbnailInfo;
			var fileName = input.FullPath;
			var writeBitmap = input.Bitmap;
			var size = input.Iconsize;

			try {
				var origBitmap = new Bitmap(fileName);
				var inputBitmap = ResizeImage(origBitmap, GetDefaultSize(size), 5);
				var inputBitmapSource = LoadBitmap(inputBitmap);
				origBitmap.Dispose();
				inputBitmap.Dispose();

				CopyBitmap(inputBitmapSource, writeBitmap, true);
			} catch { }

		}

		private ImageSource AddToDic(string fileName, IconSize size) {
			var key = ReturnKey(fileName, size);

			if (size == IconSize.Thumbnail || IsExecutable(fileName)) {
				if (!ThumbDic.ContainsKey(key))
					lock(ThumbDic)
						ThumbDic.Add(key, GetImage(fileName, size));

				return ThumbDic[key];
			}
			if (!IconDic.ContainsKey(key))
				lock(IconDic)
					IconDic.Add(key, GetImage(fileName, size));
			return IconDic[key];
		}

		public ImageSource GetImage(string fileName, int iconSize) {
			IconSize size;

			if (iconSize <= 16) size = IconSize.Small;
			else if (iconSize <= 32) size = IconSize.Large;
			else if (iconSize <= 48) size = IconSize.ExtraLarge;
			else if (iconSize <= 72) size = IconSize.Jumbo;
			else size = IconSize.Thumbnail;

			return AddToDic(fileName, size);
		}

		#endregion

		#region Instance Tools

		public static bool IsVistaUp() {
			return (Environment.OSVersion.Version.Major >= 6);
		}

		private BitmapSource GetImage(string fileName, IconSize size) {
			Icon icon;
			var key = ReturnKey(fileName, size);
			var lookup = "aaa" + Path.GetExtension(fileName).ToLower();
			if (!key.StartsWith("."))
				lookup = fileName;

			if (IsExecutable(fileName)) {

				var bitmap = new WriteableBitmap(AddToDic("aaa.exe", size) as BitmapSource);
				ThreadPool.QueueUserWorkItem(new WaitCallback(PollIconCallback), new ThumbnailInfo(bitmap, fileName, size));
				return bitmap;
			}
			switch (size) {
				case IconSize.Thumbnail:
					if (IsImage(fileName)) {
						//Load as jumbo icon first.                         
						var bitmap = new WriteableBitmap(AddToDic(fileName, IconSize.Jumbo) as BitmapSource);
						//BitmapSource bitmapSource = addToDic(fileName, IconSize.jumbo) as BitmapSource;                            
						//WriteableBitmap bitmap = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Bgra32, null);
						//copyBitmap(bitmapSource, bitmap, false);
						ThreadPool.QueueUserWorkItem(new WaitCallback(PollThumbnailCallback), new ThumbnailInfo(bitmap, fileName, size));
						return bitmap;
					} else {
						return GetImage(lookup, IconSize.Jumbo);
					}
				case IconSize.Jumbo:
					return LoadBitmap(LoadJumbo(lookup));
				case IconSize.ExtraLarge:
					ImgList.ImageListSize = SysImageListSize.ExtraLargeIcons;
					icon = ImgList.Icon(ImgList.IconIndex(lookup, IsFolder(fileName)));
					return LoadBitmap(icon.ToBitmap());
				default:
					icon = GetFileIcon(lookup, size);
					return LoadBitmap(icon.ToBitmap());
			}
		}

		#endregion

		public FileToIconConverter() {
			DefaultSize = 48;
		}

		#region IMultiValueConverter Members

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			var size = DefaultSize;
			if (values.Length > 1 && values[1] is double)
				size = (int)(float)(double)values[1];

			return GetImage(values[0] as string ?? "", size);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

		#endregion

	}
}