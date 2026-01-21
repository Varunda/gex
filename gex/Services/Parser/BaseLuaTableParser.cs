using System;
using System.Collections.Generic;

namespace gex.Services.Parser {

    public class BaseLuaTableParser {

        internal double _Double(Dictionary<object, object> dict, string field, double fallback) {
            if (dict.ContainsKey(field) == false) {
                return fallback;
            }

            object obj = dict[field];
            return Convert.ToDouble(obj);
        }

        internal int _Int(Dictionary<object, object> dict, string field, int fallback) {
            if (dict.ContainsKey(field) == false) {
                return fallback;
            }

            object obj = dict[field];
            return Convert.ToInt32(obj);
        }

        internal bool _Bool(Dictionary<object, object> dict, string field, bool fallback) {
            return (dict.GetValueOrDefault(field) as bool?) ?? fallback;
        }

        internal string? _Str(Dictionary<object, object> dict, string field) {
            return dict.GetValueOrDefault(field) as string;
        }

        internal Dictionary<object, object>? _Dict(Dictionary<object, object> dict, string field) {
            return dict.GetValueOrDefault(field) as Dictionary<object, object>;
        }

    }
}
