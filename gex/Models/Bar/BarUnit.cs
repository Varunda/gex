using System.Collections.Generic;
using System.Security.Permissions;

namespace gex.Models.Bar {

    public class BarUnit {

        public string DefinitionName { get; set; } = "";

        // basic info

        public double Health { get; set; }

        public double MetalCost { get; set; }

        public double EnergyCost { get; set; }

        public double BuildTime { get; set; }

        public double Speed { get; set; }

        public double TurnRate { get; set; }

        // https://github.com/beyond-all-reason/Beyond-All-Reason/blob/2d264117ff0d4f735e867bf352a5db0cdf32c34d/luaui/Widgets/gui_unit_stats.lua#L431
        public double Acceleration { get; set; }

        public double Deceleration { get; set; }

        public double SizeX { get; set; }

        public double SizeZ { get; set; }

        // eco stuff

        public double EnergyProduced { get; set; }

        public double WindGenerator { get; set; }

        public double TidalGenerator { get; set; }

        public double EnergyStorage { get; set; }

        public double EnergyUpkeep { get; set; }

        public double ExtractsMetal { get; set; }

        public bool MetalExtractor { get; set; }

        public double MetalProduced { get; set; }

        public double MetalStorage { get; set; }

        public double EnergyConversionCapacity { get; set; }

        public double EnergyConversionEfficiency { get; set; }

        // builder stuff

        public bool IsBuilder { get; set; }

        public double BuildDistance { get; set; }

        public double BuildPower { get; set; }

        public bool CanResurrect { get; set; }

        public bool CanAssist { get; set; }

        public bool CanReclaim { get; set; }

        public bool CanRepair { get; set; }

        public bool CanRestore { get; set; }

        // los

        public double SightDistance { get; set; }

        public double AirSightDistance { get; set; } // is 1.5 of SightDistance if not set

        public double RadarDistance { get; set; }

        public double SonarDistance { get; set; }

        public double JamDistance { get; set; }

        // transport stuff

        public double TransportCapacity { get; set; }

        public double TransportMass { get; set; }

        public double TransportSize { get; set; }

        // misc

        public string? ModelAuthor { get; set; }

        public double CloakCostStill { get; set; }

        public double CloakCostMoving { get; set; }

        public bool IsStealth { get; set; }

        public double ParalyzeMultiplier { get; set; }

        public double AutoHeal { get; set; }

        public double IdleTime { get; set; }

        public double IdleAutoHeal { get; set; }

        /// <summary>
        ///     what weapon definition to use when exploding
        /// </summary>
        public string ExplodeAs { get; set; } = "";

        /// <summary>
        ///     what weapon definition to use when self-d
        /// </summary>
        public string SelfDestructWeapon { get; set; } = "";

        /// <summary>
        ///     how long it takes the unit to self-d
        /// </summary>
        public double SelfDestructCountdown { get; set; }

        public List<BarUnitWeapon> Weapons { get; set; } = [];

    }

    public class BarUnitWeapon {

        public BarWeaponDefinition WeaponDefinition { get; set; } = new();

        public int Count { get; set; }

        public string TargetCategory { get; set; } = "";

        public double GetDefaultDamage() {
            double damage = 0d;
            if (TargetCategory == "VTOL" && WeaponDefinition.Damages.TryGetValue("vtol", out damage)) {
                //
            } else if (WeaponDefinition.Damages.TryGetValue("default", out damage)) {
                //
            }

            return damage;
        }

    }

}
