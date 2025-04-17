using System.Collections.Specialized;

namespace gex.Code.ExtensionMethods {

	public static class StringDictionaryExtensionMethods {

		/// <summary>
		/// adds a key to a <see cref="StringDictionary"/>, or updates the key with <paramref name="value"/>
		/// if the key is already present
		/// </summary>
		/// <param name="dict">extension instance</param>
		/// <param name="key">key to add or update</param>
		/// <param name="value">value to add or update</param>
		public static void AddOrUpdate(this StringDictionary dict, string key, string? value) {
			if (dict.ContainsKey(key)) {
				dict[key] = value;
			} else {
				dict.Add(key, value);
			}
		}

	}
}
