using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models {

    /// <summary>
    ///     optional class. used like Nullable, but keeps runtime info instead of being lost at runtime for reference types
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class Maybe<T> {

        public static explicit operator Maybe<T>(T value) => Some(value);

        private bool _HasValue = false;

        public static Maybe<T> Some(T value) {
            return new Options.Some(value);
        }

        public static Maybe<T> None() {
            return new Options.None();
        }

        public bool Has() {
            return _HasValue;
        }

        public T Get() {
            if (this is Options.None) {
                throw new NullReferenceException();
            } else if (this is Options.Some some) {
                return some.Value;
            } else {
                throw new InvalidOperationException();
            }
        }

        private static class Options {

            public sealed class Some : Maybe<T> {

                public T Value { get; }

                public Some(T value) {
                    this.Value = value;
                    this._HasValue = true;
                }

            }

            public sealed class None : Maybe<T> {

                public None() {
                    this._HasValue = false;
                }

            }

        }

    }

}
