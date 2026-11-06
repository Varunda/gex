using gex.Common.Models.Match;
using gex.Common.Models.Search;
using System.Collections.Generic;

namespace gex.Models.Api {

    public class ApiMatchSearchResults {

        public ApiMatchSearchResults(string query, SearchAst ast) {
            Query = query;
            Ast = ast;
        }

        public string Query { get; set; } = "";

        public string Normalized { get; set; } = "";

        public List<SearchToken> Tokens { get; set; } = [];

        public SearchAst Ast { get; set; }

        public List<ApiMatch> Results { get; set; } = [];

        public int Found { get; set; } = 0;

    }
}
