using gex.Models.Bar;
using System.Collections.Generic;

namespace gex.Models.Api {
    public class ApiBarUnit {

        public string DefinitionName { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public string Description { get; set; } = "";

        public BarUnit Unit { get; set; } = new();

        public BarMoveDefinition? MoveDefinition { get; set; } = null;

        public List<BarWeaponDefinition> IncludedWeapons { get; set; } = [];

        public List<BarUnit> IncludedUnits { get; set; } = [];

    }
}
