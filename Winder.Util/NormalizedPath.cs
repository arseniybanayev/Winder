using System;
using System.IO;

namespace Winder.Util
{
	public class NormalizedPath
	{
		public NormalizedPath(string path) {
			if (string.IsNullOrWhiteSpace(path)) {
				Value = null;
				return;
			}

			var normalizedPath = Path.GetFullPath(new Uri(Path.GetFullPath(path)).LocalPath);

			// Remove trailing directory separator chars (like '\' or '/')
			var trimmed = normalizedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			// Add back a trailing directory separator char if it now ends with a volume separator char (like ':')
			// Otherwise it won't point to a volume
			if (trimmed.EndsWith(Path.VolumeSeparatorChar.ToString()))
				trimmed += Path.DirectorySeparatorChar;

			Value = trimmed;
		}

		public string Value { get; }

		public string Extension => Path.GetExtension(Value);

		public static implicit operator string(NormalizedPath path) {
			return path.Value;
		}

		public override bool Equals(object obj) {
			if (!(obj is NormalizedPath path))
				return false; // because 'this' is not null
			return string.Equals(this.Value, path.Value, StringComparison.CurrentCultureIgnoreCase);
		}

		public override int GetHashCode() {
			return StringComparer.CurrentCultureIgnoreCase.GetHashCode(this.Value);
		}

		public override string ToString() => Value;
	}

	public static class NormalizedPathExtensions
	{
		public static NormalizedPath ToNormalizedPath(this string path) {
			return new NormalizedPath(string.IsNullOrWhiteSpace(path) ? null : path);
		}
	}
}