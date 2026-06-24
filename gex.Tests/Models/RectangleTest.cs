using gex.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Models {

    [TestClass]
    public class RectangleTest {

        [TestMethod("test Rectangle.Within()")]
        public void Test_Within() {

            Rectangle rect = new() {
                Bottom = 10,
                Top = 0,
                Left = 0,
                Right = 10
            };

            Assert.IsTrue(rect.Within(0, 0));
            Assert.IsTrue(rect.Within(1, 1));
            Assert.IsTrue(rect.Within(9, 9));
            Assert.IsTrue(rect.Within(10, 10));
            Assert.IsTrue(rect.Within(0, 10));
            Assert.IsTrue(rect.Within(10, 0));

            Assert.IsFalse(rect.Within(11, 0));
            Assert.IsFalse(rect.Within(11, 11));
            Assert.IsFalse(rect.Within(0, 11));
            Assert.IsFalse(rect.Within(-1, 5));
            Assert.IsFalse(rect.Within(5, -1));
        }

        [TestMethod("test Rectangle== and Rectangle.Equals()")]
        public void Test_Equals() {

            Rectangle r1 = new() {
                Bottom = 1,
                Top = 0,
                Left = 0,
                Right = 1,
            };

            Assert.AreEqual(r1, r1);

            Rectangle r2 = new() {
                Bottom = 2,
                Top = 0,
                Left = 0,
                Right = 2
            };

            Assert.AreEqual(r2, r2);

            Assert.AreNotEqual(r1, r2);
            Assert.IsFalse(r1 == r2);
        }

    }
}
