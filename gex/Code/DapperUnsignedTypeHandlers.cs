using Dapper;
using System;
using System.Data;

namespace gex.Code {

    public class DapperUnsignedTypeHandlers {

        /// <summary>
        ///		handles <code>uint</code>s
        /// </summary>
        public class UIntHandler : SqlMapper.TypeHandler<uint> {

            public override uint Parse(object value) {
                return Convert.ToUInt32(value);
            }

            public override void SetValue(IDbDataParameter parameter, uint value) {
                parameter.DbType = DbType.Int32;
                parameter.Value = unchecked((int)(uint)value);
            }

        }

        /// <summary>
        ///		handles <code>ulong</code>s
        /// </summary>
        public class ULongHandler : SqlMapper.TypeHandler<ulong> {

            public override ulong Parse(object value) {
                return Convert.ToUInt64(value);
            }

            public override void SetValue(IDbDataParameter parameter, ulong value) {
                parameter.DbType = DbType.Int64;
                parameter.Value = unchecked((long)(ulong)value);
            }

        }

    }
}
