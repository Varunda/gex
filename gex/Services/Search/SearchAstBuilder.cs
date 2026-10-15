using gex.Code.Exceptions;
using gex.Code.ExtensionMethods;
using gex.Models.Search;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gex.Services.Search {
    public class SearchAstBuilder {

        private readonly ILogger<SearchAstBuilder> _Logger;

        public SearchAstBuilder(ILogger<SearchAstBuilder> logger) {
            _Logger = logger;
        }

        public SearchAst Build(List<SearchToken> input) {
            SearchNode root = new(SearchNodeType.AND, new SearchToken(SearchTokenType.DEFAULT, ""), null);

            if (input.Count == 0) {
                return new SearchAst(root);
            }

            SearchTokenStream tokens = new(input);
            tokens.MoveNext();

            SearchToken? iter = tokens.Current;
            Stack<SearchNode> stack = [];
            stack.Push(root);

            SearchToken? previousFieldName = null;
            SearchToken? previousFieldOp = null;

            SearchNode _c(SearchNodeType type, SearchToken token) => new SearchNode(type, token, stack.Peek());

            while (iter != null) {
                _Logger.LogTrace($"AST iteration [iter.Type={iter.Type}] [iter.Value={iter.Value}] [parent={stack.Peek().Type}] [parent.Children count={stack.Peek().Children.Count}]");

                if (iter.Type == SearchTokenType.WORD) {
                    if (tokens.PeekNext()?.Type == SearchTokenType.OR_CONTINUE) {
                        SearchNode or = _c(SearchNodeType.OR, new SearchToken(SearchTokenType.OR_CONTINUE, ""));
                        _Logger.LogTrace($"next token is an OR continue. pushing an OR continue on the stack");
                        stack.Push(or);
                        tokens.GetNext();
                    }

                    if (stack.Peek().Type == SearchNodeType.OR) {
                        SearchNode parent = stack.Peek();

                        if (parent.Children.Count > 0) {
                            SearchNode child = parent.Children[^1];
                            if (child.Type == SearchNodeType.FIELD) {
                                _Logger.LogTrace($"checking if field in OR is a different field name [child.Children[0]={child.Children[0].Type}/{child.Children[0].Token.Value}]");
                                if (child.Children[0].Type == SearchNodeType.FIELD_NAME
                                    && iter.Value != child.Children[0].Token.Value) {

                                    _Logger.LogTrace($"in an OR node, but this field doesn't match the previous one, popping OR [iter.Value={iter.Value}] [child field name={child.Token.Value}]");
                                    //stack.Pop();
                                }
                            }
                        }
                    }

                    SearchToken name = iter;
                    SearchToken? op = null;
                    SearchToken? value = null;

                    if (tokens.PeekNext()?.Type == SearchTokenType.PARAMETERS_START) {
                        tokens.GetNext();

                        SearchNode complexField = _c(SearchNodeType.FIELD, new SearchToken(SearchTokenType.FIELD, ""));
                        stack.Push(complexField);
                        _Logger.LogTrace($"pushing FIELD node to contain the name op, and value nodes");
                        _c(SearchNodeType.FIELD_NAME, iter);
                        _c(SearchNodeType.FIELD_OP, new SearchToken(SearchTokenType.OPERATOR, "="));

                        SearchNode fieldValue = _c(SearchNodeType.FIELD_VALUE, new SearchToken(SearchTokenType.PARAMETERS_START, ""));
                        _Logger.LogTrace($"pushing FIELD_VALUE node to contain the parameters");
                        stack.Push(fieldValue);

                        SearchNodeType type = SearchNodeType.AND;
                        if (tokens.PeekNext()?.Type == SearchTokenType.OR_CONTINUE) {
                            type = SearchNodeType.OR;
                            tokens.GetNext();
                        }

                        _Logger.LogTrace($"pushing a node to contain the parameters");
                        stack.Push(_c(type, iter));
                        iter = tokens.GetNext();

                        _Logger.LogTrace($"WORD next token is PARAMETERS_START, beginning a complex field");
                        continue;
                    }

                    // implicitly use the previous field name if not given, allowing things like:
                    //      players: alice beth
                    // which will create two FIELD nodes, player=alice & player=beth
                    if (previousFieldOp != null && previousFieldName != null && tokens.PeekNext()?.Type != SearchTokenType.OPERATOR) {
                        op = previousFieldOp;

                        if (stack.Peek().Type == SearchNodeType.NOT) {
                            op = new SearchToken(SearchTokenType.OPERATOR, OperatorNegations.GetValueOrDefault(previousFieldOp.Value)
                                ?? throw new SearchAstParseException(iter, $"missing negation for operator '{op.Value}' (only !, =, [ and ] have negations)")
                            );
                            stack.Pop();
                        }

                        value = iter;
                        name = previousFieldName;

                        _Logger.LogTrace($"using implicit field name [field name={name.Value}] [field value={value.Value}]");
                    } else if (tokens.PeekNext()?.Type == SearchTokenType.NOT || tokens.PeekNext()?.Type == SearchTokenType.OPERATOR) {
                        bool negateOperator = tokens.PeekNext()!.Type == SearchTokenType.NOT;
                        if (negateOperator) {
                            _Logger.LogTrace($"next token is a NOT, getting it out of the stream, and negating the next OPERATOR");
                            tokens.GetNext();
                        }

                        op = tokens.GetNext() ?? throw new SearchAstParseException(iter, $"failed to get OPERATOR token from next token");
                        if (negateOperator == true) {
                            op.Value = new string(
                                OperatorNegations.GetValueOrDefault(op.Value)
                                ?? throw new SearchAstParseException(iter, $"missing negation for operator '{op.Value}' (only !, =, [ and ] have negations)")
                            );
                        }

                        SearchToken valueToken = tokens.GetNext()
                            ?? throw new SearchAstParseException(iter, $"missing value token when an OPERATOR token was provided");

                        if (valueToken.Type == SearchTokenType.NOT) {
                            _Logger.LogTrace($"token after the OPERATOR is NOT, negating the OPERATOR and advancing the stream for next WORD");
                            op.Value = OperatorNegations.GetValueOrDefault(op.Value)
                                ?? throw new SearchAstParseException(iter, $"missing negation for operator '{op.Value}' (only !, =, [ and ] have negations)");

                            valueToken = tokens.GetNext()
                                ?? throw new SearchAstParseException(iter, $"missing token after NOT token in FIELD node");
                        }

                        if (valueToken.Type != SearchTokenType.WORD) {
                            throw new SearchAstParseException(iter, $"missing WORD token after OPERATOR token");
                        }

                        value = valueToken;

                        previousFieldName = name;
                        previousFieldOp = op;
                    } else {
                        op = new SearchToken(SearchTokenType.OPERATOR, "=");
                        value = new SearchToken(SearchTokenType.OPERATOR, "true");
                    }

                    if (op == null || value == null) {
                        throw new SearchAstParseException(iter, $"missing operator or value for FIELD node (has op {op != null}) (has value {value != null})");
                    }

                    name.Value = _FieldNameAlias(name.Value);

                    if (op.Value == ":") {
                        op.Value = "=";
                    }

                    SearchNode field = _c(SearchNodeType.FIELD, new SearchToken(SearchTokenType.FIELD, ""));
                    stack.Push(field);
                    _Logger.LogTrace($"pushing FIELD node to contain the name, op and value");
                    _c(SearchNodeType.FIELD_NAME, name);
                    _c(SearchNodeType.FIELD_OP, op);
                    _c(SearchNodeType.FIELD_VALUE, value);
                    stack.Pop();

                    _Logger.LogTrace($"created field [name={name.Value}] [op={op.Value}] [value={value.Value}]");

                    if (stack.Peek().Type == SearchNodeType.OR) {
                        SearchToken? next = tokens.PeekNext();
                        if (next != null && next.Type != SearchTokenType.OR_CONTINUE && next.Type != SearchTokenType.OR_END) {
                            _Logger.LogTrace($"after FIELD, next token isn't an OR_CONTINUE, and in an OR, popping the OR off");
                            stack.Pop();
                        }
                    }
                }

                // PARAMETERS_START
                else if (iter.Type == SearchTokenType.PARAMETERS_START) {
                    // only occurs when an implicit name field has been given to a FIELD token

                    SearchNode parent = stack.Peek();
                    if (parent.Children.Count == 0) {
                        throw new SearchAstParseException(iter, $"got a PARAMETERS_START when the parent had no children");
                    }

                    SearchNode child = parent.Children[^1];
                    if (child.Type != SearchNodeType.FIELD) {
                        throw new SearchAstParseException(iter, $"expected a FIELD node when using an implicit name on a complex field (was a {child.Type}) instead");
                    }

                    if (child.Children.Count != 3) {
                        throw new SearchAstParseException(iter, $"expected FIELD node to contain 3 children (had {child.Children.Count} instead)");
                    }

                    // player:alice(position:air) => player(name:alice & position:air)
                    // take FIELD VALUE, push to the FIELD node with a new FIELD_NAME implicitly of name

                    SearchNode fieldName = child.Children[0];
                    SearchNode fieldOp = child.Children[1];
                    SearchNode fieldValue = child.Children[2];

                    // remove the OP and VALUE children of the FIELD
                    child.Children.RemoveAt(2);
                    child.Children.RemoveAt(1);

                    stack.Push(child);

                    _c(SearchNodeType.FIELD_OP, new SearchToken(SearchTokenType.OPERATOR, "="));
                    stack.Push(_c(SearchNodeType.FIELD_VALUE, new SearchToken(SearchTokenType.PARAMETERS_START, "")));
                    stack.Push(_c(SearchNodeType.AND, iter));

                    SearchNode nameNode = _c(SearchNodeType.FIELD, child.Token);
                    stack.Push(nameNode);

                    _Logger.LogTrace($"for PARAMETERS_START, pushed FIELD, FIELD_VALUE, AND, then FIELD");

                    SearchNode name = _c(SearchNodeType.FIELD_NAME, new SearchToken(SearchTokenType.WORD, "name"));
                    _c(SearchNodeType.FIELD_OP, fieldOp.Token);
                    _c(SearchNodeType.FIELD_VALUE, fieldValue.Token);
                    stack.Pop();
                    _Logger.LogTrace($"for PARAMETERS_START, popped FIELD off");
                }

                // PARAMETERS_END
                else if (iter.Type == SearchTokenType.PARAMETERS_END) {
                    // if the top node is an AND, implicitly assume an end and pop it off too
                    if (stack.Peek().Type == SearchNodeType.OR) {
                        stack.Pop();
                    }

                    SearchNode and = stack.Pop(); // pop the AND off
                    SearchNode field = stack.Pop(); // pop the FIELD off
                    SearchNode fieldValue = stack.Pop(); // pop the FIELD_VALUE off
                    _Logger.LogTrace($"got PARAMETERS_END, popping 3 [value={fieldValue.Type}] [field={field.Type}] [and={and.Type}]");
                }

                // OR_START
                else if (iter.Type == SearchTokenType.OR_START) {
                    stack.Push(_c(SearchNodeType.OR, iter));
                    _Logger.LogTrace($"pushed OR node because of OR_START token");
                }

                // OR_CONTINUE
                else if (iter.Type == SearchTokenType.OR_CONTINUE) {
                    _Logger.LogTrace($"got OR_CONTINUE");

                    SearchNode parent = stack.Peek();
                    if (parent.Type == SearchNodeType.AND) {
                        _Logger.LogTrace($"this is the first child of an AND node, pushing an OR node to stack and taking a child if needed");

                        // parent is set AFTER the child check is done
                        SearchNode or = new(SearchNodeType.OR, SearchToken.OR_START, null);

                        if (parent.Children.Count > 0) {
                            // if the parent has at least one child, and the last child has the same field name,
                            // create a new OR node, and take the last child of the parent
                            SearchNode child = parent.Children[^1];
                            if (child.Type == SearchNodeType.FIELD) {
                                if (child.Children[0].Type == SearchNodeType.FIELD_NAME
                                    && tokens.PeekNext()?.Type == SearchTokenType.WORD) { 

                                    parent.Children.RemoveAt(parent.Children.Count - 1);

                                    _Logger.LogTrace($"pushing OR node as parent's child last node is a FIELD_NAME and the value doesn't match");
                                    or.Children.Add(child);
                                    child.Parent = or;
                                }
                            }
                        }

                        parent.Children.Add(or);
                        or.Parent = parent;
                        stack.Push(or);
                    }
                }

                // OR_END
                else if (iter.Type == SearchTokenType.OR_END) {
                    SearchNode parent = stack.Peek();

                    if (parent.Type != SearchNodeType.OR) {
                        throw new SearchAstParseException(iter, $"expected parent node to be of OR type, was a {parent.Type} instead");
                    }

                    _Logger.LogTrace($"got OR_END, popping");
                    stack.Pop();
                }

                // NOT
                else if (iter.Type == SearchTokenType.NOT) {
                    SearchNode not = new(SearchNodeType.NOT, iter, null);
                    stack.Push(not);
                    _Logger.LogTrace($"pushing NOT node to be used on whatever is next");
                }

                // END
                else if (iter.Type == SearchTokenType.END) {
                    break;
                }

                // fallback
                else {
                    throw new NotImplementedException($"unhandled token type: {iter.Type}");
                }

                iter = tokens.GetNext();
            }

            if (tokens.MoveNext() == true) {
                throw new SearchAstParseException(null, $"invalid token list: expected MoveNext to fail after consuming all tokens, but have {tokens.Current}");
            }

            // for an OR node without explicit braces, assume the OR node ends at the end of tokens
            if (stack.Count == 2 && stack.Peek().Type == SearchNodeType.OR) {
                stack.Pop();
            }

            if (stack.Count > 1) {
                throw new SearchAstParseException(null, $"invalid token list: expected one {nameof(SearchNode)} in stack after consuming all tokens, had {stack.Count} instead");
            }

            SearchNode? treeRoot = stack.PopOrDefault();
            if (treeRoot == null) {
                throw new SearchAstParseException(null, $"failed to get root tree node, stack has no nodes left on it (did a double pop occur?)");
            }

            // it's fine for like a NOT node to have 0 children, as it's the only node
            if ((treeRoot.Type == SearchNodeType.AND || treeRoot.Type == SearchNodeType.OR) && treeRoot.Children.Count == 0) {
                throw new SearchAstParseException(null, $"invalid token list: expected root {nameof(SearchNode)} to have >0 children, had {treeRoot.Children.Count} instead");
            }

            return new SearchAst(treeRoot);
        }

        /// <summary>
        ///     helper function to create a Node, and add it to the <see cref="SearchNode.Children"/> of <paramref name="parent"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="token"></param>
        /// <param name="parent"></param>
        /// <returns>
        ///     the newly created node
        /// </returns>
        private static SearchNode _c(SearchNodeType type, SearchToken token, SearchNode parent) => new SearchNode(type, token, parent);

        private static HashSet<string> ComplexFieldNames = ["player", "map"];

        /// <summary>
        ///     list of alises for field names
        /// </summary>
        private static readonly Dictionary<string, string> FieldNameAliases = new() {
            // player
            { "p", "player" },
            { "players", "player" },

            // engine
            { "e", "engine" },
            { "eng", "engine" },
            { "engines", "engine" },
            
            // map
            { "m", "map" },
            { "maps", "map" },

            // gamemode
            { "gm", "gamemode" },
            { "mode", "gamemode" }
        };

        /// <summary>
        ///     when a NOT token shows up, it negates the next operator or previous operator if the next token is a WORD
        /// </summary>
        private static readonly Dictionary<string, string> OperatorNegations = new() {
            // equals => not equals
            { ":", "!" },
            { "=", "!" },

            { "[", ">" }, // less than or equal => greater than
            { "]", "<" }, // greater than or equal <= less than
        };

        /// <summary>
        ///     get the normalized field name if aliased
        /// </summary>
        /// <param name="fieldName">field name, may or may not be an alias</param>
        private static string _FieldNameAlias(string fieldName) => FieldNameAliases.GetValueOrDefault(fieldName) ?? fieldName;

        private SearchNode _MakeMetaNode(SearchToken iter, SearchNode parent, SearchTokenStream tokens) {
            _Logger.LogTrace($"look-ahead found META token");
            SearchToken meta = tokens.GetNext() ?? throw new Exception("something is fucked");

            // an operator can optionally be set, but defaults to =
            // for example, width:>10 has the tokens as "width", ">", "10"
            SearchToken? opOrValue = tokens.GetNext();
            if (opOrValue == null) {
                throw new SearchAstParseException(iter, $"look ahead failed: no token after META token");
            }

            SearchToken? op = null;
            SearchToken? value = null;
            if (opOrValue.Type == SearchTokenType.OPERATOR) { // a colon
                _Logger.LogTrace($"token is OPERATOR, checking for WORD [type={opOrValue.Type}] [value={opOrValue.Value}]");
                op = opOrValue;
                value = tokens.GetNext();
                if (value == null) { // expect a token after the colon (prevents something like width:>)
                    throw new SearchAstParseException(iter, $"look ahead failed: no token after OPERATOR token (for META)");
                }
                if (value.Type != SearchTokenType.WORD) {
                    throw new SearchAstParseException(iter, $"look ahead failed: expected WORD token after OPERATOR token (for META), got {value.Type} instead");
                }
            } else if (opOrValue.Type == SearchTokenType.WORD) {
                // assume if no operator is given, that the = operator is used
                op = new SearchToken(SearchTokenType.OPERATOR, "=");
                value = opOrValue;
            } else {
                throw new SearchAstParseException(iter, $"look ahead failed: expected WORD token after META token, got {opOrValue.Type} instead");
            }

            _Logger.LogTrace($"creating META node [field={iter.Value}] [operator='{op.Value}'] [value='{value.Value}']");

            SearchNode metaParent = _c(SearchNodeType.FIELD, meta, parent);
            _c(SearchNodeType.FIELD_NAME, iter, metaParent);
            _c(SearchNodeType.FIELD_OP, op, metaParent);
            _c(SearchNodeType.FIELD_VALUE, value, metaParent);

            return metaParent;
        }

    }
}
