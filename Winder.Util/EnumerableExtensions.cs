using System;
using System.Collections.Generic;
using System.Linq;

namespace Winder.Util
{
	public static class EnumerableExtensions
	{
		public static string ToStringDelimited<T>(this IEnumerable<T> source, Func<T, string> stringSelector, string separator = ",") {
			return string.Join(separator, source.Select(stringSelector).ToArray());
		}

		public static string ToStringDelimited<T>(this IEnumerable<T> source, string separator = ",") {
			return source.ToStringDelimited(str => str.ToString(), separator);
		}
	}
}