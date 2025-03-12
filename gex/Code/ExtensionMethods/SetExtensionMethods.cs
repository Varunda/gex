using System.Collections.Generic;

namespace gex.Code.ExtensionMethods {

    public static class SetExtensionMethods {

        public static void AddRange<T>(this ISet<T> set, IEnumerable<T> entries) {
            foreach (T t in entries) {
                set.Add(t);
            }
        }

    }
}
