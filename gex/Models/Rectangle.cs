using SharpPeg.Operators;
using System;
using System.Configuration;

namespace gex.Models {

    public class Rectangle {

        public float Top { get; set; }

        public float Bottom { get; set; }

        public float Left { get; set; }

        public float Right { get; set; }

        public static Rectangle Zero => new() { Top = 0, Bottom = 0, Left = 0, Right = 0 };

        /// <summary>
        ///     check if a (x, z) point is within the rectangle. x is compared to left and right,
        ///     z is compared to top and bottom
        /// </summary>
        /// <param name="x"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public bool Within(float x, float z) {
            return x >= Left && x <= Right
                && z >= Top && z <= Bottom;
        }

        public static bool operator==(Rectangle? left, Rectangle? right) {
            if (left is null) {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator!=(Rectangle? left, Rectangle? right) {
            return !(left == right);
        }

        public override bool Equals(object? obj) {
            return obj is Rectangle data
                && Left == data.Left
                && Right == data.Right
                && Top == data.Top
                && Bottom == data.Bottom;
        }

        public override int GetHashCode() {
            return HashCode.Combine(Left, Right, Top, Bottom);
        }

    }
}
