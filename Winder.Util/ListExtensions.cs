using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Winder.Util
{
	public static class ListExtensions
	{
		public static IReadOnlyList<TElement> ToReadOnlyList<TElement>(this IEnumerable<TElement> source) {
			return source.ToList();
		}
	}
}