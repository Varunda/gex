using gex.Models;
using gex.Models.Bar;
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
            ILogger<BarUnitParser> logger = new TestLogger<BarUnitParser>();
            BarUnitParser parser = new(logger);
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
            Assert.AreEqual(0d, unit.AirSightDistance);
            Assert.AreEqual(0d, unit.RadarDistance);
            Assert.AreEqual(0d, unit.SonarDistance);

            // transport
            Assert.AreEqual(0d, unit.TransportCapacity);
            Assert.AreEqual(0d, unit.TransportMass);
            Assert.AreEqual(0d, unit.TransportSize);

            // misc
            Assert.AreEqual("Kaiser", unit.ModelAuthor);
            Assert.AreEqual(0d, unit.CloakCostStill);
            Assert.AreEqual(0d, unit.CloakCostMoving);

            // weapons
            Assert.AreEqual(1, unit.Weapons.Count);
            BarUnitWeapon weapon = unit.Weapons[0];

            Assert.AreEqual("emg", weapon.DefinitionName);
            Assert.AreEqual("Rapid-fire close-quarters g2g plasma guns", weapon.Name);
            Assert.AreEqual(8d, weapon.AreaOfEffect);
            Assert.AreEqual(3d, weapon.Burst);
            Assert.AreEqual(0.1d, weapon.BurstRate);
            Assert.AreEqual(0d, weapon.FlightTime);
            Assert.AreEqual(0.123d, weapon.ImpulseFactor);
            Assert.AreEqual(180d, weapon.Range);
            Assert.AreEqual(0.3d, weapon.ReloadTime);
            Assert.AreEqual(0d, weapon.TurnRate);
            Assert.AreEqual(500d, weapon.Velocity);
            Assert.AreEqual(false, weapon.Tracks);
            Assert.AreEqual(0d, weapon.EnergyPerShot);
            Assert.AreEqual(0d, weapon.MetalPerShot);
            Assert.AreEqual("Cannon", weapon.WeaponType);
            Assert.AreEqual("NOTSUB", weapon.TargetCategory);
            Assert.AreEqual(false, weapon.WaterWeapon);
            Assert.AreEqual(9d, weapon.Damages["default"]);
            Assert.AreEqual(3d, weapon.Damages["vtol"]);
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

            // los
            Assert.AreEqual(450d, unit.SightDistance);
            Assert.AreEqual(0d, unit.AirSightDistance);
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

            // weapons
            Assert.AreEqual(3, unit.Weapons.Count);

            Assert.AreEqual(true, unit.Weapons[2].WaterWeapon);
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
            Assert.AreEqual(0d, unit.AirSightDistance);
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
            Assert.AreEqual(0d, unit.AirSightDistance);
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

            // weapons
            Assert.AreEqual(1, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[0];
            Assert.IsNotNull(weapon.ShieldData);
            Assert.AreEqual(2.5d, weapon.ShieldData.Force);
            Assert.AreEqual(3250d, weapon.ShieldData.Power);
            Assert.AreEqual(52d, weapon.ShieldData.PowerRegen);
            Assert.AreEqual(562.5, weapon.ShieldData.PowerRegenEnergy);
            Assert.AreEqual(600d, weapon.ShieldData.Radius);
            Assert.AreEqual(1100d, weapon.ShieldData.StartingPower);
            Assert.AreEqual(true, weapon.ShieldData.Repulser);
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
            Assert.AreEqual(0d, unit.AirSightDistance);
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

            // weapons
            Assert.AreEqual(7, unit.Weapons.Count);

            BarUnitWeapon weapon = unit.Weapons[5];
            Assert.AreEqual(true, weapon.IsBogus);
        }

    }
}
