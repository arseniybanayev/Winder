using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Winder.Util
{
	public static class FileUtil
	{
		/// <summary>
		/// Returns the %USERPROFILE% variable value (usually the home directory).
		/// </summary>
		public static NormalizedPath GetUserProfilePath() {
			return Environment.GetEnvironmentVariable("USERPROFILE").ToNormalizedPath();
		}

		/// <summary>
		/// If the extension of `file` is ".lnk" then returns the target of the shortcut.
		/// Otherwise, returns `file`.
		/// </summary>
		public static NormalizedPath GetShortcutTarget(NormalizedPath file) {
			if (file == null)
				return file;

			if (file.Extension?.ToLower().Equals(".lnk", StringComparison.OrdinalIgnoreCase) != true)
				return file;

			var shellObjectType = Type.GetTypeFromProgID("WScript.Shell");
			dynamic windowsShell = Activator.CreateInstance(shellObjectType);
			var shortcut = windowsShell.CreateShortcut(file.Value);
			var targetPath = shortcut.TargetPath;

			// Release the COM objects
			shortcut = null;
			windowsShell = null;
			return (targetPath as string).ToNormalizedPath();
		}

		/// <summary>
		/// Returns a set of favorite directories from the Links subdirectory of the user profile path.
		/// </summary>
		public static IEnumerable<NormalizedPath> GetFavoritesFromUserLinks() {
			var userProfilePath = GetUserProfilePath();
			if (userProfilePath == null || !Directory.Exists(userProfilePath)) {
				Log.Info("Could not get a valid directory from system environment variable USERPROFILE");
				return Enumerable.Empty<NormalizedPath>();
			}

			var path = Path.Combine(userProfilePath, "Links").ToNormalizedPath();
			if (!Directory.Exists(path)) {
				Log.Info($"Could not find Links directory in user profile path {userProfilePath}");
				return Enumerable.Empty<NormalizedPath>();
			}

			var links = new DirectoryInfo(path).GetFiles().Where(f => f.Extension.ToLower() == ".lnk");
			return links.Select(lnk => GetShortcutTarget(lnk.FullName.ToNormalizedPath()))
				.Where(tgt => !string.IsNullOrWhiteSpace(tgt)) // Things like "Recent Places"
				.Where(tgt => File.GetAttributes(tgt).HasFlag(FileAttributes.Directory)) // Slow
				.Distinct()
				.ToList();
		}

		public static string NormalizePath(string path) {
			return Path
				.GetFullPath(new Uri(path).LocalPath)
				.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		#region Recycle Bin

		/// <summary>
		/// Possible flags for the SHFileOperation method.
		/// </summary>
		[Flags]
		public enum FileOperationFlags : ushort
		{
			/// <summary>
			/// Do not show a dialog during the process
			/// </summary>
			FOF_SILENT = 0x0004,
			/// <summary>
			/// Do not ask the user to confirm selection
			/// </summary>
			FOF_NOCONFIRMATION = 0x0010,
			/// <summary>
			/// Delete the file to the recycle bin.  (Required flag to send a file to the bin
			/// </summary>
			FOF_ALLOWUNDO = 0x0040,
			/// <summary>
			/// Do not show the names of the files or folders that are being recycled.
			/// </summary>
			FOF_SIMPLEPROGRESS = 0x0100,
			/// <summary>
			/// Surpress errors, if any occur during the process.
			/// </summary>
			FOF_NOERRORUI = 0x0400,
			/// <summary>
			/// Warn if files are too big to fit in the recycle bin and will need
			/// to be deleted completely.
			/// </summary>
			FOF_WANTNUKEWARNING = 0x4000,
		}

		/// <summary>
		/// File Operation Function Type for SHFileOperation
		/// </summary>
		public enum FileOperationType : uint
		{
			/// <summary>
			/// Move the objects
			/// </summary>
			FO_MOVE = 0x0001,
			/// <summary>
			/// Copy the objects
			/// </summary>
			FO_COPY = 0x0002,
			/// <summary>
			/// Delete (or recycle) the objects
			/// </summary>
			FO_DELETE = 0x0003,
			/// <summary>
			/// Rename the object(s)
			/// </summary>
			FO_RENAME = 0x0004,
		}

		/// <summary>
		/// SHFILEOPSTRUCT for SHFileOperation from COM
		/// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private struct SHFILEOPSTRUCT
		{

			public IntPtr hwnd;
			[MarshalAs(UnmanagedType.U4)]
			public FileOperationType wFunc;
			public string pFrom;
			public string pTo;
			public FileOperationFlags fFlags;
			[MarshalAs(UnmanagedType.Bool)]
			public bool fAnyOperationsAborted;
			public IntPtr hNameMappings;
			public string lpszProgressTitle;
		}

		[DllImport("shell32.dll", CharSet = CharSet.Auto)]
		private static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

		/// <summary>
		/// Send file to recycle bin
		/// </summary>
		/// <param name="path">Location of directory or file to recycle</param>
		/// <param name="flags">FileOperationFlags to add in addition to FOF_ALLOWUNDO</param>
		public static bool SendToRecycleBin(string path, FileOperationFlags flags) {
			try {
				var fs = new SHFILEOPSTRUCT {
					wFunc = FileOperationType.FO_DELETE,
					pFrom = path + '\0' + '\0',
					fFlags = FileOperationFlags.FOF_ALLOWUNDO | flags
				};
				SHFileOperation(ref fs);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		/// <summary>
		/// Send file to recycle bin. Display dialog, display warning if files are too big to fit (FOF_WANTNUKEWARNING)
		/// </summary>
		/// <param name="path">Location of directory or file to recycle</param>
		public static bool SendToRecycleBin(string path) {
			return SendToRecycleBin(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_WANTNUKEWARNING);
		}

		/// <summary>
		/// Send file silently to recycle bin. Surpress dialog, surpress errors, delete if too large.
		/// </summary>
		/// <param name="path">Location of directory or file to recycle</param>
		public static bool SendToRecycleBinSilent(string path) {
			return SendToRecycleBin(path, FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOF_SILENT);
		}

		/// <summary>
		/// Permanently delete a file. THERE IS NO UNDO
		/// </summary>
		public static bool DeletePermanently(string path) {
			try {
				var fs = new SHFILEOPSTRUCT {
					wFunc = FileOperationType.FO_DELETE,
					pFrom = path + '\0' + '\0',
					fFlags = FileOperationFlags.FOF_NOCONFIRMATION | FileOperationFlags.FOF_NOERRORUI | FileOperationFlags.FOF_SILENT
				};
				SHFileOperation(ref fs);
				return true;
			} catch (Exception) {
				return false;
			}
		}

		#endregion

	}
}