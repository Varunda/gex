using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace gex.Common.Models.Search {

    public class SearchNode {

        public SearchNode(SearchNodeType type, SearchToken token, SearchNode? parent) {
            if (parent is not null && parent == this) {
                throw new InvalidOperationException($"a node cannot be its own parent");
            }

            Type = type;
            Token = token;
            Parent = parent;

            if (Parent is not null) {
                Parent.Children.Add(this);
            }
        }

        public SearchNodeType Type { get; set; }

        public SearchToken Token { get; set; }

        [JsonIgnore]
        private SearchNode? _parent = null;
        public SearchNode? Parent {
            get => _parent;
            set {
                _Depth = null;
                _parent = value;
            }
        }

        public List<SearchNode> Children { get; set; } = [];

        private int? _Depth = null;
        public int Depth {
            get {
                if (_Depth != null) {
                    return _Depth.Value;
                }

                SearchNode? iter = Parent;

                int i = 0;
                while (iter != null) {
                    ++i;
                    iter = iter.Parent;
                }

                _Depth = i;

                return _Depth.Value;
            }
        }

        public override string ToString() {
            return $"<{nameof(SearchNode)} [{nameof(Type)}={Type}] [Token={Token}] [Children.count={Children.Count}] [has parent={Parent != null}]>";
        }

        public override int GetHashCode() {
            int hash = HashCode.Combine((int)Type, Token);

            foreach (SearchNode node in Children) {
                hash ^= node.GetHashCode();
            }

            return hash;
        }

    }

    public enum SearchNodeType {

        DEFAULT = 0,

        FIELD,

        FIELD_NAME,

        FIELD_OP,

        FIELD_VALUE,

        AND,

        OR,

        NOT,

    }

    public static class SearchNodeExtensionMethods {

        public static SearchNode GetRoot(this SearchNode node) {
            SearchNode iter = node;

            while (true) {
                if (iter.Parent == null) {
                    return iter;
                }

                iter = iter.Parent;
            }
        }

    }

}
