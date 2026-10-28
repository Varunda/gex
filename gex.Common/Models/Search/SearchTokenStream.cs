using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace gex.Common.Models.Search {

    public class SearchTokenStream : IEnumerable<SearchToken>, IEnumerator<SearchToken> {

        public static readonly SearchToken DEFAULT_TOKEN = new SearchToken(SearchTokenType.DEFAULT, "");

        private readonly List<SearchToken> _SearchTokens;

        private int _Index = -1;

        private SearchToken _Current = DEFAULT_TOKEN;

        public SearchTokenStream(List<SearchToken> tokens) {
            _SearchTokens = tokens;
        }

        public SearchToken Current => _Current ?? throw new InvalidOperationException($"cannot iterate");

        object IEnumerator.Current => Current;

        public bool MoveNext() {
            if (_Index < _SearchTokens.Count - 1) {
                _Current = _SearchTokens.ElementAt(_Index + 1);
                ++_Index;
                return true;
            } else {
                _Current = DEFAULT_TOKEN;
                ++_Index;
                return false;
            }
        }

        /// <summary>
        ///     get the current token and advance to the next one
        /// </summary>
        /// <returns></returns>
        public SearchToken? GetNext() {
            if (MoveNext()) {
                return Current;
            }
            return null;
        }

        /// <summary>
        ///     peek at the next token without removing it from the stream
        /// </summary>
        /// <returns></returns>
        public SearchToken? PeekNext() {
            return Peek(1);
        }

        /// <summary>
        ///     peek at the next token <paramref name="count"/> away, without removing it from the stream
        /// </summary>
        /// <param name="count">how many tokens to look ahead</param>
        /// <returns>
        ///     the <see cref="SearchToken"/> that is <paramref name="count"/> after the current token,
        ///     or <c>null</c> if <paramref name="count"/> would go past the internal list
        /// </returns>
        public SearchToken? Peek(uint count) {
            if (_Index + count >= _SearchTokens.Count) {
                return null;
            }
            return _SearchTokens.ElementAt(_Index + (int)count);
        }

        /// <summary>
        ///     move back if possible in the token stream
        /// </summary>
        /// <returns></returns>
        public bool MoveBack() {
            if (--_Index >= 0) {
                _Current = _SearchTokens.ElementAt(_Index);
                return true;
            } else {
                _Current = DEFAULT_TOKEN;
                return false;
            }
        }

        /// <summary>
        ///     move back and get the previous token
        /// </summary>
        /// <returns></returns>
        public SearchToken? GetPrevious() {
            if (MoveBack()) {
                return Current;
            }
            return null;
        }

        public SearchToken? PeekPrevious() {
            if (_Index > 0) {
                return _SearchTokens.ElementAt(_Index - 1);
            }
            return null;
        }

        public void Reset() {
            _Index = 0;
        }

        public IEnumerator<SearchToken> GetEnumerator() => new List<SearchToken>(_SearchTokens).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => new List<SearchToken>(_SearchTokens).GetEnumerator();

        public void Dispose() {
            GC.SuppressFinalize(this);
        }

    }
}
