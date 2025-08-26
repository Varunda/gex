using System;
using System.Collections.Generic;

namespace gex.Models.UserStats {

    /// <summary>
    ///     represents all changes of a user
    /// </summary>
    public class BarUserSkillChanges {

        /// <summary>
        ///     ID of the user these changes are for
        /// </summary>
        public long UserID { get; set; }

        /// <summary>
        ///     list of all gamemode skill changes
        /// </summary>
        public List<BarUserSkillGamemode> Gamemodes { get; set; } = [];

    }

    /// <summary>
    ///     represents the changes in skill for a single gamemode
    /// </summary>
    public class BarUserSkillGamemode {

        /// <summary>
        ///     ID of the gamemode
        /// </summary>
        public byte Gamemode { get; set; }

        /// <summary>
        ///     all changes to skill for this gamemode
        /// </summary>
        public List<BarUserSkillChangeEntry> Changes { get; set; } = [];

    }

    /// <summary>
    ///     represents a single change in a players skill for a specific gamemode
    /// </summary>
    public class BarUserSkillChangeEntry {

        /// <summary>
        ///     skill value
        /// </summary>
        public double Skill { get; set; }

        /// <summary>
        ///     mau
        /// </summary>
        public double SkillUncertainty { get; set; }

        /// <summary>
        ///     when this change took place
        /// </summary>
        public DateTime Timestamp { get; set; }

    }

}
