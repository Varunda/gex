namespace gex.Models {

    public class Rectangle {

        public float Top { get; set; }

        public float Bottom { get; set; }

        public float Left { get; set; }

        public float Right { get; set; }

        public static Rectangle Zero => new() { Top = 0, Bottom = 0, Left = 0, Right = 0 };

    }
}
