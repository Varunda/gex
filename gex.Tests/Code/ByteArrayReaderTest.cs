using gex.Code;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Code {

    [TestClass]
    public class ByteArrayReaderTest {

        [TestMethod]
        public void Test() {

            ByteArrayReader reader = new([
                0x01, 0x02, 0x03, 0x04,
                0x08, 0x07, 0x06, 0x05,
                (byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o',
                0xFF,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01
            ]);

            Assert.AreEqual(0x01020304, reader.ReadInt32BE());
            Assert.AreEqual(0x05060708, reader.ReadInt32LE());
            Assert.AreEqual("hello", reader.ReadAsciiString(5));
            Assert.AreEqual(0xFF, reader.ReadByte());
            Assert.AreEqual(1, reader.ReadInt64BE());
        }

        [TestMethod]
        public void ReadInt32BE() {
            ByteArrayReader reader = new([0x40, 0x41, 0x42, 0x43]);

            Assert.AreEqual(0x40414243, reader.ReadInt32BE());
            Assert.AreEqual(4, reader.Index);
        }

        [TestMethod]
        public void ReadInt32LE() {
            ByteArrayReader reader = new([0x43, 0x42, 0x41, 0x40]);

            Assert.AreEqual(0x40414243, reader.ReadInt32LE());
            Assert.AreEqual(4, reader.Index);
        }

        [TestMethod]
        public void ReadInt64BE() {
            ByteArrayReader reader = new([0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08]);

            Assert.AreEqual(0x0102030405060708, reader.ReadInt64BE());
            Assert.AreEqual(8, reader.Index);
        }

        [TestMethod]
        public void ReadAsciiString() {
            ByteArrayReader reader = new([(byte)'h', (byte)'e', (byte)'l', (byte)'l', (byte)'o']);

            Assert.AreEqual("hello", reader.ReadAsciiString(5));
            Assert.AreEqual(5, reader.Index);
        }

    }
}
