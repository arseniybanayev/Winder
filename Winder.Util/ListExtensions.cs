using System.Collections.Generic;
using System.Linq;

namespace Winder.Util
{
	public static class ListExtensions
	{
		public static IReadOnlyList<TElement> ToReadOnlyList<TElement>(this IEnumerable<TElement> source) {
			return source.ToList();
		}
	}
}