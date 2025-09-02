using System;
using System.ComponentModel;

namespace gex.Code.ExtensionMethods {

    public static class NLuaTableExtensionMethod {

        public static T GetValue<T>(this NLua.LuaTable table, string field, T fallback) {
            object? value = table[field];
            if (value == null) {
                return fallback;
            }

            TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
            return (T?)conv.ConvertFrom(value) ?? fallback;
        }

        public static double GetDouble(this NLua.LuaTable table, string field, double fallback) {
            object? value = table[field];
            string? str = value?.ToString();
            if (str == null) {
                return fallback;
            }

            if (double.TryParse(str, out double d) == true) {
                return d;
            } else {
                throw new FormatException($"failed to convert '{str} into a valid double");
            }
        }

        public static bool GetBoolean(this NLua.LuaTable table, string field, bool fallback) {
            object? value = table[field];
            if (value == null) {
                return fallback;
            }

            return value is long v ? v == 1 : (bool)value;
        }

        public static string? GetString(this NLua.LuaTable table, string field) {
            object? value = table[field];
            if (value == null) {
                return null;
            }

            return value.ToString();
        }

    }
}
