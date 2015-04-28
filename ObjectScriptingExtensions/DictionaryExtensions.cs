using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectScriptingExtensions
{
    public static class DictionaryExtensions
    {
		public static T MergeLeft<T, K, V>(this T me, params IDictionary<K, V>[] others)
	  where T : IDictionary<K, V>, new()
		{
			T newMap = new T();
			foreach (IDictionary<K, V> src in
				(new List<IDictionary<K, V>> { me }).Concat(others))
			{
				// ^-- echk. Not quite there type-system.
				foreach (KeyValuePair<K, V> p in src)
				{
					newMap[p.Key] = p.Value;
				}
			}
			return newMap;
		}
        public static string FirstUnique<TValue>(this Dictionary<string, TValue> dictionary, string name)
        {
            string generatedName = name;
			if (generatedName.Length > 125)
				generatedName = generatedName.Substring(0, 125);
            int i = 0;
            while (dictionary.ContainsKey(generatedName))
            {
                i = i + 1;
                generatedName = name + i;
            }
            return generatedName;
        }
    }
}
