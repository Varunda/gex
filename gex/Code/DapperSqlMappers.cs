using Dapper;
using gex.Common.Code.Constants;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.Json;

namespace gex.Code {

    public static class DapperSqlMappers {

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

        public class JsonbHandler : SqlMapper.TypeHandler<JsonElement> {

            public override JsonElement Parse(object value) {
                return JsonSerializer.Deserialize<JsonElement>(value.ToString() ?? "");
            }

            public override void SetValue(IDbDataParameter parameter, JsonElement value) {
                throw new NotImplementedException();
            }

        }

        public class MapSymmetryAxisHandler : SqlMapper.TypeHandler<MapSymmetryAxis> {

            public override MapSymmetryAxis Parse(object value) {
                return (MapSymmetryAxis)value;
            }

            public override void SetValue(IDbDataParameter parameter, MapSymmetryAxis value) {
                parameter.DbType = DbType.Int32;
                parameter.Value = (int)value;
            }

        }

        public class HashSetStringHandler : SqlMapper.TypeHandler<HashSet<string>> {

            public override HashSet<string>? Parse(object value) {
                return new HashSet<string>(value.ToString()!.Split(",").Where(iter => iter != ""));
            }

            public override void SetValue(IDbDataParameter parameter, HashSet<string>? value) {
                throw new NotImplementedException();
            }

        }

    }
}
