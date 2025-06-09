using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace gex.Tests {

    [TestClass]
    public class ExampleTest {

        /// <summary>
        ///     Confirm equality overload is good
        /// </summary>
        [TestMethod]
        public void TestEquality() {
            Assert.AreEqual(1, 1);
        }

        [TestMethod]
        [DataRow(0, 0)]
        [DataRow(1, 1)]
        [DataRow(2, 2)]
        public void TestColumnString(int a, int b) {
            Assert.AreEqual(a, b);
        }

    }
}
