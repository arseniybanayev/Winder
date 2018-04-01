using System;
using System.Collections.Concurrent;

namespace Winder.Util
{
	public class LazyConcurrentDictionary<TKey, TValue>
	{
		private readonly ConcurrentDictionary<TKey, Lazy<TValue>> _dictionary;
		public LazyConcurrentDictionary() {
			_dictionary = new ConcurrentDictionary<TKey, Lazy<TValue>>();
		}

		public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory) {
			var lazyResult = _dictionary.GetOrAdd(key, k => new Lazy<TValue>(() => valueFactory(k)));
			return lazyResult.Value;
		}
	}
}