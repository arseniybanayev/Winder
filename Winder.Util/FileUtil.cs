using System;
using System.IO;

namespace Winder.Util
{
	public static class FileUtil
	{
		/// <summary>
		/// If the extension of `file` is ".lnk" then returns the target of the shortcut.
		/// Otherwise, returns `file`.
		/// </summary>
		public static string GetShortcutTarget(string file) {
			if (Path.GetExtension(file)?.ToLower().Equals(".lnk", StringComparison.OrdinalIgnoreCase) != true)
				return file;

			var shellObjectType = Type.GetTypeFromProgID("WScript.Shell");
			dynamic windowsShell = Activator.CreateInstance(shellObjectType);
			var shortcut = windowsShell.CreateShortcut(file);
			var targetPath = shortcut.TargetPath;
			
			// Release the COM objects
			shortcut = null;
			windowsShell = null;
			return targetPath;
		}
	}
}