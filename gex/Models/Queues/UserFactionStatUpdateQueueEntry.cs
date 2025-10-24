using gex.Common.Code.Constants;

namespace gex.Models.Queues {

    public class UserFactionStatUpdateQueueEntry {

        public long UserID { get; set; }

        public byte Faction { get; set; } = BarFaction.DEFAULT;

        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        /// <summary>
        ///     if true, this flag indicates that a lack of data for the user on the faction and gamemode
        ///     is not an error, and expected
        /// </summary>
        public bool MaybeNone { get; set; } = false;

    }
}
