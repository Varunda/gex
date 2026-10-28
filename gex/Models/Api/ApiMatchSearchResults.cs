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

        public SearchAst Ast { get; set; }

        public List<BarMatch> Results { get; set; } = [];

        public int Found { get; set; } = 0;

    }
}
