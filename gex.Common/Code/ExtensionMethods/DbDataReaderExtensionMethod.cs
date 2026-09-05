using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace gex.Common.Code.ExtensionMethods {

    public static class DbDataReaderExtensionMethods {

        /// <summary>
        /// Get a string value from a column that may be null
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static string? GetNullableString(this DbDataReader reader, string field) {
            if (reader.IsDBNull(field)) {
                return null;
            }
            return reader.GetString(field);
        }

        public static DateTime? GetNullableDateTime(this DbDataReader reader, string field) {
            if (reader.IsDBNull(field)) {
                return null;
            }
            return reader.GetDateTime(field);
        }

        public static decimal? GetNullableDecimal(this DbDataReader reader, string field) {
            if (reader.IsDBNull(field)) {
                return null;
            }
            return (decimal?)reader.GetFloat(field);
        }

        public static short? GetNullableInt16(this DbDataReader reader, string field) {
            if (reader.IsDBNull(field)) {
                return null;
            }
            return reader.GetInt16(field);
        }

        public static int? GetNullableInt32(this DbDataReader reader, string field) {
            if (reader.IsDBNull(field)) {
                return null;
            }
            return reader.GetInt32(field);
        }

        public static double? GetNullableDouble(this DbDataReader reader, string field) {
            if (reader.IsDBNull(field)) {
                return null;
            }
            return reader.GetDouble(field);
        }

        public static long? GetNullableInt64(this DbDataReader reader, string field) {
            if (reader.IsDBNull(field)) {
                return null;
            }
            return reader.GetInt64(field);
        }

        public static ulong GetUInt64(this DbDataReader reader, string field) {
            return unchecked((ulong)reader.GetInt64(field));
        }

        public static ulong? GetNullableUInt64(this DbDataReader reader, string field) {
            if (reader.IsDBNull(field)) {
                return null;
            }
            return unchecked((ulong)reader.GetInt64(field));
        }

        /// <summary>
        /// Get a <see cref="uint"/> from a column
        /// </summary>
        /// <param name="reader">Extension instance</param>
        /// <param name="field">Name of that field that has the <c>uint</c></param>
        /// <returns>The <c>uint</c> in the column, cast from an int32</returns>
        public static uint GetUInt32(this DbDataReader reader, string field) {
            return unchecked((uint)reader.GetInt32(field));
        }

        public static JsonElement GetJsonb(this DbDataReader reader, string field) {
            return JsonSerializer.Deserialize<JsonElement>(reader.GetString(field));
        }

    }

}
