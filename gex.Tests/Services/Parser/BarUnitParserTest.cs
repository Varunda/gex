using gex.Common.Models;
using gex.Models;
using gex.Models.Bar;
using gex.Services;
using gex.Services.Parser;
using gex.Tests.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Tests.Services.Parser {

    [TestClass]
    public class BarUnitParserTest {

        private BarUnitParser _MakeParser() {
            LuaRunner runner = new LuaRunner(new TestLogger<LuaRunner>());

            ILogger<BarWeaponDefinitionParser> logger2 = new TestLogger<BarWeaponDefinitionParser>();
            BarWeaponDefinitionParser defParser = new(logger2, runner);

            ILogger<BarUnitParser> logger = new TestLogger<BarUnitParser>();
            BarUnitParser parser = new(logger, defParser, runner);
            return parser;
        }

        private async Task<BarUnit> _ParseUnit(string filename) {
            BarUnitParser parser = _MakeParser();

            string file = File.ReadAllText($"./resources/unit_data/{filename}.lua");

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            Result<BarUnit, string> unitResult = await parser.Parse(file, cts.Token);
            Assert.IsTrue(unitResult.IsOk, $"expected IsOk true, got error of: {unitResult.Error}");

            return unitResult.Value;
        }

        [TestMethod]
        public async Task Parse_BadLua() {
            BarUnitParser parser = _MakeParser();

            string file = File.ReadAllText($"./resources/unit_data/bad_lua.lua");

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(1));

            Result<BarUnit, string> unitResult = await parser.Parse(file, cts.Token);
            Assert.AreEqual(false, unitResult.IsOk);
        }

        [TestMethod]
        public async Task Parse_Armpw_Pawn() {
            BarUnit unit = await _ParseUnit("armpw");

            // basic
            Assert.AreEqual("armpw", unit.DefinitionName);
            Assert.AreEqual(370d, unit.Health);
            Assert.AreEqual(54d, unit.MetalCost);
            Assert.AreEqual(900d, unit.EnergyCost);
            Assert.AreEqual(1650d, unit.BuildTime);
            Assert.AreEqual(87d, unit.Speed);
            Assert.AreEqual(1214.40002, unit.TurnRate);
            Assert.AreEqual(0.414d, unit.Acceleration);
            Assert.AreEqual(0.69d, unit.Deceleration);
            Assert.AreEqual(2d, unit.SizeX);
            Assert.AreEqual(2d, unit.SizeZ);

            // eco
            Assert.AreEqual(0d, unit.EnergyProduced);
            Assert.AreEqual(0d, unit.EnergyStorage);
            Assert.AreEqual(0d, unit.EnergyUpkeep);
            Assert.AreEqual(0d, unit.ExtractsMetal);
            Assert.AreEqual(false, unit.MetalExtractor);
            Assert.AreEqual(0d, unit.MetalProduced);
            Assert.AreEqual(0d, unit.MetalStorage);

            // builder
            Assert.AreEqual(0d, unit.BuildDistance);
            Assert.AreEqual(0d, unit.BuildPower);

            // los
            Assert.AreEqual(429d, unit.SightDistance);
            Assert.AreEqual(429d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);
            Assert.AreEqual(0d, unit.JamDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("Kaiser", unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);
            Assert.AreEqual("smallExplosionGeneric", unit.ExplodeAs);
            Assert.AreEqual(5d, unit.SelfDestructCountdown);
            Assert.AreEqual("smallExplosionGenericSelfd", unit.SelfDestructWeapon);
            Assert.AreEqual(0d, unit.AutoHeal);
            Assert.AreEqual(5d, unit.IdleAutoHeal);
            Assert.AreEqual(1800d, unit.IdleTime);
            Assert.AreEqual(1d, unit.DamageModifier);
            Assert.AreEqual(false, unit.OnOffAble);

            // weapons
            Assert.AreEqual(1, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[0];
            Assert.AreEqual(1, weapon.Count);
            Assert.AreEqual("NOTSUB", weapon.TargetCategory);

            BarWeaponDefinition def = weapon.WeaponDefinition;
            Assert.AreEqual("emg", def.DefinitionName);
            Assert.AreEqual("Rapid-fire close-quarters g2g plasma guns", def.Name);
            Assert.AreEqual(8d, def.AreaOfEffect);
            Assert.AreEqual(3d, def.Burst);
            Assert.AreEqual(0.1d, def.BurstRate);
            Assert.AreEqual(0d, def.FlightTime);
            Assert.AreEqual(0.123d, def.ImpulseFactor);
            Assert.AreEqual(180d, def.Range);
            Assert.AreEqual(0.3d, def.ReloadTime);
            Assert.AreEqual(1180d, def.SprayAngle);
            Assert.AreEqual(500d, def.Velocity);
            Assert.AreEqual(false, def.Tracks);
            Assert.AreEqual(0d, def.EnergyPerShot);
            Assert.AreEqual(0d, def.MetalPerShot);
            Assert.AreEqual("Cannon", def.WeaponType);
            Assert.AreEqual(false, def.WaterWeapon);
            Assert.AreEqual(false, def.IsBogus);
            Assert.AreEqual(9d, def.Damages["default"]);
            Assert.AreEqual(3d, def.Damages["vtol"]);
            Assert.AreEqual(false, def.IsStockpile);
            Assert.AreEqual(0d, def.StockpileTime);
            Assert.AreEqual(0, def.StockpileLimit);
            Assert.AreEqual(0d, def.ChainForkDamage);
            Assert.AreEqual(0, def.ChainMaxUnits);
            Assert.AreEqual(0d, def.ChainForkRange);
            Assert.AreEqual(0d, def.TimedAreaDamage);
            Assert.AreEqual(0d, def.TimedAreaRange);
            Assert.AreEqual(0d, def.TimedAreaTime);
        }

        [TestMethod]
        public async Task Parse_Corcom_CortexCommander() {
            BarUnit unit = await _ParseUnit("corcom");

            // basic
            Assert.AreEqual("corcom", unit.DefinitionName);
            Assert.AreEqual(3700d, unit.Health);
            Assert.AreEqual(2700d, unit.MetalCost);
            Assert.AreEqual(26000d, unit.EnergyCost);
            Assert.AreEqual(75000d, unit.BuildTime);
            Assert.AreEqual(37.5d, unit.Speed);
            Assert.AreEqual(1133, unit.TurnRate);

            // eco
            Assert.AreEqual(30d, unit.EnergyProduced);
            Assert.AreEqual(500d, unit.EnergyStorage);
            Assert.AreEqual(0d, unit.EnergyUpkeep);
            Assert.AreEqual(0d, unit.ExtractsMetal);
            Assert.AreEqual(false, unit.MetalExtractor);
            Assert.AreEqual(2d, unit.MetalProduced);
            Assert.AreEqual(500d, unit.MetalStorage);

            // builder
            Assert.AreEqual(145d, unit.BuildDistance);
            Assert.AreEqual(300d, unit.BuildPower);
            Assert.AreEqual(26, unit.BuildOptions.Count);
            Assert.AreEqual("corsolar", unit.BuildOptions[0]);
            Assert.AreEqual("corfhp", unit.BuildOptions[25]);

            // los
            Assert.AreEqual(450d, unit.SightDistance);
            Assert.AreEqual(450d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(700d, unit.RadarDistance);
            Assert.AreEqual(450d, unit.SonarDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("Mr Bob", unit.ModelAuthor);
            Assert.AreEqual(100d, unit.CloakCostStill);
            Assert.AreEqual(1000d, unit.CloakCostMoving);
            Assert.AreEqual("commanderexplosion", unit.ExplodeAs); // yes, just the casing is different in corcom.lua
            Assert.AreEqual(5d, unit.SelfDestructCountdown);
            Assert.AreEqual("commanderExplosion", unit.SelfDestructWeapon);
            Assert.AreEqual(5d, unit.AutoHeal);
            Assert.AreEqual(5d, unit.IdleAutoHeal);
            Assert.AreEqual(1800d, unit.IdleTime);
            Assert.AreEqual(1d, unit.DamageModifier);
            Assert.AreEqual(false, unit.OnOffAble);

            // weapons
            Assert.AreEqual(3, unit.Weapons.Count);

            Assert.AreEqual(true, unit.Weapons[2].WeaponDefinition.WaterWeapon);
        }

        [TestMethod]
        public async Task Parse_Cormex_CortexMetalExtractor() {
            BarUnit unit = await _ParseUnit("cormex");

            // basic
            Assert.AreEqual("cormex", unit.DefinitionName);
            Assert.AreEqual(275d, unit.Health);
            Assert.AreEqual(50d, unit.MetalCost);
            Assert.AreEqual(500d, unit.EnergyCost);
            Assert.AreEqual(1870d, unit.BuildTime);
            Assert.AreEqual(0d, unit.Speed);
            Assert.AreEqual(0d, unit.TurnRate);

            // eco
            Assert.AreEqual(0d, unit.EnergyProduced);
            Assert.AreEqual(0d, unit.EnergyStorage);
            Assert.AreEqual(3d, unit.EnergyUpkeep);
            Assert.AreEqual(0.001, unit.ExtractsMetal);
            Assert.AreEqual(true, unit.MetalExtractor);
            Assert.AreEqual(0d, unit.MetalProduced);
            Assert.AreEqual(50d, unit.MetalStorage);

            // builder
            Assert.AreEqual(0d, unit.BuildDistance);
            Assert.AreEqual(0d, unit.BuildPower);

            // los
            Assert.AreEqual(273d, unit.SightDistance);
            Assert.AreEqual(273d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("Mr Bob", unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);
            Assert.AreEqual("smallBuildingexplosiongeneric", unit.ExplodeAs);
            Assert.AreEqual(1d, unit.SelfDestructCountdown);
            Assert.AreEqual("smallMex", unit.SelfDestructWeapon);
            Assert.AreEqual(true, unit.OnOffAble);

            // weapons
            Assert.AreEqual(0, unit.Weapons.Count);
        }

        [TestMethod]
        public async Task Parse_Corfgate_CortextFGate() {
            BarUnit unit = await _ParseUnit("corfgate");

            // basic
            Assert.AreEqual("corfgate", unit.DefinitionName);
            Assert.AreEqual(4100d, unit.Health);
            Assert.AreEqual(4100d, unit.MetalCost);
            Assert.AreEqual(74000d, unit.EnergyCost);
            Assert.AreEqual(59000d, unit.BuildTime);
            Assert.AreEqual(0d, unit.Speed);
            Assert.AreEqual(0d, unit.TurnRate);

            // eco
            Assert.AreEqual(0d, unit.EnergyProduced);
            Assert.AreEqual(0d, unit.EnergyStorage);
            Assert.AreEqual(0d, unit.EnergyUpkeep);
            Assert.AreEqual(0d, unit.ExtractsMetal);
            Assert.AreEqual(false, unit.MetalExtractor);
            Assert.AreEqual(0d, unit.MetalProduced);
            Assert.AreEqual(0d, unit.MetalStorage);

            // builder
            Assert.AreEqual(0d, unit.BuildDistance);
            Assert.AreEqual(0d, unit.BuildPower);

            // los
            Assert.AreEqual(600d, unit.SightDistance);
            Assert.AreEqual(600d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual(null, unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);
            Assert.AreEqual(false, unit.OnOffAble);

            // weapons
            Assert.AreEqual(1, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[0];
            Assert.AreEqual(1, weapon.Count);

            BarWeaponDefinition def = weapon.WeaponDefinition;
            Assert.IsNotNull(def.ShieldData);
            Assert.AreEqual(2.5d, def.ShieldData.Force);
            Assert.AreEqual(3250d, def.ShieldData.Power);
            Assert.AreEqual(52d, def.ShieldData.PowerRegen);
            Assert.AreEqual(562.5, def.ShieldData.PowerRegenEnergy);
            Assert.AreEqual(600d, def.ShieldData.Radius);
            Assert.AreEqual(1100d, def.ShieldData.StartingPower);
            Assert.AreEqual(true, def.ShieldData.Repulser);
        }

        [TestMethod]
        public async Task Parse_Corkorg_Juggernaut() {
            BarUnit unit = await _ParseUnit("corkorg");

            // basic
            Assert.AreEqual("corkorg", unit.DefinitionName);
            Assert.AreEqual(149000d, unit.Health);
            Assert.AreEqual(29000d, unit.MetalCost);
            Assert.AreEqual(615000d, unit.EnergyCost);
            Assert.AreEqual(555000d, unit.BuildTime);
            Assert.AreEqual(37d, unit.Speed);
            Assert.AreEqual(437d, unit.TurnRate);

            // eco
            Assert.AreEqual(300d, unit.EnergyProduced);
            Assert.AreEqual(5000d, unit.EnergyStorage);
            Assert.AreEqual(0d, unit.EnergyUpkeep);
            Assert.AreEqual(0d, unit.ExtractsMetal);
            Assert.AreEqual(false, unit.MetalExtractor);
            Assert.AreEqual(0d, unit.MetalProduced);
            Assert.AreEqual(0d, unit.MetalStorage);

            // builder
            Assert.AreEqual(0d, unit.BuildDistance);
            Assert.AreEqual(0d, unit.BuildPower);

            // los
            Assert.AreEqual(845d, unit.SightDistance);
            Assert.AreEqual(845d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("FireStorm", unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);
            Assert.AreEqual(false, unit.OnOffAble);

            // weapons
            Assert.AreEqual(7, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[5];
            Assert.AreEqual(true, weapon.WeaponDefinition.IsBogus);
            Assert.AreEqual(10, unit.Weapons[0].WeaponDefinition.Projectiles);
        }

        [TestMethod]
        public async Task Parse_Legeheatraymech_SolInvictus() {
            BarUnit unit = await _ParseUnit("legeheatraymech");

            // basic
            Assert.AreEqual("legeheatraymech", unit.DefinitionName);
            Assert.AreEqual(110000d, unit.Health);
            Assert.AreEqual(23500d, unit.MetalCost);
            Assert.AreEqual(615000d, unit.EnergyCost);
            Assert.AreEqual(440000d, unit.BuildTime);
            Assert.AreEqual(40d, unit.Speed);
            Assert.AreEqual(360d, unit.TurnRate);
            Assert.AreEqual(0.1750d, unit.Acceleration);
            Assert.AreEqual(0.75d, unit.Deceleration);

            // eco
            Assert.AreEqual(500d, unit.EnergyProduced);
            Assert.AreEqual(0d, unit.EnergyStorage);
            Assert.AreEqual(0d, unit.EnergyUpkeep);
            Assert.AreEqual(0d, unit.ExtractsMetal);
            Assert.AreEqual(false, unit.MetalExtractor);
            Assert.AreEqual(0d, unit.MetalProduced);
            Assert.AreEqual(0d, unit.MetalStorage);

            // builder
            Assert.AreEqual(0d, unit.BuildDistance);
            Assert.AreEqual(0d, unit.BuildPower);

            // los
            Assert.AreEqual(845d, unit.SightDistance);
            Assert.AreEqual(845d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("Protar & ZephyrSkies", unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);
            Assert.AreEqual("banthaSelfd", unit.ExplodeAs);
            Assert.AreEqual(10d, unit.SelfDestructCountdown);
            Assert.AreEqual("korgExplosion", unit.SelfDestructWeapon);
            Assert.AreEqual(false, unit.OnOffAble);

            // weapons
            Assert.AreEqual(5, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[2];
            Assert.AreEqual(2, weapon.Count);
        }

        [TestMethod]
        public async Task Parse_Legeallterrainmech_Myrmidion() {
            BarUnit unit = await _ParseUnit("legeallterrainmech");

            // basic
            Assert.AreEqual("legeallterrainmech", unit.DefinitionName);

            // weapons
            Assert.AreEqual(5, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[0];
            Assert.AreEqual("drone_controller", weapon.WeaponDefinition.DefinitionName);
            Assert.IsNotNull(weapon.WeaponDefinition.CarriedUnit);

            BarUnitCarriedUnit c = weapon.WeaponDefinition.CarriedUnit;
            Assert.AreEqual("legheavydronesmall", c.DefinitionName);
            Assert.AreEqual(1600d, c.EngagementRange);
            Assert.AreEqual("LAND", c.SpawnSurface);
            Assert.AreEqual(8d, c.SpawnRate);
            Assert.AreEqual(2, c.MaxUnits);
            Assert.AreEqual(1000d, c.EnergyCost);
            Assert.AreEqual(90d, c.MetalCost);
            Assert.AreEqual(1800d, c.ControlRadius);
            Assert.AreEqual(4d, c.DecayRate);
            Assert.AreEqual(true, c.EnableDocking);
            Assert.AreEqual(0.2d, c.DockingArmor);
            Assert.AreEqual(256d, c.DockingHealRate);
            Assert.AreEqual(5d, c.DockingHelperSpeed);
            Assert.AreEqual(33d, c.DockToHealThreshold);
        }

        [TestMethod]
        public async Task Parse_Legmos_Mosquito() {
            BarUnit unit = await _ParseUnit("legmos");

            // basic
            Assert.AreEqual("legmos", unit.DefinitionName);

            // weapons
            Assert.AreEqual(1, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[0];
            Assert.AreEqual("cor_bot_rocket", weapon.WeaponDefinition.DefinitionName);
            BarWeaponDefinition def = weapon.WeaponDefinition;
            Assert.AreEqual(true, def.IsStockpile);
            Assert.AreEqual(2d, def.StockpileTime);
            Assert.AreEqual(4, def.StockpileLimit);
        }

        [TestMethod]
        public async Task Parse_Armzeus_Welder() {
            BarUnit unit = await _ParseUnit("armzeus");

            // basic
            Assert.AreEqual("armzeus", unit.DefinitionName);
            Assert.AreEqual(2950d, unit.Health);
            Assert.AreEqual(350d, unit.MetalCost);
            Assert.AreEqual(6100d, unit.EnergyCost);
            Assert.AreEqual(7250d, unit.BuildTime);
            Assert.AreEqual(47.4d, unit.Speed);
            Assert.AreEqual(1214.40002, unit.TurnRate);
            Assert.AreEqual(0.138d, unit.Acceleration);
            Assert.AreEqual(0.8625d, unit.Deceleration);

            // eco
            Assert.AreEqual(0d, unit.EnergyProduced);
            Assert.AreEqual(0d, unit.EnergyStorage);
            Assert.AreEqual(0d, unit.EnergyUpkeep);
            Assert.AreEqual(0d, unit.ExtractsMetal);
            Assert.AreEqual(false, unit.MetalExtractor);
            Assert.AreEqual(0d, unit.MetalProduced);
            Assert.AreEqual(0d, unit.MetalStorage);

            // builder
            Assert.AreEqual(0d, unit.BuildDistance);
            Assert.AreEqual(0d, unit.BuildPower);

            // los
            Assert.AreEqual(331.5d, unit.SightDistance);
            Assert.AreEqual(331.5d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);
            Assert.AreEqual(0d, unit.JamDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("FireStorm, FLaka", unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);
            Assert.AreEqual("mediumexplosiongeneric", unit.ExplodeAs);
            Assert.AreEqual(5d, unit.SelfDestructCountdown);
            Assert.AreEqual("mediumExplosionGenericSelfd", unit.SelfDestructWeapon);
            Assert.AreEqual(false, unit.OnOffAble);

            // weapons
            Assert.AreEqual(1, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[0];
            Assert.AreEqual(1, weapon.Count);
            Assert.AreEqual("SURFACE", weapon.TargetCategory);

            BarWeaponDefinition def = weapon.WeaponDefinition;
            Assert.AreEqual("lightning", def.DefinitionName);
            Assert.AreEqual("Close-quarters g2g lightning rifle", def.Name);
            Assert.AreEqual(8d, def.AreaOfEffect);
            Assert.AreEqual(10d, def.Burst);
            Assert.AreEqual(0.03333d, def.BurstRate);
            Assert.AreEqual(0d, def.FlightTime);
            Assert.AreEqual(0d, def.ImpulseFactor);
            Assert.AreEqual(280d, def.Range);
            Assert.AreEqual(1.7d, def.ReloadTime);
            Assert.AreEqual(0d, def.SprayAngle);
            Assert.AreEqual(400d, def.Velocity);
            Assert.AreEqual(false, def.Tracks);
            Assert.AreEqual(35d, def.EnergyPerShot);
            Assert.AreEqual(0d, def.MetalPerShot);
            Assert.AreEqual("LightningCannon", def.WeaponType);
            Assert.AreEqual(false, def.WaterWeapon);
            Assert.AreEqual(false, def.IsBogus);
            Assert.AreEqual(22d, def.Damages["default"]);
            Assert.AreEqual(6d, def.Damages["vtol"]);
            Assert.AreEqual(false, def.IsStockpile);
            Assert.AreEqual(0d, def.StockpileTime);
            Assert.AreEqual(0, def.StockpileLimit);
            Assert.AreEqual(0.33d, def.ChainForkDamage);
            Assert.AreEqual(2, def.ChainMaxUnits);
            Assert.AreEqual(60, def.ChainForkRange);
        }

        [TestMethod]
        public async Task Parse_Legbart_Belcher() {
            BarUnit unit = await _ParseUnit("legbart");

            // basic
            Assert.AreEqual("legbart", unit.DefinitionName);
            Assert.AreEqual(false, unit.OnOffAble);

            // weapons
            Assert.AreEqual(1, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[0];
            Assert.AreEqual(1, weapon.Count);
            Assert.AreEqual("SURFACE", weapon.TargetCategory);

            BarWeaponDefinition def = weapon.WeaponDefinition;
            Assert.AreEqual("clusternapalm", def.DefinitionName);
            Assert.AreEqual("HeavyCannon", def.Name);
            Assert.AreEqual(150d, def.AreaOfEffect);
            Assert.AreEqual(0d, def.Burst);
            Assert.AreEqual(0d, def.BurstRate);
            Assert.AreEqual(0d, def.FlightTime);
            Assert.AreEqual(0.123d, def.ImpulseFactor);
            Assert.AreEqual(625d, def.Range);
            Assert.AreEqual(4d, def.ReloadTime);
            Assert.AreEqual(2500d, def.SprayAngle);
            Assert.AreEqual(300d, def.Velocity);
            Assert.AreEqual(false, def.Tracks);
            Assert.AreEqual(0d, def.EnergyPerShot);
            Assert.AreEqual(0d, def.MetalPerShot);
            Assert.AreEqual("Cannon", def.WeaponType);
            Assert.AreEqual(false, def.WaterWeapon);
            Assert.AreEqual(false, def.IsBogus);
            Assert.AreEqual(45d, def.Damages["default"]);
            Assert.AreEqual(10d, def.Damages["vtol"]);
            Assert.AreEqual(10d, def.Damages["subs"]);
            Assert.AreEqual(false, def.IsStockpile);
            Assert.AreEqual(0d, def.StockpileTime);
            Assert.AreEqual(0, def.StockpileLimit);
            Assert.AreEqual(0d, def.ChainForkDamage);
            Assert.AreEqual(0, def.ChainMaxUnits);
            Assert.AreEqual(0, def.ChainForkRange);
            Assert.AreEqual(45d, def.TimedAreaDamage);
            Assert.AreEqual(75d, def.TimedAreaRange);
            Assert.AreEqual(10d, def.TimedAreaTime);
        }

        [TestMethod]
        public async Task Parse_Armsolar_ArmadaSolar() {
            BarUnit unit = await _ParseUnit("armsolar");

            // basic
            Assert.AreEqual("armsolar", unit.DefinitionName);
            Assert.AreEqual(340d, unit.Health);
            Assert.AreEqual(155d, unit.MetalCost);
            Assert.AreEqual(0d, unit.EnergyCost);
            Assert.AreEqual(2600d, unit.BuildTime);
            Assert.AreEqual(0d, unit.Speed);
            Assert.AreEqual(0d, unit.TurnRate);
            Assert.AreEqual(0d, unit.Acceleration);
            Assert.AreEqual(0d, unit.Deceleration);
            Assert.AreEqual(5d, unit.SizeX);
            Assert.AreEqual(5d, unit.SizeZ);

            // eco
            Assert.AreEqual(0d, unit.EnergyProduced);
            Assert.AreEqual(50d, unit.EnergyStorage);
            Assert.AreEqual(-20d, unit.EnergyUpkeep);
            Assert.AreEqual(0d, unit.ExtractsMetal);
            Assert.AreEqual(false, unit.MetalExtractor);
            Assert.AreEqual(0d, unit.MetalProduced);
            Assert.AreEqual(0d, unit.MetalStorage);

            // builder
            Assert.AreEqual(0d, unit.BuildDistance);
            Assert.AreEqual(0d, unit.BuildPower);

            // los
            Assert.AreEqual(273d, unit.SightDistance);
            Assert.AreEqual(273d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);
            Assert.AreEqual(0d, unit.JamDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("Cremuss", unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);
            Assert.AreEqual("smallBuildingexplosiongeneric", unit.ExplodeAs);
            Assert.AreEqual(5d, unit.SelfDestructCountdown);
            Assert.AreEqual("smallBuildingExplosionGenericSelfd", unit.SelfDestructWeapon);
            Assert.AreEqual(0d, unit.AutoHeal);
            Assert.AreEqual(5d, unit.IdleAutoHeal);
            Assert.AreEqual(1800d, unit.IdleTime);
            Assert.AreEqual(0.5d, unit.DamageModifier);
            Assert.AreEqual(true, unit.OnOffAble);

            // weapons
            Assert.AreEqual(0, unit.Weapons.Count);
        }

        [TestMethod]
        public async Task Parse_Legelrpcmech_Astraeus() {
            BarUnit unit = await _ParseUnit("legelrpcmech");

            // basic
            Assert.AreEqual("legelrpcmech", unit.DefinitionName);
            Assert.AreEqual(17000d, unit.Health);
            Assert.AreEqual(11000d, unit.MetalCost);
            Assert.AreEqual(150000d, unit.EnergyCost);
            Assert.AreEqual(125000d, unit.BuildTime);
            Assert.AreEqual(25d, unit.Speed);
            Assert.AreEqual(265.64999, unit.TurnRate);
            Assert.AreEqual(0.02645, unit.Acceleration);
            Assert.AreEqual(0.345, unit.Deceleration);
            Assert.AreEqual(7d, unit.SizeX);
            Assert.AreEqual(7d, unit.SizeZ);

            // eco
            Assert.AreEqual(0d, unit.EnergyProduced);
            Assert.AreEqual(0d, unit.EnergyStorage);
            Assert.AreEqual(0d, unit.EnergyUpkeep);
            Assert.AreEqual(0d, unit.ExtractsMetal);
            Assert.AreEqual(false, unit.MetalExtractor);
            Assert.AreEqual(0d, unit.MetalProduced);
            Assert.AreEqual(0d, unit.MetalStorage);

            // builder
            Assert.AreEqual(0d, unit.BuildDistance);
            Assert.AreEqual(0d, unit.BuildPower);

            // los
            Assert.AreEqual(625d, unit.SightDistance);
            Assert.AreEqual(625d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);
            Assert.AreEqual(0d, unit.JamDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("ZephyrSkies (Model), Phill-Art (Concept Art)", unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);
            Assert.AreEqual("explosiont3", unit.ExplodeAs);
            Assert.AreEqual(5d, unit.SelfDestructCountdown);
            Assert.AreEqual("explosiont3xl", unit.SelfDestructWeapon);
            Assert.AreEqual(0d, unit.AutoHeal);
            Assert.AreEqual(5d, unit.IdleAutoHeal);
            Assert.AreEqual(1800d, unit.IdleTime);
            Assert.AreEqual(1d, unit.DamageModifier);
            Assert.AreEqual(false, unit.OnOffAble);

            // weapons
            Assert.AreEqual(1, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[0];
            Assert.AreEqual(1, weapon.Count);
            Assert.AreEqual("SURFACE", weapon.TargetCategory);

            BarWeaponDefinition def = weapon.WeaponDefinition;
            Assert.AreEqual("shocker_low", def.DefinitionName);
            Assert.AreEqual("Long-Range g2g Heavy Cluster Plasma Cannon", def.Name);
            Assert.AreEqual(150d, def.AreaOfEffect);
            Assert.AreEqual(4d, def.Burst);
            Assert.AreEqual(0.06d, def.BurstRate);
            Assert.AreEqual(0d, def.FlightTime);
            Assert.AreEqual(0.5d, def.ImpulseFactor);
            Assert.AreEqual(3100d, def.Range);
            Assert.AreEqual(12d, def.ReloadTime);
            Assert.AreEqual(300d, def.SprayAngle);
            Assert.AreEqual(1000d, def.Velocity);
            Assert.AreEqual(false, def.Tracks);
            Assert.AreEqual(6000d, def.EnergyPerShot);
            Assert.AreEqual(0d, def.MetalPerShot);
            Assert.AreEqual("Cannon", def.WeaponType);
            Assert.AreEqual(false, def.WaterWeapon);
            Assert.AreEqual(false, def.IsBogus);
            Assert.AreEqual(500d, def.Damages["default"]);
            Assert.AreEqual(250d, def.Damages["shields"]);
            Assert.AreEqual(100d, def.Damages["subs"]);
            Assert.AreEqual(false, def.IsStockpile);
            Assert.AreEqual(0d, def.StockpileTime);
            Assert.AreEqual(0, def.StockpileLimit);
            Assert.AreEqual(0d, def.ChainForkDamage);
            Assert.AreEqual(0, def.ChainMaxUnits);
            Assert.AreEqual(0d, def.ChainForkRange);
            Assert.AreEqual(0d, def.TimedAreaDamage);
            Assert.AreEqual(0d, def.TimedAreaRange);
            Assert.AreEqual(0d, def.TimedAreaTime);
            Assert.AreEqual("CLUSTER_MUNITION", def.ClusterWeaponDefinition);
            Assert.AreEqual(4, def.ClusterNumber);
            Assert.IsNotNull(def.ClusterWeapon);

            BarWeaponDefinition cluster = def.ClusterWeapon;
            Assert.AreEqual(115d, cluster.AreaOfEffect);
            Assert.AreEqual(110d, cluster.Range);
            Assert.AreEqual(11d, cluster.ReloadTime);
            Assert.AreEqual(0d, cluster.Velocity);
            Assert.AreEqual(50d, cluster.Damages["default"]);
            Assert.AreEqual(100d, cluster.Damages["lboats"]);
            Assert.AreEqual(25d, cluster.Damages["subs"]);
            Assert.AreEqual(25d, cluster.Damages["vtol"]);
        }

        [TestMethod]
        public async Task Parse_Legkark_Karkios() {
            BarUnit unit = await _ParseUnit("legkark");

            // basic
            Assert.AreEqual("legkark", unit.DefinitionName);
            Assert.AreEqual(1725d, unit.Health);
            Assert.AreEqual(330d, unit.MetalCost);
            Assert.AreEqual(2600d, unit.EnergyCost);
            Assert.AreEqual(4400d, unit.BuildTime);
            Assert.AreEqual(42.0d, unit.Speed);
            Assert.AreEqual(900d, unit.TurnRate);
            Assert.AreEqual(0.095, unit.Acceleration);
            Assert.AreEqual(0.8211, unit.Deceleration);
            Assert.AreEqual(2d, unit.SizeX);
            Assert.AreEqual(2d, unit.SizeZ);

            // eco
            Assert.AreEqual(0d, unit.EnergyProduced);
            Assert.AreEqual(0d, unit.EnergyStorage);
            Assert.AreEqual(0d, unit.EnergyUpkeep);
            Assert.AreEqual(0d, unit.ExtractsMetal);
            Assert.AreEqual(false, unit.MetalExtractor);
            Assert.AreEqual(0d, unit.MetalProduced);
            Assert.AreEqual(0d, unit.MetalStorage);

            // builder
            Assert.AreEqual(0d, unit.BuildDistance);
            Assert.AreEqual(0d, unit.BuildPower);

            // los
            Assert.AreEqual(400d, unit.SightDistance);
            Assert.AreEqual(400d * 1.5d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);
            Assert.AreEqual(0d, unit.JamDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("Tharsis", unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);
            Assert.AreEqual("smallExplosionGeneric", unit.ExplodeAs);
            Assert.AreEqual(5d, unit.SelfDestructCountdown);
            Assert.AreEqual("smallExplosionGenericSelfd", unit.SelfDestructWeapon);
            Assert.AreEqual(0d, unit.AutoHeal);
            Assert.AreEqual(5d, unit.IdleAutoHeal);
            Assert.AreEqual(1800d, unit.IdleTime);
            Assert.AreEqual(0.5d, unit.DamageModifier);
            Assert.AreEqual(false, unit.OnOffAble);
            Assert.AreEqual(300d, unit.ReactiveArmorHealth);
            Assert.AreEqual(15d, unit.ReactiveArmorRestore);

            // weapons
            Assert.AreEqual(2, unit.Weapons.Count);
        }

    }
}
