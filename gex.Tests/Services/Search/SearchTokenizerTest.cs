using gex.Models.Search;
using gex.Services.Search;
using gex.Tests.Util;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gex.Tests.Services.Search {

    [TestClass]
    public class SearchTokenizerTest {

        [DataTestMethod]
        [DataRow("hi howdy")]
        [DataRow("hi  howdy")]
        [DataRow(" hi howdy")]
        [DataRow("hi howdy ")]
        [DataRow(" hi  howdy")]
        [DataRow(" hi  howdy ")]
        [DataRow("                    hi                         howdy                         ")]
        public void Test_TokenizeAnd(string input) {

            List<SearchToken> tokens = _t(input);
            Assert.AreEqual(3, tokens.Count);

            List<SearchToken> expected = [
                new SearchToken(SearchTokenType.WORD, "hi"),
                new SearchToken(SearchTokenType.WORD, "howdy"),
                new SearchToken(SearchTokenType.END, ""),
            ];

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     test words with a - in them don't get split into WORD OPERATOR WORD WORD
        /// </summary>
        [DataTestMethod]
        [DataRow("hey-howdy -hi")]
        [DataRow(" hey-howdy - hi")]
        public void Test_TokenizeWordWithDashInIt(string input) {
            List<SearchToken> tokens = _t(input);

            Assert.AreEqual(4, tokens.Count); // include the END token

            List<SearchToken> expected = new() {
                new SearchToken(SearchTokenType.WORD, "hey-howdy"),
                new SearchToken(SearchTokenType.NOT, "!"),
                new SearchToken(SearchTokenType.WORD, "hi"),
                new SearchToken(SearchTokenType.END, "")
            };

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     test that the NOT operator is parsed correctly and ignores whitespace as needed
        /// </summary>
        [DataTestMethod]
        [DataRow("-hello")]
        [DataRow(" - hello ")]
        [DataRow("!hello")]
        [DataRow(" ! hello ")]
        public void SearchTokenizerTest_Not(string input) {
            List<SearchToken> tokens = _t(input);
            Assert.AreEqual(3, tokens.Count);

            List<SearchToken> expected = [
                new SearchToken(SearchTokenType.NOT, "!"),
                new SearchToken(SearchTokenType.WORD, "hello"),
                new SearchToken(SearchTokenType.END, "")
            ];

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     test tokenizing words and operators together
        /// </summary>
        [DataTestMethod]
        [DataRow("player: alice beth cat map:supreme mode : duel")]
        public void Test_TokenizeFields(string input) {
            List<SearchToken> tokens = _t(input);
            Assert.AreEqual(12, tokens.Count);

            List<SearchToken> expected = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.WORD, "beth"),
                new SearchToken(SearchTokenType.WORD, "cat"),
                new SearchToken(SearchTokenType.WORD, "map"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "supreme"),
                new SearchToken(SearchTokenType.WORD, "mode"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "duel"),
                new SearchToken(SearchTokenType.END, "")
            ];

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     test quoted strings
        /// </summary>
        [DataTestMethod]
        [DataRow("\"well hello there\"")]
        [DataRow("        \"well hello there\"")]
        [DataRow("\"well hello there\"              ")]
        [DataRow("             \"well hello there\"             ")]
        public void Test_TokenizeQuotedStringsKeptInOneWord(string input) {
            List<SearchToken> tokens = _t(input);

            List<SearchToken> expected = [
                new SearchToken(SearchTokenType.WORD, "well hello there"),
                new SearchToken(SearchTokenType.END, ""),
            ];

            Assert.AreEqual(expected.Count, tokens.Count);

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     test operators are parsed the same even with whitespace
        /// </summary>
        [DataTestMethod]
        [DataRow("<>*=:")]
        [DataRow("  <> * =  :")]
        [DataRow(" <     >*=         :")]
        public void Test_OperatorBreaks(string input) {
            List<SearchToken> tokens = _t(input);

            List<SearchToken> expected = [
                new SearchToken(SearchTokenType.OPERATOR, "<"),
                new SearchToken(SearchTokenType.OPERATOR, ">"),
                new SearchToken(SearchTokenType.OPERATOR, "*"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.END, ""),
            ];

            Assert.AreEqual(expected.Count, tokens.Count);

            _c(tokens, expected.ToArray());
        }

        /// <summary>
        ///     test the PARAMETERS tokens
        /// </summary>
        [TestMethod]
        public void Test_ParameterTokens() {
            List<SearchToken> tokens = _t("player:alice(air, won, os>15)");

            List<SearchToken> expected = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "air"),
                new SearchToken(SearchTokenType.WORD, "won"),
                new SearchToken(SearchTokenType.WORD, "os"),
                new SearchToken(SearchTokenType.OPERATOR, ">"),
                new SearchToken(SearchTokenType.WORD, "15"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ")"),
                new SearchToken(SearchTokenType.END, ""),
            ];
        }

        // ============================================================================================================
        // helper functions
        // ============================================================================================================

        /// <summary>
        ///     (c)ompare a list of tokens from the tokenizer and the expected tokens
        /// </summary>
        /// <param name="tokens">tokens from the tokenizer</param>
        /// <param name="expected">
        ///     hand-constructed array of tokens we expect to get.
        ///     this is an array to prevent mixing of the two parameters
        /// </param>
        private void _c(List<SearchToken> tokens, SearchToken[] expected) {
            Assert.AreEqual(expected.Length, tokens.Count); // double check the expected value is correct

            for (int i = 0; i < expected.Length; ++i) {
                SearchToken actual = tokens.ElementAt(i);
                SearchToken wanted = expected[i];

                Assert.AreEqual(wanted.Type, actual.Type);
                Assert.AreEqual(wanted.Value, actual.Value);
            }
        }

        /// <summary>
        ///     create a <see cref="SearchTokenizer"/> with a logger that prints to console,
        ///     and return the <see cref="Token"/>s it generates
        /// </summary>
        /// <param name="q">input search query</param>
        /// <returns>
        ///     a list of <see cref="Token"/>s
        /// </returns>
        private List<SearchToken> _t(string q) {
            TestLogger<SearchTokenizer> logger = new TestLogger<SearchTokenizer>();

            SearchTokenizer tokenizer = new(logger);

            List<SearchToken> tokens = tokenizer.Tokenize(q);

            Console.WriteLine($"got {tokens.Count} tokens from '{q}'");
            foreach (SearchToken t in tokens) {
                Console.Write($"\t{t.Type}");
                if (t.Type == SearchTokenType.WORD) {
                    Console.Write($"={t.Value}");
                }
                Console.WriteLine();
            }

            return tokens;
        }

    }
}
