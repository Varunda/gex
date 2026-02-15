using gex.Common.Models;
using Microsoft.Extensions.Logging;
using SharpPeg.Operators;
using SharpPeg.SelfParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace gex.Common.Services.Bar {

    public class TdfParser {

        private readonly ILogger<TdfParser> _Logger;

        private const string TdfGrammer = """
            ---
            export type TDFObj = { [k: string]: (TDFObj | string) };
            ---

            Start := root=SectionBody $

            LineComment := '//.*'
            BlockComment := '/\*.*?\*/'s
            Comment := LineComment | BlockComment
            Empty := { '\s*' Comment? }*

            Key := '[^\s=]+'
            UnquotedValue := value='[^\n;]*'
            QuotedValue := '"' value='[^\n"]*' '"[ \t]*' &';'
            Entry := !'\[' key=Key '[ \t]*=[ \t]*' _val={ QuotedValue | UnquotedValue } ';+'
                .value = string { return this._val.value; }

            SectionBody := Empty _e={ _el={ Section | Entry } Empty }*
                .value = TDFObj { return Object.fromEntries(this._e.map(e => [e._el.key.toLowerCase(), e._el.value])); }

            Section := '\[' key='[^\]]+' '\]' Empty '\{' body=SectionBody '\}'
                .value = TDFObj { return body.value; }
        """;

        public TdfParser(ILogger<TdfParser> logger) {
            _Logger = logger;
        }

        public Result<JsonElement, string> Parse(string input) {
            PegGrammar grammar = new PegGrammar();
            IEnumerable<Pattern> pattern = grammar.ParseGrammar(TdfGrammer);


            return Result<JsonElement, string>.Err("not implemented");
        }

    }
}
