using System;

namespace gex.Models.Search {

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

        public static SearchToken OR_START => new SearchToken(SearchTokenType.OR_START, "");

    }

    public enum SearchTokenType {

        DEFAULT = 0,

        WORD,

        NOT,

        OR_START,

        OR_CONTINUE,

        OR_END,

        FIELD,

        PARAMETERS_START,

        PARAMETERS_END,

        OPERATOR,

        END,

    }

}
