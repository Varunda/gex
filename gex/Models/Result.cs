using System;

namespace gex.Models {

    public class Result<T, E> {

        private readonly bool _Success;

        public T Value { get; private set; }
        public E Error { get; private set; }
        public Exception? Exception { get; private set; }

        private Result(T? value, E? err, bool success) {
            if (success == true && value == null) {
                throw new NullReferenceException();
            }
            if (success == false && err == null) {
                throw new NullReferenceException();
            }

            this.Value = value!;
            this.Error = err!;
            this._Success = success;
        }

        public bool IsOk => _Success;

        public static Result<T, E> Ok(T value) {
            return new Result<T, E>(value, default(E), true);
        }

        public static Result<T, E> Err(E err) {
            return new Result<T, E>(default(T), err, false);
        }

        public static Result<T, E> Err(E err, Exception ex) {
            return new Result<T, E>(default(T), err, false) {
                Exception = ex
            };
        }

        public static implicit operator Result<T, E>(T value) => Ok(value);

        public static implicit operator Result<T, E>(E err) => Err(err);

    }

}
