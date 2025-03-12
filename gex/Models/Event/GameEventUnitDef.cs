using Dapper.ColumnMapper;
using gex.Code;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitDef : GameEvent {

        [ColumnMapping("hash")]
        public string Hash { get; set; } = "";

        [JsonPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonPropertyName("defName")]
        [ColumnMapping("definition_name")]
        public string DefinitionName { get; set; } = "";

        [JsonPropertyName("name")]
        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("tooltip")]
        [ColumnMapping("tooltip")]
        public string Tooltip { get; set; } = "";

        [JsonPropertyName("metalCost")]
        [ColumnMapping("metal_cost")]
        public int MetalCost { get; set; } = 0;

        [JsonPropertyName("energyCost")]
        [ColumnMapping("energy_cost")]
        public int EnergyCost { get; set; }

        [JsonPropertyName("health")]
        [ColumnMapping("health")]
        public int Health { get; set; }

        [JsonPropertyName("speed")]
        [ColumnMapping("speed")]
        public double Speed { get; set; }

        [JsonPropertyName("buildTime")]
        [ColumnMapping("build_time")]
        public double BuildTime { get; set; }

        [JsonPropertyName("unitGroup")]
        [ColumnMapping("unit_group")]
        public string UnitGroup { get; set; } = "";

        [JsonPropertyName("buildPower")]
        [ColumnMapping("build_power")]
        public double BuildPower { get; set; }

        [JsonPropertyName("metalMake")]
        [ColumnMapping("metal_make")]
        public double MetalMake { get; set; }

        [JsonPropertyName("isMetalExtractor")]
        [ColumnMapping("is_metal_extractor")]
        public bool IsMetalExtractor { get; set; }

        [JsonPropertyName("extractsMetal")]
        [ColumnMapping("extracts_metal")]
        public double ExtractsMetal { get; set; }

        [JsonPropertyName("metalStorage")]
        [ColumnMapping("metal_storage")]
        public double MetalStorage { get; set; }

        [JsonPropertyName("windGenerator")]
        [ColumnMapping("wind_generator")]
        public double WindGenerator { get; set; }

        [JsonPropertyName("tidalGenerator")]
        [ColumnMapping("tidal_generator")]
        public double TidalGenerator { get; set; }

        [JsonPropertyName("energyProduction")]
        [ColumnMapping("energy_production")]
        public double EnergyProduction { get; set; }

        [JsonPropertyName("energyUpkeep")]
        [ColumnMapping("energy_upkeep")]
        public double EnergyUpkeep { get; set; }

        [JsonPropertyName("energyStorage")]
        [ColumnMapping("energy_storage")]
        public double EnergyStorage { get; set; }

        [JsonPropertyName("energyConversionCapacity")]
        [ColumnMapping("energy_conversion_capacity")]
        public double EnergyConversionCapacity { get; set; }

        [JsonPropertyName("energyConversionEfficiency")]
        [ColumnMapping("energy_conversion_efficiency")]
        public double EnergyConversionEfficiency { get; set; }

        [JsonPropertyName("sightDistance")]
        [ColumnMapping("sight_distance")]
        public double SightDistance { get; set; }

        [JsonPropertyName("airSightDistance")]
        [ColumnMapping("air_sight_distance")]
        public double AirSightDistance { get; set; }

        [JsonPropertyName("attackRange")]
        [ColumnMapping("attack_range")]
        public double AttackRange { get; set; }

        [JsonPropertyName("isCommander")]
        [ColumnMapping("is_commander")]
        public bool IsCommander { get; set; }

        [JsonPropertyName("isReclaimer")]
        [ColumnMapping("is_reclaimer")]
        public bool IsReclaimer { get; set; }

        [JsonPropertyName("isFactory")]
        [ColumnMapping("is_factory")]
        public bool IsFactory { get; set; }

        [JsonPropertyName("weaponCount")]
        [ColumnMapping("weapon_count")]
        public int WeaponCount { get; set; }

        /// <summary>
        ///     compute a hash that does not change, i.e. is deterministic.
        ///     the default HashCode implementation is randomly seeded, so unit def hashes
        ///     will change each start up, causing duplicate code
        /// </summary>
        /// <returns></returns>
        public int GetDefinitionHash() {

            int hash = 5381;

            hash ^= GetIntHash(DefinitionID);
            hash ^= GetStringHash(DefinitionName);
            hash ^= GetStringHash(Name);
            hash ^= GetStringHash(Tooltip);
            hash ^= GetIntHash(MetalCost);
            hash ^= GetIntHash(EnergyCost);
            hash ^= GetIntHash(Health);
            hash ^= GetDoubleHash(Speed);
            hash ^= GetDoubleHash(BuildTime);
            hash ^= GetStringHash(UnitGroup);
            hash ^= GetDoubleHash(BuildPower);
            hash ^= GetDoubleHash(MetalMake);
            hash ^= (int)(IsMetalExtractor ? 0xAAAAAAAA : 0x55555555);
            hash ^= GetDoubleHash(ExtractsMetal);
            hash ^= GetDoubleHash(MetalStorage);
            hash ^= GetDoubleHash(WindGenerator);
            hash ^= GetDoubleHash(TidalGenerator);
            hash ^= GetDoubleHash(EnergyProduction);
            hash ^= GetDoubleHash(EnergyUpkeep);
            hash ^= GetDoubleHash(EnergyStorage);
            hash ^= GetDoubleHash(EnergyConversionCapacity);
            hash ^= GetDoubleHash(EnergyConversionEfficiency);
            hash ^= GetDoubleHash(SightDistance);
            hash ^= GetDoubleHash(AirSightDistance);
            hash ^= GetDoubleHash(AttackRange);
            hash ^= (int)(IsCommander ? 0xAAAAAAAA : 0x55555555);
            hash ^= (int)(IsReclaimer ? 0xAAAAAAAA : 0x55555555);
            hash ^= (int)(IsFactory ? 0xAAAAAAAA : 0x55555555);
            hash ^= GetIntHash(WeaponCount);

            return hash;
        }

        // http://www.cse.yorku.ca/~oz/hash.html
        private static int GetStringHash(string str) {
            int hash = 5381;

            foreach (char c in str) {
                hash = ((hash << 5) + hash) + c;
            }

            return hash;
        }

        // https://stackoverflow.com/questions/664014/what-integer-hash-function-are-good-that-accepts-an-integer-hash-key
        private static int GetIntHash(int i) {
            i = ((i >> 16) ^ i) * 0x45d9feb;
            i = ((i >> 16) ^ i) * 0x45d9feb;
            i = (i >> 16) ^ i;
            return i;
        }

        private static int GetDoubleHash(double d) {
            int hash = 0;
            unchecked {
                hash ^= (int)((BitConverter.DoubleToInt64Bits(d) >> 32) & 0xFFFFFFFF);
                hash ^= (int)((BitConverter.DoubleToInt64Bits(d) >> 0) & 0xFFFFFFFF);
            }
            return hash;
        }

    }
}
