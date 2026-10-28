using System;

namespace gex.Common.Models.Search {

    public class SearchToken {

        public SearchToken(SearchTokenType type, string value) {
            Type = type;
            Value = value;
        }

        public SearchTokenType Type { get; set; }

        public string Value { get; set; }

        public override string ToString() {
            return $"<{nameof(SearchToken)} [{nameof(Type)}={Type}] [{nameof(Value)}='{Value}']>";
        }

        public override int GetHashCode() {
            return HashCode.Combine((int)Type, Value);
        }

        public static SearchToken GROUP_START => new(SearchTokenType.GROUP_START, "");

        public static SearchToken GROUP_END => new(SearchTokenType.GROUP_END, "");

        public static SearchToken OR_CONTINUE => new(SearchTokenType.OR_CONTINUE, "");

    }

    public enum SearchTokenType {

        DEFAULT = 0,

        WORD,

        NOT,

        GROUP_START,

        OR_CONTINUE,

        GROUP_END,

        FIELD,

        PARAMETERS_START,

        PARAMETERS_END,

        OPERATOR,

        END,

    }

}
