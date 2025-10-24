using System.Collections.Generic;
using System.Linq;

namespace gex.Common.Models.Lobby {

    public class LobbyMessage {

        public int? MessageId { get; set; }

        public string Command { get; set; } = "";

        private string _Arguments = "";
        private string _ArgumentsReader = "";

        public string Arguments {
            get {
                return _Arguments;
            }
            set {
                _Arguments = value;
                _ArgumentsReader = _Arguments;
            }
        }

        public string? GetWord() => GetPart(' ');

        public string? GetSentence() => GetPart('\t');

        private string? GetPart(char sep) {
            if (_ArgumentsReader == "") {
                return null;
            }

            int offset = _ArgumentsReader.IndexOf(sep);
            // if there is no seperator left, it's the whole rest of the thing
            if (offset == -1) {
                string rest = _ArgumentsReader;
                // strip \n on last character
                if (rest.Last() == '\n') {
                    rest = rest[..(rest.Length - 1)];
                }
                _ArgumentsReader = "";
                return rest;
            }

            string sent = _ArgumentsReader[..offset];

            if (offset == _ArgumentsReader.Length) {
                _ArgumentsReader = "";
            } else {
                _ArgumentsReader = _ArgumentsReader[(sent.Length + 1)..];
            }

            return sent;
        }

    }
}
