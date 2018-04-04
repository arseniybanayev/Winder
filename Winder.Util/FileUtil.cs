using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Winder.Util
{
	public static class FileUtil
	{
		/// <summary>
		/// Returns the %USERPROFILE% variable value (usually the home directory).
		/// </summary>
		public static string GetUserProfilePath() {
			return Environment.GetEnvironmentVariable("USERPROFILE");
		}
		
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

		/// <summary>
		/// Returns a set of favorite directories from the Links subdirectory of the user profile path.
		/// </summary>
		public static IEnumerable<DirectoryInfo> GetFavoritesFromUserLinks() {
			var userProfilePath = GetUserProfilePath();
			if (userProfilePath == null || !Directory.Exists(userProfilePath)) {
				Log.Info("Could not get a valid directory from system environment variable USERPROFILE");
				return Enumerable.Empty<DirectoryInfo>();
			}

			var path = Path.Combine(userProfilePath, "Links");
			if (!Directory.Exists(path)) {
				Log.Info($"Could not find Links directory in user profile path {userProfilePath}");
				return Enumerable.Empty<DirectoryInfo>();
			}

			var links = new DirectoryInfo(path).GetFiles().Where(f => f.Extension.ToLower() == ".lnk");
			return links.Select(lnk => GetShortcutTarget(lnk.FullName))
				.Where(tgt => !string.IsNullOrWhiteSpace(tgt)) // Things like "Recent Places"
				.Where(tgt => File.GetAttributes(tgt).HasFlag(FileAttributes.Directory)) // Slow
				.Select(tgt => new DirectoryInfo(tgt));
		}
	}
}