using System.Collections.Generic;

namespace gex.Code.ExtensionMethods {

    public static class StackExtensionMethod {

        /// <summary>
        ///     pop the top element off of the <see cref="Stack{T}"/>,
        ///     or returning null if the stack is empty
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stack"></param>
        /// <returns></returns>
        public static T? PopOrDefault<T>(this Stack<T> stack) where T : class {
            if (stack.TryPop(out T? result) == true) {
                return result;
            }

            return null;
        }

    }
}
