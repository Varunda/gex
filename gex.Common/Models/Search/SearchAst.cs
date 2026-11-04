using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gex.Common.Models.Search {
    public class SearchAst {

        public SearchAst(SearchNode root) {
            this.Root = root;
        }

        public SearchNode Root { get; set; }

        private List<SearchNode> _BuildEnumerable(SearchNode node, List<SearchNode> list) {
            list.Add(node);

            foreach (SearchNode child in node.Children) {
                _BuildEnumerable(child, list);
            }

            return list;
        }

        public string Print(bool indented = false) {
            if (indented) {
                return _PrintNodeIndent(Root, 0);
            } else {
                return _PrintNode(Root);
            }
        }

        public override string ToString() {
            return _ToString(Root);
        }

        private string _ToString(SearchNode node) {
            StringBuilder sb = new();
            if (node.Children.Count > 0) {
                if (node.Type != SearchNodeType.BASIC_FIELD && node.Type != SearchNodeType.COMPLEX_FIELD) {
                    sb.Append('(');
                }
                sb.Append(string.Join(node.Type == SearchNodeType.AND ? " AND " : node.Type == SearchNodeType.OR ? " OR " : " ", node.Children.Select(_ToString)));
                if (node.Type != SearchNodeType.BASIC_FIELD && node.Type != SearchNodeType.COMPLEX_FIELD) {
                    sb.Append(')');
                }
            } else {
                if (node.Type == SearchNodeType.AND) {
                    sb.Append(" AND ");
                } else if (node.Type == SearchNodeType.OR) {
                    sb.Append(" OR ");
                }

                sb.Append(node.Token.Value);
            }

            return sb.ToString();
        }

        private string _PrintNode(SearchNode node, int depth = 0) {
            if (node.Type == SearchNodeType.FIELD_VALUE) {
                return node.Token.Value;
            }

            StringBuilder sb = new();
            sb.Append(node.Type.ToString().ToUpper());
            sb.Append(' ');

            if (node.Children.Count > 0) {
                sb.Append('(');
                sb.Append(string.Join(", ", node.Children.Select(iter => _PrintNode(iter, depth + 1))));
                sb.Append(')');
            } else {
                sb.Append(node.Token.Value);
            }

            return sb.ToString();
        }

        private string _PrintNodeIndent(SearchNode node, int depth = 0) {
            StringBuilder sb = new();
            for (int i = 0; i < depth; ++i) { sb.Append('\t'); }
            sb.Append(node.Type.ToString().ToUpper());
            sb.Append('=');

            if (node.Children.Count > 0) {
                sb.Append('(');
                sb.Append('\n');
                sb.Append(string.Join(" ", node.Children.Select(iter => _PrintNodeIndent(iter, depth + 1))));
                //sb.Append('\n');
                for (int i = 0; i < depth; ++i) { sb.Append('\t'); }
                sb.Append(')');
                sb.Append('\n');
            } else {
                sb.Append("'" + node.Token.Value + "'");
                sb.Append('\n');
            }

            return sb.ToString();
        }

        public override int GetHashCode() {
            return Root.GetHashCode();
        }

    }
}
