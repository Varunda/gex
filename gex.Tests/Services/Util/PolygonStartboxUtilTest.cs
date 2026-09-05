using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Services.Util;
using gex.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Services.Util {

    [TestClass]
    public class PolygonStartboxUtilTest {

        /**
         * much of this code is based on BAR's Lua implementation of this:
         * https://github.com/beyond-all-reason/Beyond-All-Reason/pull/7513
         */

        [TestMethod]
        public void Test_ParseJson() {
            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            Result<PolygonStartbox, string> ret = util.ParseJson(@"{
                ""startboxes"":[
                    {""poly"":[{""strength"":1,""x"":98,""y"":55},{""strength"":1,""x"":58,""y"":41},{""x"":42,""y"":21},{""x"":0,""y"":44}]},
                    {""poly"":[{""x"":0,""y"":156},{""x"":200,""y"":200}
                ]}]}");

            Assert.IsTrue(ret.IsOk, $"error: {ret.Error}");

            PolygonStartbox box = ret.Value;
            Assert.AreEqual(2, box.Sides.Count);

            PolygonStartbox.Side side0 = box.Sides[0];
            Assert.AreEqual(0, side0.Index);
            Assert.AreEqual(4, side0.Anchors.Count);

            Assert.AreEqual(98, side0.Anchors[0].X);
            Assert.AreEqual(55, side0.Anchors[0].Z);
            Assert.AreEqual(1, side0.Anchors[0].Strength);

            Assert.AreEqual(58, side0.Anchors[1].X);
            Assert.AreEqual(41, side0.Anchors[1].Z);
            Assert.AreEqual(1, side0.Anchors[1].Strength);

            Assert.AreEqual(42, side0.Anchors[2].X);
            Assert.AreEqual(21, side0.Anchors[2].Z);
            Assert.AreEqual(0d, side0.Anchors[2].Strength);

            Assert.AreEqual(0, side0.Anchors[3].X);
            Assert.AreEqual(44, side0.Anchors[3].Z);
            Assert.AreEqual(0d, side0.Anchors[3].Strength);

            PolygonStartbox.Side side1 = box.Sides[1];
            Assert.AreEqual(1, side1.Index);
            Assert.AreEqual(2, side1.Anchors.Count);

            Assert.AreEqual(0, side1.Anchors[0].X);
            Assert.AreEqual(156, side1.Anchors[0].Z);
            Assert.AreEqual(0d, side1.Anchors[0].Strength);

            Assert.AreEqual(200, side1.Anchors[1].X);
            Assert.AreEqual(200, side1.Anchors[1].Z);
            Assert.AreEqual(0d, side1.Anchors[1].Strength);
        }

        [TestMethod]
        public void Test_ParseBase64() {
            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            /*
             * {"startboxes":[
             *      {"poly":[{"x":0,"y":0},{"x":200,"y":0},{"x":200,"y":44},{"x":153,"y":23},{"strength":1,"x":98,"y":55},{"strength":1,"x":58,"y":41},{"x":42,"y":21},{"x":0,"y":44}]},
             *      {"poly":[{"x":0,"y":156},{"x":200,"y":200}]}
             *  ]}
             */
            Result<PolygonStartbox, string> ret = util.Parse(@"eJyrViouSSwqScqvSC1WsoquVirIz6kEMyqUrAx0lCqVrAxqdcA8IwPsfBMTqIChqTFYwMgYJFBcUpSal16SoWRlqAOStbQAS5qaYpM0hUiaGEKNMjGCmATjw22KBQlgutHQ1AzNVUYGBrWxtbG1AIHdP0Q");

            Assert.IsTrue(ret.IsOk, $"error: {ret.Error}");

            PolygonStartbox box = ret.Value;
            Assert.AreEqual(2, box.Sides.Count);

            PolygonStartbox.Side side0 = box.Sides[0];
            Assert.AreEqual(0, side0.Index);
            Assert.AreEqual(8, side0.Anchors.Count);

            Assert.AreEqual(0, side0.Anchors[0].X);
            Assert.AreEqual(0, side0.Anchors[0].Z);
            Assert.AreEqual(0, side0.Anchors[0].Strength);

            Assert.AreEqual(200, side0.Anchors[1].X);
            Assert.AreEqual(0, side0.Anchors[1].Z);
            Assert.AreEqual(0, side0.Anchors[1].Strength);

            Assert.AreEqual(200, side0.Anchors[2].X);
            Assert.AreEqual(44, side0.Anchors[2].Z);
            Assert.AreEqual(0, side0.Anchors[2].Strength);

            Assert.AreEqual(153, side0.Anchors[3].X);
            Assert.AreEqual(23, side0.Anchors[3].Z);
            Assert.AreEqual(0d, side0.Anchors[3].Strength);

            Assert.AreEqual(98, side0.Anchors[4].X);
            Assert.AreEqual(55, side0.Anchors[4].Z);
            Assert.AreEqual(1d, side0.Anchors[4].Strength);

            Assert.AreEqual(58, side0.Anchors[5].X);
            Assert.AreEqual(41, side0.Anchors[5].Z);
            Assert.AreEqual(1d, side0.Anchors[5].Strength);

            Assert.AreEqual(42, side0.Anchors[6].X);
            Assert.AreEqual(21, side0.Anchors[6].Z);
            Assert.AreEqual(0d, side0.Anchors[6].Strength);

            Assert.AreEqual(0, side0.Anchors[7].X);
            Assert.AreEqual(44, side0.Anchors[7].Z);
            Assert.AreEqual(0d, side0.Anchors[7].Strength);

            PolygonStartbox.Side side1 = box.Sides[1];
            Assert.AreEqual(1, side1.Index);
            Assert.AreEqual(2, side1.Anchors.Count);

            Assert.AreEqual(0, side1.Anchors[0].X);
            Assert.AreEqual(156, side1.Anchors[0].Z);
            Assert.AreEqual(0d, side1.Anchors[0].Strength);

            Assert.AreEqual(200, side1.Anchors[1].X);
            Assert.AreEqual(200, side1.Anchors[1].Z);
            Assert.AreEqual(0d, side1.Anchors[1].Strength);
        }

        /// <summary>
        ///     ensure that no strength anchor points return themselves
        /// </summary>
        [TestMethod]
        public void Test_TessellateRing_NoStrength() {
            List<PolygonStartbox.Anchor> anchors = [
                new PolygonStartbox.Anchor() { X = 0, Z = 0 },
                new PolygonStartbox.Anchor() { X = 100, Z = 0 },
                new PolygonStartbox.Anchor() { X = 100, Z = 100 },
                new PolygonStartbox.Anchor() { X = 0, Z = 100 },
            ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            List<PolygonStartboxUtil.Pair> tessellated = util.TessellateRing(anchors);
            Assert.AreEqual(4, tessellated.Count);

            Assert.AreEqual(0, tessellated[0].X);
            Assert.AreEqual(0, tessellated[0].Z);

            Assert.AreEqual(100, tessellated[1].X);
            Assert.AreEqual(0, tessellated[1].Z);

            Assert.AreEqual(100, tessellated[2].Z);
            Assert.AreEqual(100, tessellated[2].Z);

            Assert.AreEqual(0, tessellated[3].X);
            Assert.AreEqual(100, tessellated[3].Z);
        }

        [TestMethod]
        public void Test_TesselateRing_MixedStrengthStaysSharp() {
            List<PolygonStartbox.Anchor> anchors = [
                new PolygonStartbox.Anchor() { X = 0, Z = 0 },
                new PolygonStartbox.Anchor() { X = 100, Z = 0, Strength = 1 },
                new PolygonStartbox.Anchor() { X = 100, Z = 100, Strength = 1 },
                new PolygonStartbox.Anchor() { X = 0, Z = 100 },
            ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            List<PolygonStartboxUtil.Pair> tessellated = util.TessellateRing(anchors, 4);

            Assert.AreEqual(13, tessellated.Count);
        }

        /// <summary>
        ///     ensure that the anchor points don't move when tessellated
        /// </summary>
        [TestMethod]
        public void Test_TesselateRing_AnchorPositionsKept() {
            List<PolygonStartbox.Anchor> anchors = [
                new PolygonStartbox.Anchor() { X = 0, Z = 0, Strength = 1 },
                new PolygonStartbox.Anchor() { X = 100, Z = 0, Strength = 1 },
                new PolygonStartbox.Anchor() { X = 100, Z = 100, Strength = 1 },
                new PolygonStartbox.Anchor() { X = 0, Z = 100, Strength = 1 },
            ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            List<PolygonStartboxUtil.Pair> tessellated = util.TessellateRing(anchors, 4);

            Assert.AreEqual(16, tessellated.Count);

            Assert.AreEqual(0, tessellated[0].X);
            Assert.AreEqual(0, tessellated[0].X);

            Assert.AreEqual(100, tessellated[4].X);
            Assert.AreEqual(0, tessellated[4].Z);

            Assert.AreEqual(100, tessellated[8].X);
            Assert.AreEqual(100, tessellated[8].Z);

            Assert.AreEqual(0, tessellated[12].X);
            Assert.AreEqual(100, tessellated[12].Z);

            Assert.AreEqual(20.3125, tessellated[1].X);
            Assert.AreEqual(-9.375, tessellated[1].Z);

            Assert.AreEqual(50, tessellated[2].X);
            Assert.AreEqual(-12.5, tessellated[2].Z);

            Assert.AreEqual(79.6875, tessellated[3].X);
            Assert.AreEqual(-9.375, tessellated[3].Z);
        }

        /// <summary>
        ///     test points within a square
        /// </summary>
        [TestMethod]
        public void Test_PointWithinPolygon_Square() {

            List<PolygonStartboxUtil.Pair> verts = [
                new PolygonStartboxUtil.Pair() { X = 0, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 100 },
                new PolygonStartboxUtil.Pair() { X = 0, Z = 100 },
            ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            Assert.IsTrue(util.PointWithinPolygon(verts, 50, 50));

            Assert.IsFalse(util.PointWithinPolygon(verts, 150, 0));
            Assert.IsFalse(util.PointWithinPolygon(verts, 0, 150));
            Assert.IsFalse(util.PointWithinPolygon(verts, 50, 150));
            Assert.IsFalse(util.PointWithinPolygon(verts, 150, 50));
        }

        /// <summary>
        ///     test points within a diamond
        /// </summary>
        [TestMethod]
        public void Test_PointWithinPolygon_Diamond() {

            List<PolygonStartboxUtil.Pair> verts = [
                new PolygonStartboxUtil.Pair() { X = 50, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 50 },
                new PolygonStartboxUtil.Pair() { X = 50, Z = 100 },
                new PolygonStartboxUtil.Pair() { X = 0, Z = 50 },
            ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            Assert.IsTrue(util.PointWithinPolygon(verts, 50, 50));
            Assert.IsTrue(util.PointWithinPolygon(verts, 0, 50));

            Assert.IsFalse(util.PointWithinPolygon(verts, 150, 0));
            Assert.IsFalse(util.PointWithinPolygon(verts, 0, 150));
            Assert.IsFalse(util.PointWithinPolygon(verts, 50, 150));
            Assert.IsFalse(util.PointWithinPolygon(verts, 150, 50));
            Assert.IsFalse(util.PointWithinPolygon(verts, 5, 5));
            Assert.IsFalse(util.PointWithinPolygon(verts, 95, 5));
        }

        /// <summary>
        ///     test points within an L
        /// </summary>
        [TestMethod]
        public void Test_PointWithinPolygon_LShape() {

            /*
                6-------5
                |       |
                |   3---4
                |   |
                1---2   
            */

            List<PolygonStartboxUtil.Pair> verts = [
                new PolygonStartboxUtil.Pair() { X = 0, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 50, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 50, Z = 50 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 50 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 100 },
                new PolygonStartboxUtil.Pair() { X = 0, Z = 100 },
            ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            Assert.IsTrue(util.PointWithinPolygon(verts, 75, 75));
            Assert.IsTrue(util.PointWithinPolygon(verts, 25, 25));

            Assert.IsFalse(util.PointWithinPolygon(verts, 60, 40));
            Assert.IsFalse(util.PointWithinPolygon(verts, 150, 0));
            Assert.IsFalse(util.PointWithinPolygon(verts, 0, 150));
            Assert.IsFalse(util.PointWithinPolygon(verts, 50, 150));
            Assert.IsFalse(util.PointWithinPolygon(verts, 150, 50));
            Assert.IsFalse(util.PointWithinPolygon(verts, 75, 25));
        }

        /// <summary>
        ///     list of vertices that aren't valid polygons
        /// </summary>
        [TestMethod]
        public void Test_PointWithinPolygon_BadInputs() {

            List<PolygonStartboxUtil.Pair> verts = [ ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            Assert.IsFalse(util.PointWithinPolygon(verts, 0, 0));

            verts.Add(new PolygonStartboxUtil.Pair() {
                X = 0,
                Z = 0
            });
            Assert.IsFalse(util.PointWithinPolygon(verts, 0, 0));

            verts.Add(new PolygonStartboxUtil.Pair() {
                X = 0,
                Z = 0
            });
            Assert.IsFalse(util.PointWithinPolygon(verts, 0, 0));
        }

        /// <summary>
        ///     square with big maps
        /// </summary>
        [TestMethod]
        public void Test_PointWithinPolygon_BigMap() {

            List<PolygonStartboxUtil.Pair> verts = [
                new PolygonStartboxUtil.Pair() { X = 0, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 16384, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 16384, Z = 16384 },
                new PolygonStartboxUtil.Pair() { X = 0, Z = 16384 },
            ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            Assert.IsTrue(util.PointWithinPolygon(verts, 8192, 8192));
            Assert.IsTrue(util.PointWithinPolygon(verts, 8192, 0));
        }

        /// <summary>
        ///     test points within an square without a corner
        /// </summary>
        [TestMethod]
        public void Test_PointWithinPolygon_SquareWithoutCorner() {

            /*
                5-----4
                |     |
                |     3
                |    /
                1--2/
            */

            List<PolygonStartboxUtil.Pair> verts = [
                new PolygonStartboxUtil.Pair() { X = 0, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 50, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 50 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 100 },
                new PolygonStartboxUtil.Pair() { X = 0, Z = 100 },
            ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            Assert.IsTrue(util.PointWithinPolygon(verts, 75, 75));
            Assert.IsTrue(util.PointWithinPolygon(verts, 25, 25));
            Assert.IsTrue(util.PointWithinPolygon(verts, 60, 40));

            Assert.IsFalse(util.PointWithinPolygon(verts, 150, 0));
            Assert.IsFalse(util.PointWithinPolygon(verts, 0, 150));
            Assert.IsFalse(util.PointWithinPolygon(verts, 50, 150));
            Assert.IsFalse(util.PointWithinPolygon(verts, 150, 50));
            Assert.IsFalse(util.PointWithinPolygon(verts, 75, 25));
        }

        /// <summary>
        ///     test points within an square without a corner
        /// </summary>
        [TestMethod]
        public void Test_PointWithinPolygon_Concave() {

            /*
                8-------7
                |       |
                |   5---6
                |   |
                |   4---3
                |       |
                1-------2
            */

            List<PolygonStartboxUtil.Pair> verts = [
                new PolygonStartboxUtil.Pair() { X = 0, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 0 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 50 },
                new PolygonStartboxUtil.Pair() { X = 50, Z = 50 },
                new PolygonStartboxUtil.Pair() { X = 50, Z = 100 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 100 },
                new PolygonStartboxUtil.Pair() { X = 100, Z = 150 },
                new PolygonStartboxUtil.Pair() { X = 0, Z = 150 }
            ];

            PolygonStartboxUtil util = new(new TestLogger<PolygonStartboxUtil>());

            Assert.IsTrue(util.PointWithinPolygon(verts, 10, 10));
            Assert.IsTrue(util.PointWithinPolygon(verts, 0, 0));
            Assert.IsTrue(util.PointWithinPolygon(verts, 75, 25));

            Assert.IsFalse(util.PointWithinPolygon(verts, 110, 0));
            Assert.IsFalse(util.PointWithinPolygon(verts, 60, 60));
        }

    }

}
