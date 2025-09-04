using System.Collections.Generic;

namespace gex.Models.Bar {

    public class BarWeaponDefinition {

        public string Name { get; set; } = "";

        public string DefinitionName { get; set; } = "";

        public double AreaOfEffect { get; set; }

        public double Burst { get; set; }

        public double BurstRate { get; set; }

        public double EdgeEffectiveness { get; set; }

        public double FlightTime { get; set; }

        public double ImpulseFactor { get; set; }

        public bool ImpactOnly { get; set; }

        public double Range { get; set; }

        public double ReloadTime { get; set; }

        public double TurnRate { get; set; }

        public double Velocity { get; set; }

        public string WeaponType { get; set; } = "";

        public bool Tracks { get; set; }

        public bool WaterWeapon { get; set; }

        public double EnergyPerShot { get; set; }

        public double MetalPerShot { get; set; }

        public bool IsParalyzer { get; set; }

        public double ParalyzerTime { get; set; }

        public string ParalyzerExceptions { get; set; } = "";

        public bool IsBogus { get; set; }

        public Dictionary<string, double> Damages { get; set; } = [];

        public BarUnitShield? ShieldData { get; set; } = null;

    }

    public class BarUnitShield {

        public double EnergyUpkeep { get; set; }

        public double Power { get; set; }

        public double PowerRegen { get; set; }

        public double PowerRegenEnergy { get; set; }

        public double Radius { get; set; }

        public bool Repulser { get; set; }

        public double StartingPower { get; set; }

        public double Force { get; set; }

    }
}
