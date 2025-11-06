using gex.Common.Code.Constants;
using gex.Models.Bar.Commands;
using gex.Models.Bar.Commands.Types;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace gex.Models.Bar {

    // guh, this kinda sucks no cap!
    [JsonDerivedType(typeof(BarCommandBuild))]
    [JsonDerivedType(typeof(BarCommandInsert))]
    [JsonDerivedType(typeof(BarCommandCustom))]
    [JsonDerivedType(typeof(BarCommandTypeIcon))]
    [JsonDerivedType(typeof(BarCommandTypeIconArea))]
    [JsonDerivedType(typeof(BarCommandTypeIconMap))]
    [JsonDerivedType(typeof(BarCommandTypeIconMode))]
    [JsonDerivedType(typeof(BarCommandTypeIconUnit))]
    [JsonDerivedType(typeof(BarCommandTypeIconUnitFeatureOrArea))]
    [JsonDerivedType(typeof(BarCommandTypeIconUnitOrArea))]
    [JsonDerivedType(typeof(BarCommandTypeIconUnitOrMap))]
    [JsonDerivedType(typeof(BarCommandTypeNumber))]
    [JsonDerivedType(typeof(BarCommandTypeUnitIconOrRect))]
    public class BarCommand {

        [JsonConstructor]
        protected BarCommand() { }

        /// <summary>
        ///     ID of the command. see <see cref="BarCommandId"/>
        /// </summary>
        public int ID { get; set; }

        public float FullGameTime { get; set; }

        /// <summary>
        ///     ID of the player that gave this command
        /// </summary>
        public byte PlayerID { get; set; }

        /// <summary>
        ///     list of units that were given this command
        /// </summary>
        public List<ushort> UnitIDs { get; set; } = [];

        public bool OptionMetaKey { get; set; }

        public bool OptionInternalOrder { get; set; }

        public bool OptionRightMouseKey { get; set; }

        public bool OptionShiftKey { get; set; }

        public bool OptionControlKey { get; set; }

        public bool OptionAltKey { get; set; }

    }

}
