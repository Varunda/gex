using gex.Common.Code.Constants;

namespace gex.Models.Queues {

    public class UserMapStatUpdateQueueEntry {

        public long UserID { get; set; }

        public string Map { get; set; } = "";

        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        /// <summary>
        ///     if true, this flag indicates that a lack of data for the user on the map and gamemode
        ///     is not an error, and expected
        /// </summary>
        public bool MaybeNone { get; set; } = false;

    }
}
