using System.Collections.Generic;

namespace gex.Models.Bar {

    public class PolygonStartbox {

        public List<Side> Sides { get; set; } = [];

        public class Side {

            public int Index { get; set; }

            public List<Anchor> Anchors { get; set; } = [];

        }

        public struct Anchor {

            public double X { get; set; }

            public double Z { get; set; }

            /// <summary>
            ///     Catmull-Rom anchor weight, 0 = sharp corner
            /// </summary>
            public double Strength { get; set; }

        }

    }

}
