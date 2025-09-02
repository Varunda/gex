using System.Collections.Generic;

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

        // eco stuff

        public double EnergyProduced { get; set; }

        public double WindGenerator { get; set; }

        public double EnergyStorage { get; set; }

        public double EnergyUpkeep { get; set; }

        public double ExtractsMetal { get; set; }

        public bool MetalExtractor { get; set; }

        public double MetalProduced { get; set; }

        public double MetalStorage { get; set; }

        // builder stuff

        public double BuildDistance { get; set; }

        public double BuildPower { get; set; }

        // los

        public double SightDistance { get; set; }

        public double AirSightDistance { get; set; }

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

        public bool CanResurrect { get; set; }

        public List<BarUnitWeapon> Weapons { get; set; } = [];

    }

    public class BarUnitWeapon {

        public string Name { get; set; } = "";

        public string DefinitionName { get; set; } = "";

        public double AreaOfEffect { get; set; }

        public double Burst { get; set; }

        public double BurstRate { get; set; }

        public double FlightTime { get; set; }

        public double ImpulseFactor { get; set; }

        public double Range { get; set; }

        public double ReloadTime { get; set; }

        public double TurnRate { get; set; }

        public double Velocity { get; set; }

        public string WeaponType { get; set; } = "";

        public bool Tracks { get; set; }

        public bool WaterWeapon { get; set; }

        public string TargetCategory { get; set; } = "";

        public double EnergyPerShot { get; set; }

        public double MetalPerShot { get; set; }

        public bool IsParalyzer { get; set; }

        public double ParalyzerTime { get; set; }

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
