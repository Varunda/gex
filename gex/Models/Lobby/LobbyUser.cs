namespace gex.Models.Lobby {

    public class LobbyUser {

        public string Username { get; set; } = "";

        public long UserID { get; set; }

        public string Version { get; set; } = "";

        public bool InGame { get; set; }

        public bool Away { get; set; }

        public int Rank { get; set; }

        public bool AccessStatus { get; set; }

        public bool IsBot { get; set; }

    }
}
