using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Winder.App.WindowsUtilities
{
	public static class WindowsInterop
	{
		#region Converting icon to image source

		// from https://stackoverflow.com/a/29819585

		/// <summary>Maximal Length of unmanaged Windows-Path-strings</summary>
		private const int MaxPath = 260;

		/// <summary>Maximal Length of unmanaged Typename</summary>
		private const int MaxType = 80;

		private const int FileAttributeDirectory = 0x10;
		private const int FileAttributeNormal = 0x80;

		[Flags]
		private enum Shgfi : int
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
			AttrSpecified = 0x000020000,

			/// <summary>get large icon</summary>
			LargeIcon = 0x000000000,

			/// <summary>get small icon</summary>
			SmallIcon = 0x000000001,

			/// <summary>get open icon</summary>
			OpenIcon = 0x000000002,

			/// <summary>get shell size icon</summary>
			ShellIconSize = 0x000000004,

			/// <summary>pszPath is a pidl</summary>
			Pidl = 0x000000008,

			/// <summary>use passed dwFileAttribute</summary>
			UseFileAttributes = 0x000000010,

			/// <summary>apply the appropriate overlays</summary>
			AddOverlays = 0x000000020,

			/// <summary>Get the index of the overlay in the upper 8 bits of the iIcon</summary>
			OverlayIndex = 0x000000040,
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct Shfileinfo
		{
			public Shfileinfo(bool b) {
				hIcon = IntPtr.Zero;
				iIcon = 0;
				dwAttributes = 0;
				szDisplayName = "";
				szTypeName = "";
			}

			public readonly IntPtr hIcon;
			public readonly int iIcon;
			public readonly uint dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)] public readonly string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxType)] public readonly string szTypeName;
		};

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern int SHGetFileInfo(
			string pszPath,
			int dwFileAttributes,
			out Shfileinfo psfi,
			uint cbfileInfo,
			Shgfi uFlags);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool DestroyIcon(IntPtr hIcon);

		public static ImageSource GetIcon(string strPath, bool isDirectory, bool bSmall) {
			var info = new Shfileinfo(true);
			var cbFileInfo = Marshal.SizeOf(info);
			Shgfi flags;
			if (bSmall)
				flags = Shgfi.Icon | Shgfi.SmallIcon | Shgfi.UseFileAttributes;
			else
				flags = Shgfi.Icon | Shgfi.LargeIcon | Shgfi.UseFileAttributes;

			SHGetFileInfo(strPath,
				isDirectory ? FileAttributeDirectory : FileAttributeNormal,
				out info, (uint)cbFileInfo, flags);

			var iconHandle = info.hIcon;
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