using gex.Code.Exceptions;
using gex.Models.Search;
using gex.Services.Search;
using gex.Tests.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gex.Tests.Services.Search {

    [TestClass]
    public class SearchAstBuilderTest {

        /// <summary>
        ///     just a basic field=blah
        /// </summary>
        [TestMethod]
        public void Test_BasicField() {
            // engine:105
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure a field without an operator or value defaults =true
        /// </summary>
        [TestMethod]
        public void Test_BasicField_ImplicitTrue() {
            // engine
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "true"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure two fields without an operator or value default to =true
        /// </summary>
        [TestMethod]
        public void Test_TwoBasicField_ImplicitTrue() {
            // engine gamemode
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.WORD, "gamemode"),
            ];

            SearchAst ast = _a(tokens);

            // engine=true AND gamemode=true
            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "true"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "gamemode"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "true"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure that both a field name of 'engines' and 'e' are alised to 'engine'
        /// </summary>
        [TestMethod]
        public void Test_BasicField_WithAlias() {
            // engines: 105
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engines"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
            ];

            List<Action<SearchNode>> children = [
                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                ]),
            ];

            SearchAst ast1 = _a(tokens);
            _CheckNodeTree(ast1.Root, SearchNodeType.AND, children);

            tokens[0].Value = "e";
            SearchAst ast2 = _a(tokens);
            _CheckNodeTree(ast2.Root, SearchNodeType.AND, children);
        }

        /// <summary>
        ///     ensure two fields back to back work
        /// </summary>
        [TestMethod]
        public void Test_SimpleFields() {
            // engine: 105 engine: 104
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.WORD, "104")
            ];

            SearchAst ast1 = _a(tokens);

            // engine=105 AND engine=104
            _CheckNodeTree(ast1.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "104"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure implict field names are carried across fields
        /// </summary>
        [TestMethod]
        public void Test_BasicField_WithImplicitFieldName() {
            // engine: 105 104
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
                new SearchToken(SearchTokenType.WORD, "104")
            ];

            SearchAst ast1 = _a(tokens);

            // engine=105 AND engine=104
            _CheckNodeTree(ast1.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "104"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure implict field names are carried across fields, with 2 fields
        /// </summary>
        [TestMethod]
        public void Test_TwoField_WithImplicitFieldName() {
            // engine: 105 104 min_os=10 15
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
                new SearchToken(SearchTokenType.WORD, "104"),
                new SearchToken(SearchTokenType.WORD, "min_os"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.WORD, "10"),
                new SearchToken(SearchTokenType.WORD, "15"),
            ];

            SearchAst ast1 = _a(tokens);

            // engine=105 AND engine=104 AND min_os=10 AND min_os=15
            _CheckNodeTree(ast1.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "104"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "min_os"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "10"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "min_os"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "15"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure a complex field without any the implicit name field is parsed
        /// </summary>
        [TestMethod]
        public void Test_ComplexField_NoImplicitNameField() {
            // player(name=alice position:air)
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "name"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.WORD, "position"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "air"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ""),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD_VALUE, [
                            (node) => _CheckNodeTree(node, SearchNodeType.AND, [
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "name"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "alice"),
                                ]),
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "position"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "air"),
                                ])
                            ])
                        ]),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure a complex field without any the implicit name field is parsed
        /// </summary>
        [TestMethod]
        public void Test_ComplexField_NoImplicitNameField_UsingOr() {
            // player(name=alice | position:air)
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "name"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.OR_CONTINUE, ""),
                new SearchToken(SearchTokenType.WORD, "position"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "air"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ""),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    // player=(name=alice OR position=air)
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD_VALUE, [
                            (node) => _CheckNodeTree(node, SearchNodeType.AND, [
                                (node) => _CheckNodeTree(node, SearchNodeType.OR, [
                                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "name"),
                                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "alice"),
                                    ]),
                                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "position"),
                                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "air"),
                                    ])
                                ])
                            ])
                        ]),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure a complex field without any the implicit name field is parsed
        /// </summary>
        [TestMethod]
        public void Test_BasicField_And_ComplexField_NoImplicitNameField() {
            // engine=105 player(name=alice position:air)
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.WORD, "105"),
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "name"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.WORD, "position"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "air"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ""),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    // engine=105
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),

                    // player=(name=alice AND position=air)
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD_VALUE, [
                            (node) => _CheckNodeTree(node, SearchNodeType.AND, [
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "name"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "alice"),
                                ]),
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "position"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "air"),
                                ])
                            ])
                        ]),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure a complex field without any the implicit name field followed by a basic field is parsed
        /// </summary>
        [TestMethod]
        public void Test_ComplexField_NoImplicitNameField_And_BasicField() {
            // player(name=alice position:air) engine=105
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "name"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.WORD, "position"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "air"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ""),
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.WORD, "105"),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    // player=(name=alice AND position=air)
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD_VALUE, [
                            (node) => _CheckNodeTree(node, SearchNodeType.AND, [
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "name"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "alice"),
                                ]),
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "position"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "air"),
                                ])
                            ])
                        ]),
                    ]),

                    // engine=105
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure a complex field without any the implicit name field followed by a basic field is parsed
        /// </summary>
        [TestMethod]
        public void Test_ComplexField_WithImplicitNameField() {
            // player:alice(position:air)
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "position"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "air"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ""),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    // player=(name=alice AND position=air)
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD_VALUE, [
                            (node) => _CheckNodeTree(node, SearchNodeType.AND, [
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "name"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "alice"),
                                ]),
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "position"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "air"),
                                ])
                            ])
                        ]),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure a complex field without any the implicit name field followed by a basic field is parsed
        /// </summary>
        [TestMethod]
        public void Test_TwoComplexField_WithImplicitNameField() {
            // player:alice(position:air) player:beth(position:air)
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "position"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "air"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ""),
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "beth"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "position"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "front"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ""),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    // player=(name=alice AND position=air)
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD_VALUE, [
                            (node) => _CheckNodeTree(node, SearchNodeType.AND, [
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "name"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "alice"),
                                ]),
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "position"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "air"),
                                ])
                            ])
                        ]),
                    ]),

                    // player=(name=beth AND position=front)
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD_VALUE, [
                            (node) => _CheckNodeTree(node, SearchNodeType.AND, [
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "name"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "beth"),
                                ]),
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "position"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "front"),
                                ])
                            ])
                        ]),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure a complex field without any the implicit name field followed by a basic field is parsed
        /// </summary>
        [TestMethod]
        public void Test_TwoComplexField_WithImplicitNameField_AndOneWithout() {
            // player(name:alice position:air) player:beth(position:air)
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "name"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.WORD, "position"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "air"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ""),
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "beth"),
                new SearchToken(SearchTokenType.PARAMETERS_START, ""),
                new SearchToken(SearchTokenType.WORD, "position"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "front"),
                new SearchToken(SearchTokenType.PARAMETERS_END, ""),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    // player=(name=alice AND position=air)
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD_VALUE, [
                            (node) => _CheckNodeTree(node, SearchNodeType.AND, [
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "name"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "alice"),
                                ]),
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "position"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "air"),
                                ])
                            ])
                        ]),
                    ]),

                    // player=(name=beth AND position=front)
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD_VALUE, [
                            (node) => _CheckNodeTree(node, SearchNodeType.AND, [
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "name"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "beth"),
                                ]),
                                (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "position"),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                                    (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "front"),
                                ])
                            ])
                        ]),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure simple OR
        /// </summary>
        [TestMethod]
        public void Test_BasicField_Or() {
            // engine:105 | engine:104
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
                new SearchToken(SearchTokenType.OR_CONTINUE, "|"),
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "104"),
            ];

            SearchAst ast1 = _a(tokens);

            _CheckNodeTree(ast1.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.OR, [
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                        ]),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "104"),
                        ]),
                    ])
                ]
            );
        }

        /// <summary>
        ///     ensure OR between 3 fields
        /// </summary>
        [TestMethod]
        public void Test_BasicField_Or_3_WithBraces() {
            // { engine:105 | engine:104 | engine:103 }
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.OR_START, ""),
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
                new SearchToken(SearchTokenType.OR_CONTINUE, "|"),
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "104"),
                new SearchToken(SearchTokenType.OR_CONTINUE, "|"),
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "103"),
                new SearchToken(SearchTokenType.OR_END, ""),
            ];

            SearchAst ast1 = _a(tokens);

            _CheckNodeTree(ast1.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.OR, [
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                        ]),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "104"),
                        ]),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "103"),
                        ]),
                    ])
                ]
            );
        }

        /// <summary>
        ///     ensure OR between 3 fields
        /// </summary>
        [TestMethod]
        public void Test_BasicField_Or_3_NoBraces() {
            // engine:105 | engine:104 | engine:103
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
                new SearchToken(SearchTokenType.OR_CONTINUE, "|"),
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "104"),
                new SearchToken(SearchTokenType.OR_CONTINUE, "|"),
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "103"),
            ];

            SearchAst ast1 = _a(tokens);

            _CheckNodeTree(ast1.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.OR, [
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                        ]),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "104"),
                        ]),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "103"),
                        ]),
                    ])
                ]
            );
        }

        /// <summary>
        ///     ensure an AND followed by an OR parses
        /// </summary>
        [TestMethod]
        public void Test_BasicField_AndThenOr() {
            // engine:105 gamemode:duel | gamemode:small
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
                new SearchToken(SearchTokenType.WORD, "gamemode"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "duel"),
                new SearchToken(SearchTokenType.OR_CONTINUE, "|"),
                new SearchToken(SearchTokenType.WORD, "gamemode"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "small"),
            ];

            SearchAst ast1 = _a(tokens);

            _CheckNodeTree(ast1.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.OR, [
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "gamemode"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "duel"),
                        ]),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "gamemode"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "small"),
                        ]),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     ensure an AND followed by an OR parses
        /// </summary>
        [TestMethod]
        public void Test_BasicField_OrThenAnd() {
            // gamemode:duel | gamemode:small engine=105
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "gamemode"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "duel"),
                new SearchToken(SearchTokenType.OR_CONTINUE, "|"),
                new SearchToken(SearchTokenType.WORD, "gamemode"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "small"),
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "105"),
            ];

            SearchAst ast1 = _a(tokens);

            _CheckNodeTree(ast1.Root,
                SearchNodeType.AND,
                [
                    // gamemode=duel | gamemode=small
                    (node) => _CheckNodeTree(node, SearchNodeType.OR, [
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "gamemode"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "duel"),
                        ]),
                        (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                            (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "gamemode"),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                            (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "small"),
                        ]),
                    ]),

                    // engine=105
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     test NOT before an operator
        /// </summary>
        [TestMethod]
        public void Test_Not_BeforeOperator_SingleField() {
            // engine != 105
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.NOT, "!"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.WORD, "105"),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "!"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     test NOT after an operator
        /// </summary>
        [TestMethod]
        public void Test_Not_AfterOperator_SingleField() {
            // engine=!105
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "engine"),
                new SearchToken(SearchTokenType.OPERATOR, "="),
                new SearchToken(SearchTokenType.NOT, "!"),
                new SearchToken(SearchTokenType.WORD, "105"),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "engine"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "!"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "105"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     test a NOT within an AND node is correctly interpreted
        /// </summary>
        [TestMethod]
        public void Test_BasicFieldName_WithNegation() {
            // player: alice beth ! cat map:supreme mode: duel
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.WORD, "beth"),
                new SearchToken(SearchTokenType.NOT, "!"),
                new SearchToken(SearchTokenType.WORD, "cat"),
                new SearchToken(SearchTokenType.WORD, "map"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "supreme"),
                new SearchToken(SearchTokenType.WORD, "mode"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "duel"),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "alice"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "beth"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "!"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "cat"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "map"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "supreme"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "gamemode"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "duel"),
                    ]),
                ]
            );
        }

        /// <summary>
        ///     test a NOT within an AND node is correctly interpreted
        /// </summary>
        [TestMethod]
        public void Test_1() {
            // player: alice beth ! cat map:supreme mode: duel
            List<SearchToken> tokens = [
                new SearchToken(SearchTokenType.WORD, "player"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "alice"),
                new SearchToken(SearchTokenType.WORD, "beth"),
                new SearchToken(SearchTokenType.NOT, "!"),
                new SearchToken(SearchTokenType.WORD, "cat"),
                new SearchToken(SearchTokenType.WORD, "map"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "supreme"),
                new SearchToken(SearchTokenType.WORD, "mode"),
                new SearchToken(SearchTokenType.OPERATOR, ":"),
                new SearchToken(SearchTokenType.WORD, "duel"),
            ];

            SearchAst ast = _a(tokens);

            _CheckNodeTree(ast.Root,
                SearchNodeType.AND,
                [
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "alice"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "beth"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "player"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "!"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "cat"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "map"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "supreme"),
                    ]),
                    (node) => _CheckNodeTree(node, SearchNodeType.FIELD, [
                        (node) => _CheckNode(node, SearchNodeType.FIELD_NAME, "gamemode"),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_OP, "="),
                        (node) => _CheckNode(node, SearchNodeType.FIELD_VALUE, "duel"),
                    ]),
                ]
            );
        }

        // ============================================================================================================
        // helper functions
        // ============================================================================================================

        /// <summary>
        ///     helper function to build an AST tree
        /// </summary>
        /// <param name="tokens">tokens used to build the AST</param>
        /// <returns></returns>
        private SearchAst _a(List<SearchToken> tokens) {
            Console.WriteLine($"AST generating from: [{string.Join(", ", tokens.Select(iter => $"{iter.Type}{(iter.Value != "" ? (":" + iter.Value): "")}"))}]");
            Console.WriteLine($"{string.Join(" ", tokens.Select(iter => $"{(iter.Value == "" ? iter.Type : iter.Value)}"))}");

            SearchAstBuilder builder = new SearchAstBuilder(new TestLogger<SearchAstBuilder>());
            SearchAst ast = builder.Build(tokens);

            Console.WriteLine($"parsed AST: {ast.Print()}");
            _PrintAst(ast);

            Assert.IsNull(ast.Root.Parent);

            return ast;
        }

        /// <summary>
        ///     helper function to validate the nodes in an AST. does NOT check children, ONLY the order
        /// </summary>
        /// <param name="ast"></param>
        /// <param name="expected"></param>
        private void _CheckNodeOrder(SearchAst ast, List<NodeIter> expected) {

            int count = 0;
            IEnumerator<SearchNode> astNodes = ast.GetEnumerator();
            while (astNodes.MoveNext()) {
                ++count;
            }

            Console.WriteLine($"have {count} nodes from the AST");

            Assert.AreEqual(expected.Count, count);

            int i = 0;
            Console.WriteLine($"nodes within AST tree:");
            foreach (SearchNode node in ast) {
                Assert.IsTrue(i < expected.Count, $"i is {i}, and {i} < {expected.Count} is true, which means we've iterated past the end of the AST");

                NodeIter ex = expected.ElementAt(i);

                Assert.AreEqual(ex.Type, node.Type);
                Assert.AreEqual(ex.Token.Type, node.Token.Type);
                Assert.AreEqual(ex.Token.Value, node.Token.Value);

                Console.WriteLine($"{new string('\t', node.Depth + 1)}{node.Type} {node.Token.Type} {node.Token.Value}");

                ++i;
            }
        }

        private void _CheckNodeTree(SearchNode node, SearchNodeType type, List<Action<SearchNode>> childrenChecks) {
            Assert.AreEqual(type, node.Type);
            Assert.AreEqual(childrenChecks.Count, node.Children.Count, $"expected {childrenChecks.Count} children in {node}, has {node.Children.Count} instead");

            for (int i = 0; i < node.Children.Count; ++i) {
                SearchNode childNode = node.Children.ElementAt(i);
                Action<SearchNode> check = childrenChecks.ElementAt(i);

                check(childNode);
            }
        }

        private void _CheckNode(SearchNode node, SearchNodeType type, string value) {
            Assert.AreEqual(type, node.Type);
            Assert.AreEqual(value, node.Token.Value);
            Assert.AreEqual(0, node.Children.Count);
        }

        private void _PrintAst(SearchAst ast) {
            foreach (SearchNode node in ast) {
                Console.WriteLine($"{new string('\t', node.Depth + 1)}{node.Type} ({node.Token.Type}) {node.Token.Value}");
            }
        }

        private class NodeIter {

            public NodeIter(SearchNodeType type, SearchToken token) {
                Type = type;
                Token = token;
            }

            public NodeIter(SearchNode node) {
                Type = node.Type;
                Token = node.Token;
            }

            public SearchNodeType Type { get; set; }

            public SearchToken Token { get; set; }

        }


    }
}
