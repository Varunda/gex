using gex.Common.Models.Search;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace gex.Services.Search {

    public class SearchTokenizer {

        private readonly ILogger<SearchTokenizer> _Logger;

        public SearchTokenizer(ILogger<SearchTokenizer> logger) {
            _Logger = logger;
        }

        public List<SearchToken> Tokenize(string q) {
            _Logger.LogDebug($"starting new tokenizing [q={q}]");

            List<SearchToken> tokens = new();

            bool inQuote = false;
            string word = "";

            void SaveWord() {
                tokens.Add(new SearchToken(SearchTokenType.WORD, word));
                _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                word = "";
            }

            foreach (char i in q) {
                _Logger.LogTrace($"tokenize iteration [iter={i}] [word={word}]");

                // if in a quote, only stop for another quote
                // TODO: support escaped quotes?
                if (inQuote == true) {
                    if (i != '"') {
                        word += i;
                        continue;
                    }
                }

                if (i == ' ') {
                    _Logger.LogTrace($"got space character");
                    if (word.Length > 0) {
                        SaveWord();
                    }
                }

                // GROUP_START {
                else if (i == '{') {
                    if (word.Length > 0) {
                        SaveWord();
                    }

                    tokens.Add(new SearchToken(SearchTokenType.GROUP_START, ""));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                } 

                // GROUP_END }
                else if (i == '}') {
                    if (word.Length > 0) {
                        SaveWord();
                    }

                    tokens.Add(new SearchToken(SearchTokenType.GROUP_END, ""));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                }

                // PARAMETERS_START (
                else if (i == '(') {
                    if (word.Length > 0) {
                        SaveWord();
                    }

                    tokens.Add(new SearchToken(SearchTokenType.PARAMETERS_START, ""));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                }

                // PARAMETERS_END )
                else if (i == ')') {
                    if (word.Length > 0) {
                        SaveWord();
                    }

                    tokens.Add(new SearchToken(SearchTokenType.PARAMETERS_END, ""));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                }

                // OR_CONTINUE
                else if (i == '|') {
                    if (word.Length > 0) {
                        SaveWord();
                    }

                    tokens.Add(new SearchToken(SearchTokenType.OR_CONTINUE, ""));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                }

                // OPERATORS that can exist in a word
                else if (i == '[' || i == ']') {
                    // if a word has already started, assume the bracket is part of the WORD, not an OPERATOR token
                    if (word.Length > 0) {
                        word += i;
                    } else {
                        // otherwise it's an OPERATOR token
                        tokens.Add(new SearchToken(SearchTokenType.OPERATOR, "" + i));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                    }
                }

                // OPERATORS
                else if (i == '<' || i == '>' || i == '*' || i == ':' || i == '=') {
                    if (word.Length > 0) {
                        SaveWord();
                    }

                    tokens.Add(new SearchToken(SearchTokenType.OPERATOR, "" + i));
                    _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                }

                // NOT
                else if (i == '!' || i == '-') {
                    // if a word has already started, assume the - is part of the WORD, not a NOT token (the same is not given to !)
                    if (i == '-' && word.Length > 0) {
                        word += i;
                    } else {
                        if (word.Length > 0) {
                            SaveWord();
                        }

                        tokens.Add(new SearchToken(SearchTokenType.NOT, "!"));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                    }
                }
                
                // word quote handling
                else if (i == '"') {
                    if (inQuote == true) {
                        _Logger.LogTrace($"ending quoted strong");
                        inQuote = false;
                    } else {
                        _Logger.LogTrace($"starting quoted string");
                        inQuote = true;
                    }

                    if (word.Length > 0) {
                        tokens.Add(new SearchToken(SearchTokenType.WORD, word));
                        _Logger.LogTrace($"new token [type={tokens.Last().Type}] [value={tokens.Last().Value}]");
                        word = "";
                    }

                }

                // WORD
                else {
                    word += i;
                }
            }

            if (word.Length > 0) {
                tokens.Add(new SearchToken(SearchTokenType.WORD, word));
            }

            tokens.Add(new SearchToken(SearchTokenType.END, ""));

            if (inQuote == true) {
                throw new System.Exception($"missing end quote");
            }

            return tokens;
        }

    }
}
