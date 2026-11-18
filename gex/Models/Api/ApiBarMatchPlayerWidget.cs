using gex.Common.Models.Bar;
using gex.Common.Models.Match;

namespace gex.Models.Api {

    public class ApiBarMatchPlayerWidget {

        public string GameID { get; set; } = "";

        public long UserID { get; set; }

        public string Hash { get; set; } = "";

        public int State { get; set; }

        public string Name { get; set; } = "";

        public string Author { get; set; } = "";

        public string Description { get; set; } = "";

        public ApiBarMatchPlayerWidget(BarMatchPlayerWidget playerWidget, BarWidgetEntry? widget) {
            GameID = playerWidget.GameID;
            UserID = playerWidget.UserID;
            Hash = playerWidget.Hash;
            State = playerWidget.State;

            Name = widget?.Name ?? $"<missing widget {playerWidget.Hash}>";
            Author = widget?.Author ?? $"<missing widget {playerWidget.Hash}>";
            Description = widget?.Description ?? $"<missing widget {playerWidget.Hash}>";
        }

    }
}
