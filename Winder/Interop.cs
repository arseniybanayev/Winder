using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Winder
{
	public static class WindowsInterop
	{
		#region Hiding Window buttons (close, minimize)

		// from https://stackoverflow.com/a/958980

		private const int GWL_STYLE = -16;
		private const int WS_SYSMENU = 0x80000;

		[DllImport("user32.dll", SetLastError = true)]
		private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll")]
		private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

		internal static void HideWindowButtons(Window window) {
			var hwnd = new WindowInteropHelper(window).Handle;
			SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
		}

		#endregion

		#region Converting icon to image source

		// from https://stackoverflow.com/a/29819585

		/// <summary>Maximal Length of unmanaged Windows-Path-strings</summary>
		private const int MAX_PATH = 260;

		/// <summary>Maximal Length of unmanaged Typename</summary>
		private const int MAX_TYPE = 80;

		private const int FILE_ATTRIBUTE_DIRECTORY = 0x10;
		private const int FILE_ATTRIBUTE_NORMAL = 0x80;

		[Flags]
		private enum SHGFI : int
		{
			/// <summary>get icon</summary>
			Icon = 0x000000100,
			/// <summary>get display name</summary>
			DisplayName = 0x000000200,
			/// <summary>get type name</summary>
			TypeName = 0x000000400,
			/// <summary>get attributes</summary>
			Attributes = 0x000000800,
			/// <summary>get icon location</summary>
			IconLocation = 0x000001000,
			/// <summary>return exe type</summary>
			ExeType = 0x000002000,
			/// <summary>get system icon index</summary>
			SysIconIndex = 0x000004000,
			/// <summary>put a link overlay on icon</summary>
			LinkOverlay = 0x000008000,
			/// <summary>show icon in selected state</summary>
			Selected = 0x000010000,
			/// <summary>get only specified attributes</summary>
			Attr_Specified = 0x000020000,
			/// <summary>get large icon</summary>
			LargeIcon = 0x000000000,
			/// <summary>get small icon</summary>
			SmallIcon = 0x000000001,
			/// <summary>get open icon</summary>
			OpenIcon = 0x000000002,
			/// <summary>get shell size icon</summary>
			ShellIconSize = 0x000000004,
			/// <summary>pszPath is a pidl</summary>
			PIDL = 0x000000008,
			/// <summary>use passed dwFileAttribute</summary>
			UseFileAttributes = 0x000000010,
			/// <summary>apply the appropriate overlays</summary>
			AddOverlays = 0x000000020,
			/// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
			OverlayIndex = 0x000000040,
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct SHFILEINFO
		{
			public SHFILEINFO(bool b) {
				hIcon = IntPtr.Zero;
				iIcon = 0;
				dwAttributes = 0;
				szDisplayName = "";
				szTypeName = "";
			}
			public IntPtr hIcon;
			public int iIcon;
			public uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH)]
			public string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_TYPE)]
			public string szTypeName;
		};

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern int SHGetFileInfo(
		  string pszPath,
		  int dwFileAttributes,
		  out SHFILEINFO psfi,
		  uint cbfileInfo,
		  SHGFI uFlags);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool DestroyIcon(IntPtr hIcon);

		public static ImageSource GetIcon(string strPath, bool isDirectory, bool bSmall) {
			SHFILEINFO info = new SHFILEINFO(true);
			int cbFileInfo = Marshal.SizeOf(info);
			SHGFI flags;
			if (bSmall)
				flags = SHGFI.Icon | SHGFI.SmallIcon | SHGFI.UseFileAttributes;
			else
				flags = SHGFI.Icon | SHGFI.LargeIcon | SHGFI.UseFileAttributes;

			SHGetFileInfo(strPath,
				isDirectory ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL,
				out info, (uint)cbFileInfo, flags);

			IntPtr iconHandle = info.hIcon;
			//if (IntPtr.Zero == iconHandle) // not needed, always return icon (blank)
			//  return DefaultImgSrc;
			var img = Imaging.CreateBitmapSourceFromHIcon(
						iconHandle,
						Int32Rect.Empty,
						BitmapSizeOptions.FromEmptyOptions());
			DestroyIcon(iconHandle);
			return img;
		}

		#endregion
	}
}