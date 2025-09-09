using System.Collections.Generic;
using System.Security.Permissions;

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

        /// <summary>
        ///     if only the impact of the projectile deals damage (i.e. no splash damage)
        /// </summary>
        public bool ImpactOnly { get; set; }

        public int Projectiles { get; set; }

        public double SweepFire { get; set; }

        public double Range { get; set; }

        public double ReloadTime { get; set; }

        public double SprayAngle { get; set; }

        public double Velocity { get; set; }

        public string WeaponType { get; set; } = "";

        public bool Tracks { get; set; }

        public bool WaterWeapon { get; set; }

        public double EnergyPerShot { get; set; }

        public double MetalPerShot { get; set; }

        public bool IsStockpile { get; set; }

        public double StockpileTime { get; set; }

        public int StockpileLimit { get; set; }

        public bool IsParalyzer { get; set; }

        public double ParalyzerTime { get; set; }

        public string ParalyzerExceptions { get; set; } = "";

        public bool IsBogus { get; set; }

        public double ChainForkDamage { get; set; }

        public int ChainMaxUnits { get; set; }

        public double ChainForkRange { get; set; }

        public Dictionary<string, double> Damages { get; set; } = [];

        public BarUnitShield? ShieldData { get; set; } = null;

        public BarUnitCarriedUnit? CarriedUnit { get; set; } = null;

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

    public class BarUnitCarriedUnit {

        public string DefinitionName { get; set; } = "";

        public double EngagementRange { get; set; }

        public string SpawnSurface { get; set; } = "";

        public double SpawnRate { get; set; }

        public int MaxUnits { get; set; }

        public double EnergyCost { get; set; }

        public double MetalCost { get; set; }

        public double ControlRadius { get; set; }

        public double DecayRate { get; set; }

        public bool EnableDocking { get; set; }

        public double DockingArmor { get; set; }

        public double DockingHealRate { get; set; }

        public double DockToHealThreshold { get; set; }

        public double DockingHelperSpeed { get; set; }

    }

}
