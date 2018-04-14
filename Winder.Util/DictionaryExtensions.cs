using System;
using System.Collections.Generic;
using System.Linq;

namespace Winder.Util
{
	public static class DictionaryExtensions
	{
		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
			this IEnumerable<TValue> source, Func<TValue, TKey> keySelector) {
			return source.ToDictionary(keySelector, v => v);
		}

		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
			this IEnumerable<TValue> source, Func<TValue, int, TKey> keySelector) {
			return source.ToDictionary(keySelector, (v, i) => v);
		}

		public static Dictionary<TKey, TValue> ToDictionary<TSource, TKey, TValue>(
			this IEnumerable<TSource> source, Func<TSource, int, TKey> keySelector, Func<TSource, int, TValue> valueSelector) {
			return source
				.Select((v, i) => Tuple.Create(v, i))
				.ToDictionary(t => keySelector(t.Item1, t.Item2), t => valueSelector(t.Item1, t.Item2));
		}

		public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
			this IEnumerable<KeyValuePair<TKey, TValue>> source) {
			return source.ToDictionary(kv => kv.Key, kv => kv.Value);
		}
	}
}