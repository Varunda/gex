namespace gex.Models.Db {

    public class BarMatchAllyTeam {

        public string GameID { get; set; } = "";

        public int AllyTeamID { get; set; }

        public int PlayerCount { get; set; }

        public bool Won { get; set; }

        public Rectangle StartBox { get; set; } = Rectangle.Zero;

    }
}
