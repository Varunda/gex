using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace gex.Models.Search {
    public class SearchAst : IEnumerable<SearchNode> {

        public SearchAst(SearchNode root) {
            this.Root = root;
        }

        public SearchNode Root { get; set; }

        /// <summary>
        ///     get a <see cref="IEnumerable{T}"/> that iterates thru the <see cref="Node"/>s starting from the left
        /// </summary>
        /// <returns></returns>
        public IEnumerator<SearchNode> GetEnumerator() {
            List<SearchNode> nodes = _BuildEnumerable(Root, new List<SearchNode>());

            return nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

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
                sb.Append('\n');
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
