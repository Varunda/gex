using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace gex.Code.ExtensionMethods {

    public static class ICollectionExtentionMethod {

        /// <summary>
        ///     extension method to turn a <see cref="ICollection"/> into an <see cref="List{T}"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="coll"></param>
        /// <returns></returns>
        public static List<T> CopyToList<T>(this ICollection coll) {
            T[] arr = new T[coll.Count];
            coll.CopyTo(arr, 0);

            return arr.ToList();
        }

    }
}
