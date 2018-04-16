using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winder.Util
{
	public class NormalizedPath
	{
		public NormalizedPath(string path) {
			Value = string.IsNullOrWhiteSpace(path)
				? null
				: Path.GetFullPath(new Uri(path).LocalPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}

		public string Value { get; }

		public string Extension => Path.GetExtension(Value);

		public static implicit operator string(NormalizedPath path) {
			return path.Value;
		}

		public override bool Equals(object obj) {
			var path = obj as NormalizedPath;
			if (path == null)
				return false; // because 'this' is not null
			return string.Equals(this.Value, path.Value, StringComparison.CurrentCultureIgnoreCase);
		}

		public override int GetHashCode() {
			return StringComparer.CurrentCultureIgnoreCase.GetHashCode(this.Value);
		}
	}

	public static class NormalizedPathExtensions
	{
		public static NormalizedPath ToNormalizedPath(this string path) {
			return new NormalizedPath(string.IsNullOrWhiteSpace(path) ? null : path);
		}
	}
}