using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Winder.App.WindowsUtilities
{

	#region Public Enumerations

	/// <summary>
	/// Available system image list sizes
	/// </summary>
	public enum SysImageListSize : int
	{
		/// <summary>
		/// System Large Icon Size (typically 32x32)
		/// </summary>
		LargeIcons = 0x0,

		/// <summary>
		/// System Small Icon Size (typically 16x16)
		/// </summary>
		SmallIcons = 0x1,

		/// <summary>
		/// System Extra Large Icon Size (typically 48x48).
		/// Only available under XP; under other OS the
		/// Large Icon ImageList is returned.
		/// </summary>
		ExtraLargeIcons = 0x2,
		Jumbo = 0x4
	}

	/// <summary>
	/// Flags controlling how the Image List item is 
	/// drawn
	/// </summary>
	[Flags]
	public enum ImageListDrawItemConstants : int
	{
		/// <summary>
		/// Draw item normally.
		/// </summary>
		IldNormal = 0x0,

		/// <summary>
		/// Draw item transparently.
		/// </summary>
		IldTransparent = 0x1,

		/// <summary>
		/// Draw item blended with 25% of the specified foreground colour
		/// or the Highlight colour if no foreground colour specified.
		/// </summary>
		IldBlend25 = 0x2,

		/// <summary>
		/// Draw item blended with 50% of the specified foreground colour
		/// or the Highlight colour if no foreground colour specified.
		/// </summary>
		IldSelected = 0x4,

		/// <summary>
		/// Draw the icon's mask
		/// </summary>
		IldMask = 0x10,

		/// <summary>
		/// Draw the icon image without using the mask
		/// </summary>
		IldImage = 0x20,

		/// <summary>
		/// Draw the icon using the ROP specified.
		/// </summary>
		IldRop = 0x40,

		/// <summary>
		/// Preserves the alpha channel in dest. XP only.
		/// </summary>
		IldPreservealpha = 0x1000,

		/// <summary>
		/// Scale the image to cx, cy instead of clipping it.  XP only.
		/// </summary>
		IldScale = 0x2000,

		/// <summary>
		/// Scale the image to the current DPI of the display. XP only.
		/// </summary>
		IldDpiscale = 0x4000
	}

	/// <summary>
	/// Enumeration containing XP ImageList Draw State options
	/// </summary>
	[Flags]
	public enum ImageListDrawStateConstants : int
	{
		/// <summary>
		/// The image state is not modified. 
		/// </summary>
		IlsNormal = (0x00000000),

		/// <summary>
		/// Adds a glow effect to the icon, which causes the icon to appear to glow 
		/// with a given color around the edges. (Note: does not appear to be
		/// implemented)
		/// </summary>
		IlsGlow = (0x00000001), //The color for the glow effect is passed to the IImageList::Draw method in the crEffect member of IMAGELISTDRAWPARAMS. 

		/// <summary>
		/// Adds a drop shadow effect to the icon. (Note: does not appear to be
		/// implemented)
		/// </summary>
		IlsShadow = (0x00000002), //The color for the drop shadow effect is passed to the IImageList::Draw method in the crEffect member of IMAGELISTDRAWPARAMS. 

		/// <summary>
		/// Saturates the icon by increasing each color component 
		/// of the RGB triplet for each pixel in the icon. (Note: only ever appears
		/// to result in a completely unsaturated icon)
		/// </summary>
		IlsSaturate = (0x00000004), // The amount to increase is indicated by the frame member in the IMAGELISTDRAWPARAMS method. 

		/// <summary>
		/// Alpha blends the icon. Alpha blending controls the transparency 
		/// level of an icon, according to the value of its alpha channel. 
		/// (Note: does not appear to be implemented).
		/// </summary>
		IlsAlpha = (0x00000008) //The value of the alpha channel is indicated by the frame member in the IMAGELISTDRAWPARAMS method. The alpha channel can be from 0 to 255, with 0 being completely transparent, and 255 being completely opaque. 
	}

	/// <summary>
	/// Flags specifying the state of the icon to draw from the Shell
	/// </summary>
	[Flags]
	public enum ShellIconStateConstants
	{
		/// <summary>
		/// Get icon in normal state
		/// </summary>
		ShellIconStateNormal = 0,

		/// <summary>
		/// Put a link overlay on icon 
		/// </summary>
		ShellIconStateLinkOverlay = 0x8000,

		/// <summary>
		/// show icon in selected state 
		/// </summary>
		ShellIconStateSelected = 0x10000,

		/// <summary>
		/// get open icon 
		/// </summary>
		ShellIconStateOpen = 0x2,

		/// <summary>
		/// apply the appropriate overlays
		/// </summary>
		ShellIconAddOverlays = 0x000000020,
	}

	#endregion

	#region SysImageList

	/// <summary>
	/// Summary description for SysImageList.
	/// </summary>
	public class SysImageList : IDisposable
	{
		#region UnmanagedCode

		private const int MaxPath = 260;

		[DllImport("shell32")]
		private static extern IntPtr SHGetFileInfo(
			string pszPath,
			int dwFileAttributes,
			ref Shfileinfo psfi,
			uint cbFileInfo,
			uint uFlags);

		[DllImport("user32.dll")]
		private static extern int DestroyIcon(IntPtr hIcon);

		private const int FileAttributeNormal = 0x80;
		private const int FileAttributeDirectory = 0x10;

		private const int FormatMessageAllocateBuffer = 0x100;
		private const int FormatMessageArgumentArray = 0x2000;
		private const int FormatMessageFromHmodule = 0x800;
		private const int FormatMessageFromString = 0x400;
		private const int FormatMessageFromSystem = 0x1000;
		private const int FormatMessageIgnoreInserts = 0x200;
		private const int FormatMessageMaxWidthMask = 0xFF;

		[DllImport("kernel32")]
		private extern static int FormatMessage(
			int dwFlags,
			IntPtr lpSource,
			int dwMessageId,
			int dwLanguageId,
			string lpBuffer,
			uint nSize,
			int argumentsLong);

		[DllImport("kernel32")]
		private extern static int GetLastError();

		[DllImport("comctl32")]
		private extern static int ImageList_Draw(
			IntPtr hIml,
			int i,
			IntPtr hdcDst,
			int x,
			int y,
			int fStyle);

		[DllImport("comctl32")]
		private extern static int ImageList_DrawIndirect(
			ref Imagelistdrawparams pimldp);

		[DllImport("comctl32")]
		private extern static int ImageList_GetIconSize(
			IntPtr himl,
			ref int cx,
			ref int cy);

		[DllImport("comctl32")]
		private extern static IntPtr ImageList_GetIcon(
			IntPtr himl,
			int i,
			int flags);

		/// <summary>
		/// SHGetImageList is not exported correctly in XP.  See KB316931
		/// http://support.microsoft.com/default.aspx?scid=kb;EN-US;Q316931
		/// Apparently (and hopefully) ordinal 727 isn't going to change.
		/// </summary>
		[DllImport("shell32.dll", EntryPoint = "#727")]
		private extern static int SHGetImageList(
			int iImageList,
			ref Guid riid,
			ref IImageList ppv
		);

		[DllImport("shell32.dll", EntryPoint = "#727")]
		private extern static int SHGetImageListHandle(
			int iImageList,
			ref Guid riid,
			ref IntPtr handle
		);

		#endregion

		#region Private Enumerations

		[Flags]
		private enum ShGetFileInfoConstants : int
		{
			ShgfiIcon = 0x100, // get icon 
			ShgfiDisplayname = 0x200, // get display name 
			ShgfiTypename = 0x400, // get type name 
			ShgfiAttributes = 0x800, // get attributes 
			ShgfiIconlocation = 0x1000, // get icon location 
			ShgfiExetype = 0x2000, // return exe type 
			ShgfiSysiconindex = 0x4000, // get system icon index 
			ShgfiLinkoverlay = 0x8000, // put a link overlay on icon 
			ShgfiSelected = 0x10000, // show icon in selected state 
			ShgfiAttrSpecified = 0x20000, // get only specified attributes 
			ShgfiLargeicon = 0x0, // get large icon 
			ShgfiSmallicon = 0x1, // get small icon 
			ShgfiOpenicon = 0x2, // get open icon 
			ShgfiShelliconsize = 0x4, // get shell size icon 

			//SHGFI_PIDL = 0x8,                  // pszPath is a pidl 
			ShgfiUsefileattributes = 0x10, // use passed dwFileAttribute 
			ShgfiAddoverlays = 0x000000020, // apply the appropriate overlays
			ShgfiOverlayindex = 0x000000040 // Get the index of the overlay
		}

		#endregion

		#region Private ImageList structures

		[StructLayout(LayoutKind.Sequential)]
		private struct Rect
		{
			readonly int left;
			readonly int top;
			readonly int right;
			readonly int bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Point
		{
			readonly int x;
			readonly int y;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Imagelistdrawparams
		{
			public int cbSize;
			public IntPtr himl;
			public int i;
			public IntPtr hdcDst;
			public int x;
			public int y;
			public int cx;
			public int cy;
			public readonly int xBitmap; // x offest from the upperleft of bitmap
			public readonly int yBitmap; // y offset from the upperleft of bitmap
			public readonly int rgbBk;
			public int rgbFg;
			public int fStyle;
			public readonly int dwRop;
			public int fState;
			public int Frame;
			public int crEffect;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Imageinfo
		{
			public readonly IntPtr hbmImage;
			public readonly IntPtr hbmMask;
			public readonly int Unused1;
			public readonly int Unused2;
			public readonly Rect rcImage;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Shfileinfo
		{
			public readonly IntPtr hIcon;
			public readonly int iIcon;
			public readonly int dwAttributes;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxPath)] public readonly string szDisplayName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public readonly string szTypeName;
		}

		#endregion

		#region Private ImageList COM Interop (XP)

		[ComImport()]
		[Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
		[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		//helpstring("Image List"),
		interface IImageList
		{
			[PreserveSig]
			int Add(
				IntPtr hbmImage,
				IntPtr hbmMask,
				ref int pi);

			[PreserveSig]
			int ReplaceIcon(
				int i,
				IntPtr hicon,
				ref int pi);

			[PreserveSig]
			int SetOverlayImage(
				int iImage,
				int iOverlay);

			[PreserveSig]
			int Replace(
				int i,
				IntPtr hbmImage,
				IntPtr hbmMask);

			[PreserveSig]
			int AddMasked(
				IntPtr hbmImage,
				int crMask,
				ref int pi);

			[PreserveSig]
			int Draw(
				ref Imagelistdrawparams pimldp);

			[PreserveSig]
			int Remove(
				int i);

			[PreserveSig]
			int GetIcon(
				int i,
				int flags,
				ref IntPtr picon);

			[PreserveSig]
			int GetImageInfo(
				int i,
				ref Imageinfo pImageInfo);

			[PreserveSig]
			int Copy(
				int iDst,
				IImageList punkSrc,
				int iSrc,
				int uFlags);

			[PreserveSig]
			int Merge(
				int i1,
				IImageList punk2,
				int i2,
				int dx,
				int dy,
				ref Guid riid,
				ref IntPtr ppv);

			[PreserveSig]
			int Clone(
				ref Guid riid,
				ref IntPtr ppv);

			[PreserveSig]
			int GetImageRect(
				int i,
				ref Rect prc);

			[PreserveSig]
			int GetIconSize(
				ref int cx,
				ref int cy);

			[PreserveSig]
			int SetIconSize(
				int cx,
				int cy);

			[PreserveSig]
			int GetImageCount(
				ref int pi);

			[PreserveSig]
			int SetImageCount(
				int uNewCount);

			[PreserveSig]
			int SetBkColor(
				int clrBk,
				ref int pclr);

			[PreserveSig]
			int GetBkColor(
				ref int pclr);

			[PreserveSig]
			int BeginDrag(
				int iTrack,
				int dxHotspot,
				int dyHotspot);

			[PreserveSig]
			int EndDrag();

			[PreserveSig]
			int DragEnter(
				IntPtr hwndLock,
				int x,
				int y);

			[PreserveSig]
			int DragLeave(
				IntPtr hwndLock);

			[PreserveSig]
			int DragMove(
				int x,
				int y);

			[PreserveSig]
			int SetDragCursorImage(
				ref IImageList punk,
				int iDrag,
				int dxHotspot,
				int dyHotspot);

			[PreserveSig]
			int DragShowNolock(
				int fShow);

			[PreserveSig]
			int GetDragImage(
				ref Point ppt,
				ref Point pptHotspot,
				ref Guid riid,
				ref IntPtr ppv);

			[PreserveSig]
			int GetItemFlags(
				int i,
				ref int dwFlags);

			[PreserveSig]
			int GetOverlayImage(
				int iOverlay,
				ref int piIndex);
		};

		#endregion

		#region Member Variables

		private IntPtr _hIml = IntPtr.Zero;
		private IImageList _iImageList = null;
		private SysImageListSize _size = SysImageListSize.SmallIcons;
		private bool _disposed = false;

		#endregion

		#region Implementation

		#region Properties

		/// <summary>
		/// Gets the hImageList handle
		/// </summary>
		public IntPtr Handle {
			get { return _hIml; }
		}

		/// <summary>
		/// Gets/sets the size of System Image List to retrieve.
		/// </summary>
		public SysImageListSize ImageListSize {
			get { return _size; }
			set {
				_size = value;
				Create();
			}

		}

		/// <summary>
		/// Returns the size of the Image List FileSystemImages.
		/// </summary>
		public Size Size {
			get {
				var cx = 0;
				var cy = 0;
				if (_iImageList == null) {
					ImageList_GetIconSize(
						_hIml,
						ref cx,
						ref cy);
				} else {
					_iImageList.GetIconSize(ref cx, ref cy);
				}
				var sz = new Size(
					cx, cy);
				return sz;
			}
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns a GDI+ copy of the icon from the ImageList
		/// at the specified index.
		/// </summary>
		/// <param name="index">The index to get the icon for</param>
		/// <returns>The specified icon</returns>
		public Icon Icon(int index) {
			Icon icon = null;

			var hIcon = IntPtr.Zero;
			if (_iImageList == null) {
				hIcon = ImageList_GetIcon(
					_hIml,
					index,
					(int)ImageListDrawItemConstants.IldTransparent);

			} else {
				_iImageList.GetIcon(
					index,
					(int)ImageListDrawItemConstants.IldTransparent,
					ref hIcon);
			}

			if (hIcon != IntPtr.Zero) {
				icon = System.Drawing.Icon.FromHandle(hIcon);
			}
			return icon;
		}

		/// <summary>
		/// Return the index of the icon for the specified file, always using 
		/// the cached version where possible.
		/// </summary>
		/// <param name="fileName">Filename to get icon for</param>
		/// <returns>Index of the icon</returns>
		public int IconIndex(string fileName) {
			return IconIndex(fileName, false);
		}

		/// <summary>
		/// Returns the index of the icon for the specified file
		/// </summary>
		/// <param name="fileName">Filename to get icon for</param>
		/// <param name="forceLoadFromDisk">If True, then hit the disk to get the icon,
		/// otherwise only hit the disk if no cached icon is available.</param>
		/// <returns>Index of the icon</returns>
		public int IconIndex(
			string fileName,
			bool forceLoadFromDisk) {
			return IconIndex(
				fileName,
				forceLoadFromDisk,
				ShellIconStateConstants.ShellIconStateNormal);
		}

		/// <summary>
		/// Returns the index of the icon for the specified file
		/// </summary>
		/// <param name="fileName">Filename to get icon for</param>
		/// <param name="forceLoadFromDisk">If True, then hit the disk to get the icon,
		/// otherwise only hit the disk if no cached icon is available.</param>
		/// <param name="iconState">Flags specifying the state of the icon
		/// returned.</param>
		/// <returns>Index of the icon</returns>
		public int IconIndex(
			string fileName,
			bool forceLoadFromDisk,
			ShellIconStateConstants iconState
		) {
			var dwFlags = ShGetFileInfoConstants.ShgfiSysiconindex;
			var dwAttr = 0;
			if (_size == SysImageListSize.SmallIcons) {
				dwFlags |= ShGetFileInfoConstants.ShgfiSmallicon;
			}

			// We can choose whether to access the disk or not. If you don't
			// hit the disk, you may get the wrong icon if the icon is
			// not cached. Also only works for files.
			if (!forceLoadFromDisk) {
				dwFlags |= ShGetFileInfoConstants.ShgfiUsefileattributes;
				dwAttr = FileAttributeNormal;
			} else {
				dwAttr = 0;
			}

			// sFileSpec can be any file. You can specify a
			// file that does not exist and still get the
			// icon, for example sFileSpec = "C:\PANTS.DOC"
			var shfi = new Shfileinfo();
			var shfiSize = (uint)Marshal.SizeOf(shfi.GetType());
			var retVal = SHGetFileInfo(
				fileName, dwAttr, ref shfi, shfiSize,
				((uint)(dwFlags) | (uint)iconState));

			if (retVal.Equals(IntPtr.Zero)) {
				System.Diagnostics.Debug.Assert((!retVal.Equals(IntPtr.Zero)), "Failed to get icon index");
				return 0;
			}
			return shfi.iIcon;
		}

		/// <summary>
		/// Draws an image
		/// </summary>
		/// <param name="hdc">Device context to draw to</param>
		/// <param name="index">Index of image to draw</param>
		/// <param name="x">X Position to draw at</param>
		/// <param name="y">Y Position to draw at</param>
		public void DrawImage(
			IntPtr hdc,
			int index,
			int x,
			int y
		) {
			DrawImage(hdc, index, x, y, ImageListDrawItemConstants.IldTransparent);
		}

		/// <summary>
		/// Draws an image using the specified flags
		/// </summary>
		/// <param name="hdc">Device context to draw to</param>
		/// <param name="index">Index of image to draw</param>
		/// <param name="x">X Position to draw at</param>
		/// <param name="y">Y Position to draw at</param>
		/// <param name="flags">Drawing flags</param>
		public void DrawImage(
			IntPtr hdc,
			int index,
			int x,
			int y,
			ImageListDrawItemConstants flags
		) {
			if (_iImageList == null) {
				var ret = ImageList_Draw(
					_hIml,
					index,
					hdc,
					x,
					y,
					(int)flags);
			} else {
				var pimldp = new Imagelistdrawparams();
				pimldp.hdcDst = hdc;
				pimldp.cbSize = Marshal.SizeOf(pimldp.GetType());
				pimldp.i = index;
				pimldp.x = x;
				pimldp.y = y;
				pimldp.rgbFg = -1;
				pimldp.fStyle = (int)flags;
				_iImageList.Draw(ref pimldp);
			}

		}

		/// <summary>
		/// Draws an image using the specified flags and specifies
		/// the size to clip to (or to stretch to if ILD_SCALE
		/// is provided).
		/// </summary>
		/// <param name="hdc">Device context to draw to</param>
		/// <param name="index">Index of image to draw</param>
		/// <param name="x">X Position to draw at</param>
		/// <param name="y">Y Position to draw at</param>
		/// <param name="flags">Drawing flags</param>
		/// <param name="cx">Width to draw</param>
		/// <param name="cy">Height to draw</param>
		public void DrawImage(
			IntPtr hdc,
			int index,
			int x,
			int y,
			ImageListDrawItemConstants flags,
			int cx,
			int cy
		) {
			var pimldp = new Imagelistdrawparams();
			pimldp.hdcDst = hdc;
			pimldp.cbSize = Marshal.SizeOf(pimldp.GetType());
			pimldp.i = index;
			pimldp.x = x;
			pimldp.y = y;
			pimldp.cx = cx;
			pimldp.cy = cy;
			pimldp.fStyle = (int)flags;
			if (_iImageList == null) {
				pimldp.himl = _hIml;
				var ret = ImageList_DrawIndirect(ref pimldp);
			} else {

				_iImageList.Draw(ref pimldp);
			}
		}

		/// <summary>
		/// Draws an image using the specified flags and state on XP systems.
		/// </summary>
		/// <param name="hdc">Device context to draw to</param>
		/// <param name="index">Index of image to draw</param>
		/// <param name="x">X Position to draw at</param>
		/// <param name="y">Y Position to draw at</param>
		/// <param name="flags">Drawing flags</param>
		/// <param name="cx">Width to draw</param>
		/// <param name="cy">Height to draw</param>
		/// <param name="foreColor">Fore colour to blend with when using the 
		/// ILD_SELECTED or ILD_BLEND25 flags</param>
		/// <param name="stateFlags">State flags</param>
		/// <param name="glowOrShadowColor">If stateFlags include ILS_GLOW, then
		/// the colour to use for the glow effect.  Otherwise if stateFlags includes 
		/// ILS_SHADOW, then the colour to use for the shadow.</param>
		/// <param name="saturateColorOrAlpha">If stateFlags includes ILS_ALPHA,
		/// then the alpha component is applied to the icon. Otherwise if 
		/// ILS_SATURATE is included, then the (R,G,B) components are used
		/// to saturate the image.</param>
		public void DrawImage(
			IntPtr hdc,
			int index,
			int x,
			int y,
			ImageListDrawItemConstants flags,
			int cx,
			int cy,
			Color foreColor,
			ImageListDrawStateConstants stateFlags,
			Color saturateColorOrAlpha,
			Color glowOrShadowColor
		) {
			var pimldp = new Imagelistdrawparams();
			pimldp.hdcDst = hdc;
			pimldp.cbSize = Marshal.SizeOf(pimldp.GetType());
			pimldp.i = index;
			pimldp.x = x;
			pimldp.y = y;
			pimldp.cx = cx;
			pimldp.cy = cy;
			pimldp.rgbFg = Color.FromArgb(0,
				foreColor.R, foreColor.G, foreColor.B).ToArgb();
			Console.WriteLine("{0}", pimldp.rgbFg);
			pimldp.fStyle = (int)flags;
			pimldp.fState = (int)stateFlags;
			if ((stateFlags & ImageListDrawStateConstants.IlsAlpha) ==
			    ImageListDrawStateConstants.IlsAlpha) {
				// Set the alpha:
				pimldp.Frame = (int)saturateColorOrAlpha.A;
			} else if ((stateFlags & ImageListDrawStateConstants.IlsSaturate) ==
			           ImageListDrawStateConstants.IlsSaturate) {
				// discard alpha channel:
				saturateColorOrAlpha = Color.FromArgb(0,
					saturateColorOrAlpha.R,
					saturateColorOrAlpha.G,
					saturateColorOrAlpha.B);
				// set the saturate color
				pimldp.Frame = saturateColorOrAlpha.ToArgb();
			}
			glowOrShadowColor = Color.FromArgb(0,
				glowOrShadowColor.R,
				glowOrShadowColor.G,
				glowOrShadowColor.B);
			pimldp.crEffect = glowOrShadowColor.ToArgb();
			if (_iImageList == null) {
				pimldp.himl = _hIml;
				var ret = ImageList_DrawIndirect(ref pimldp);
			} else {

				_iImageList.Draw(ref pimldp);
			}
		}

		/// <summary>
		/// Determines if the system is running Windows XP
		/// or above
		/// </summary>
		/// <returns>True if system is running XP or above, False otherwise</returns>
		private bool IsXpOrAbove() {
			var ret = false;
			if (Environment.OSVersion.Version.Major > 5) {
				ret = true;
			} else if ((Environment.OSVersion.Version.Major == 5) &&
			           (Environment.OSVersion.Version.Minor >= 1)) {
				ret = true;
			}
			return ret;
			//return false;
		}

		/// <summary>
		/// Creates the SystemImageList
		/// </summary>
		private void Create() {
			// forget last image list if any:
			_hIml = IntPtr.Zero;

			if (IsXpOrAbove()) {
				// Get the System IImageList object from the Shell:
				var iidImageList = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
				var ret = SHGetImageList(
					(int)_size,
					ref iidImageList,
					ref _iImageList
				);
				// the image list handle is the IUnknown pointer, but 
				// using Marshal.GetIUnknownForObject doesn't return
				// the right value.  It really doesn't hurt to make
				// a second call to get the handle:
				SHGetImageListHandle((int)_size, ref iidImageList, ref _hIml);
			} else {
				// Prepare flags:
				var dwFlags = ShGetFileInfoConstants.ShgfiUsefileattributes | ShGetFileInfoConstants.ShgfiSysiconindex;
				if (_size == SysImageListSize.SmallIcons) {
					dwFlags |= ShGetFileInfoConstants.ShgfiSmallicon;
				}
				// Get image list
				var shfi = new Shfileinfo();
				var shfiSize = (uint)Marshal.SizeOf(shfi.GetType());

				// Call SHGetFileInfo to get the image list handle
				// using an arbitrary file:
				_hIml = SHGetFileInfo(
					".txt",
					FileAttributeNormal,
					ref shfi,
					shfiSize,
					(uint)dwFlags);
				System.Diagnostics.Debug.Assert((_hIml != IntPtr.Zero), "Failed to create Image List");
			}
		}

		#endregion

		#region Constructor, Dispose, Destructor

		/// <summary>
		/// Creates a Small FileSystemImages SystemImageList 
		/// </summary>
		public SysImageList() {
			Create();
		}

		/// <summary>
		/// Creates a SystemImageList with the specified size
		/// </summary>
		/// <param name="size">Size of System ImageList</param>
		public SysImageList(SysImageListSize size) {
			_size = size;
			Create();
		}

		/// <summary>
		/// Clears up any resources associated with the SystemImageList
		/// </summary>
		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Clears up any resources associated with the SystemImageList
		/// when disposing is true.
		/// </summary>
		/// <param name="disposing">Whether the object is being disposed</param>
		public virtual void Dispose(bool disposing) {
			if (!_disposed) {
				if (disposing) {
					if (_iImageList != null) {
						Marshal.ReleaseComObject(_iImageList);
					}
					_iImageList = null;
				}
			}
			_disposed = true;
		}

		/// <summary>
		/// Finalise for SysImageList
		/// </summary>
		~SysImageList() {
			Dispose(false);
		}

	}

	#endregion

	#endregion

	#endregion

	#region SysImageListHelper

	/// <summary>
	/// Helper Methods for Connecting SysImageList to Common Controls
	/// </summary>
	public class SysImageListHelper
	{
		#region UnmanagedMethods

		private const int LvmFirst = 0x1000;
		private const int LvmSetimagelist = (LvmFirst + 3);

		private const int LvsilNormal = 0;
		private const int LvsilSmall = 1;
		private const int LvsilState = 2;

		private const int TvFirst = 0x1100;
		private const int TvmSetimagelist = (TvFirst + 9);

		private const int TvsilNormal = 0;
		private const int TvsilState = 2;

		[DllImport("user32", CharSet = CharSet.Auto)]
		private static extern IntPtr SendMessage(
			IntPtr hWnd,
			int wMsg,
			IntPtr wParam,
			IntPtr lParam);

		#endregion

		/// <summary>
		/// Associates a SysImageList with a ListView control
		/// </summary>
		/// <param name="listView">ListView control to associate ImageList with</param>
		/// <param name="sysImageList">System Image List to associate</param>
		/// <param name="forStateImages">Whether to add ImageList as StateImageList</param>
		public static void SetListViewImageList(
			ListView listView,
			SysImageList sysImageList,
			bool forStateImages
		) {
			var wParam = (IntPtr)LvsilNormal;
			if (sysImageList.ImageListSize == SysImageListSize.SmallIcons) {
				wParam = (IntPtr)LvsilSmall;
			}
			if (forStateImages) {
				wParam = (IntPtr)LvsilState;
			}
			SendMessage(
				listView.Handle,
				LvmSetimagelist,
				wParam,
				sysImageList.Handle);
		}

		/// <summary>
		/// Associates a SysImageList with a TreeView control
		/// </summary>
		/// <param name="treeView">TreeView control to associated ImageList with</param>
		/// <param name="sysImageList">System Image List to associate</param>
		/// <param name="forStateImages">Whether to add ImageList as StateImageList</param>
		public static void SetTreeViewImageList(
			TreeView treeView,
			SysImageList sysImageList,
			bool forStateImages
		) {
			var wParam = (IntPtr)TvsilNormal;
			if (forStateImages) {
				wParam = (IntPtr)TvsilState;
			}
			SendMessage(
				treeView.Handle,
				TvmSetimagelist,
				wParam,
				sysImageList.Handle);
		}
	}

	#endregion

}