using System;
using System.IO;

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