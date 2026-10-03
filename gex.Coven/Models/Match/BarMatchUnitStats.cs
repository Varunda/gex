using Avalonia.Rendering.Composition;
using gex.Common.Models.Event;
using gex.Coven.ViewModels;
using LiveChartsCore.SkiaSharpView.TypeConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Match {

    public class BarMatchUnitStats {

        public string ID { get; set; } = "";

        public List<BarMatchEntity> Entities = [];

        public int DefinitionID { get; set; }

        public string Name { get; set; } = "";

        public string DefinitionName { get; set; } = "";

        public GameEventUnitDef? UnitDefinition { get; set; } = null;

        public int Made { get; set; }

        public int Rank { get; set; }

        public int Kills { get; set; }

        public int MobileKills { get; set; }

        public int StaticKills { get; set; }

        public int Lost { get; set; }

        public int Reclaimed { get; set; }

        public int Reclaims { get; set; }

        public double DamageDealt { get; set; }

        public double DamageTaken { get; set; }

        public Dictionary<string, int> UnitsKilled { get; set; } = [];

        public Dictionary<string, int> UnitsTeamKilled { get; set; } = [];

        public double MetalKilled { get; set; }

        public double EnergyKilled { get; set; }

        public double BuildPowerKilled { get; set; }

        public double MetalEcoKilledMetal { get; set; }

        public double MetalEcoKilledEnergy { get; set; }

        public double EnergyEcoKilledMetal { get; set; }

        public double EnergyEcoKilledEnergy { get; set; }

        public double DamageRatio { get; set; }

        public double MetalRatio { get; set; }

        public double EnergyRatio { get; set; }

        public bool EntityMatches(BarMatchEntity entity) {
            return this.Entities.FirstOrDefault(iter => iter == entity) != null;
        }

    }
}
