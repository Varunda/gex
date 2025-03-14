using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace gex.Code {

    public class ByteArrayReader {

        private int _Index;

        private byte[] _Data;

        public ByteArrayReader(byte[] data) {
            _Data = data;
            _Index = 0;
        }

        public int Index => _Index;

        public Span<byte> Read(int amount) {
            Span<byte> span = _Data.AsSpan(_Index, amount);
            _Index += amount;

            return span;
        }

        public Span<byte> Read(uint amount) {
            return Read((int)amount);
        }

        public string ReadAsciiString(int length) {
            Span<byte> span = Read(length);
            return Encoding.ASCII.GetString(span);
        }

        public string ReadAsciiStringNullTerminated(int length) {
            Span<byte> span = Read(length);
            if (span[^1] != 0x00) {
                throw new Exception($"expected null terminator for last byte, got {span[^1]} instead");
            }
            return Encoding.ASCII.GetString(span[..^1]);
        }

        /// <summary>
        ///     read the rest of the bytes
        /// </summary>
        /// <returns></returns>
        public byte[] ReadAll() {
            Span<byte> b = _Data.AsSpan(_Index);
            _Index += b.Length;
            return b.ToArray();
        }

        public byte ReadByte() {
            byte b = _Data[_Index];
            _Index += 1;
            return b;
        }

        public byte[] ReadUntilNull() {
            List<byte> bytes = [];

            while (true) {
                byte b = ReadByte();
                if (b == 0x00) {
                    break;
                }
                bytes.Add(b);
            }

            return bytes.ToArray();
        }

        public short ReadInt16BE() {
            short value = BinaryPrimitives.ReadInt16BigEndian(_Data.AsSpan(_Index, 2));
            _Index += 2;
            return value;
        }

        public short ReadInt16LE() {
            short value = BinaryPrimitives.ReadInt16LittleEndian(_Data.AsSpan(_Index, 2));
            _Index += 2;
            return value;
        }

        public int ReadInt32BE() {
            int value = BinaryPrimitives.ReadInt32BigEndian(_Data.AsSpan(_Index, 4));
            _Index += 4;
            return value;
        }

        public uint ReadUInt32BE() {
            uint value = BinaryPrimitives.ReadUInt32BigEndian(_Data.AsSpan(_Index, 4));
            _Index += 4;
            return value;
        }

        public long ReadInt64BE() {
            long value = BinaryPrimitives.ReadInt64BigEndian(_Data.AsSpan(_Index, 8));
            _Index += 8;
            return value;
        }

        public ulong ReadUInt64BE() {
            ulong value = BinaryPrimitives.ReadUInt64BigEndian(_Data.AsSpan(_Index, 8));
            _Index += 8;
            return value;
        }

        public int ReadInt32LE() {
            int value = BinaryPrimitives.ReadInt32LittleEndian(_Data.AsSpan(_Index, 4));
            _Index += 4;
            return value;
        }

        public uint ReadUInt32LE() {
            uint value = BinaryPrimitives.ReadUInt32LittleEndian(_Data.AsSpan(_Index, 4));
            _Index += 4;
            return value;
        }

        public long ReadInt64LE() {
            long value = BinaryPrimitives.ReadInt64LittleEndian(_Data.AsSpan(_Index, 8));
            _Index += 8;
            return value;
        }

        public ulong ReadUInt64LE() {
            ulong value = BinaryPrimitives.ReadUInt64LittleEndian(_Data.AsSpan(_Index, 8));
            _Index += 8;
            return value;
        }

        public float ReadFloat32BE() {
            float value = BinaryPrimitives.ReadSingleBigEndian(_Data.AsSpan(_Index, 4));
            _Index += 4;
            return value;
        }

        public float ReadFloat32LE() {
            float value = BinaryPrimitives.ReadSingleLittleEndian(_Data.AsSpan(_Index, 4));
            _Index += 4;
            return value;
        }

    }
}
