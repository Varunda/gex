using gex.Common.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Models.Common {

    [TestClass]
    public class MaybeTest {

        [TestMethod]
        public void Test_Some() {
            Maybe<int> a = Maybe<int>.Some(4);
            Assert.IsTrue(a.Has());
            Assert.AreEqual(4, a.Get());

            Maybe<string> b = Maybe<string>.Some("abc");
            Assert.IsTrue(b.Has());
            Assert.AreEqual("abc", b.Get());
        }

        [TestMethod]
        public void Test_None() {
            Maybe<int> a = Maybe<int>.None();
            Assert.IsFalse(a.Has());
            Assert.ThrowsException<NullReferenceException>(() => a.Get());

            Maybe<string> b = Maybe<string>.None();
            Assert.IsFalse(b.Has());
            Assert.ThrowsException<NullReferenceException>(b.Get);
        }

    }
}
