using gex.Code.ExtensionMethods;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Code.ExtensionMethods {

    [TestClass]
    public class StringExtensionMethodsTest {

        [TestMethod]
        public void StringExtensionMethod_TruncateTest() {
            Assert.AreEqual("a...", "abcdef".Truncate(1));
            Assert.AreEqual("abc", "abc".Truncate(10));
            Assert.AreEqual("...", "abc".Truncate(0));
            Assert.AreEqual("abc...", "abcdef".Truncate(3));

            string s = "abcdef";
            Assert.AreEqual("abc...", s.Truncate(3));
            Assert.AreEqual("abcdef", s); // ensure .Truncate() does not change input
        }

        [TestMethod]
        public void StringExtensionMethod_EscapeDiscordTest() {
            Assert.AreEqual("\\~~hi\\~~", "~~hi~~".EscapeDiscordCharacters());
            Assert.AreEqual("\\*\\*hi\\*\\*", "**hi**".EscapeDiscordCharacters());
            Assert.AreEqual("\\_name", "_name".EscapeDiscordCharacters());
        }

        [TestMethod]
        public void StringExtensionMethod_EscapeRecoileFilesystemTest() {
            Assert.AreEqual("_________", "<>|' :/?\"".EscapeRecoilFilesytemCharacters());
            Assert.AreEqual("Devil_s_Pond", "Devil's Pond".EscapeRecoilFilesytemCharacters());
        }

    }
}
