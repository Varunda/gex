using gex.Common.Models.Search;
using System;

namespace gex.Code.Exceptions {

    public class SearchAstParseException : Exception {

        public SearchToken? ErroringToken { get; set; }

        public SearchAstParseException(SearchToken? erroringToken) : base() {
            ErroringToken = erroringToken;
        }

        public SearchAstParseException(SearchToken? erroringToken, string message) : base(message) {
            ErroringToken = erroringToken;
        }

        public SearchAstParseException(SearchToken? erroringToken, string message, Exception? inner) : base(message, inner) {
            ErroringToken = erroringToken;
        }

    }
}
