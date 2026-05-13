using gex.Services.Db;
using System;
using System.Collections.Generic;

namespace gex.Models.Db {

    /// <summary>
    ///     options used in a <see cref="UserUnitsMadeLeaderboardDb"/>
    /// </summary>
    public class UserUnitsMadeLeaderboardOptions {

        /// <summary>
        ///     list of unit definitions to include
        /// </summary>
        public List<string> UnitDefinitions { get; set; } = [];

        public int Offset { get; set; }

        public int Limit { get; set; }

        /// <summary>
        ///     when to start the include (inclusive)
        /// </summary>
        public DateTime PeriodStart { get; set; }

        /// <summary>
        ///     when to end the period being searched over (exclusive)
        /// </summary>
        public DateTime PeriodEnd { get; set; }

        /// <summary>
        ///     optinal, filter based on gamemodes
        /// </summary>
        public List<byte>? Gamemodes { get; set; }

        /// <summary>
        ///     optional, filter based on map filenames
        /// </summary>
        public List<string>? MapFilename { get; set; }

        /// <summary>
        ///     list of user IDs to only show
        /// </summary>
        public List<long>? UserIDs { get; set; }

    }
}
