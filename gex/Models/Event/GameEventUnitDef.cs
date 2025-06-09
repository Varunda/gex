using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Event {

    [DapperColumnsMapped]
    public class GameEventUnitDef : GameEvent {

        [ColumnMapping("hash")]
        public string Hash { get; set; } = "";

        [JsonActionLogPropertyName("defID")]
        [ColumnMapping("definition_id")]
        public int DefinitionID { get; set; }

        [JsonActionLogPropertyName("defName")]
        [ColumnMapping("definition_name")]
        public string DefinitionName { get; set; } = "";

        [JsonActionLogPropertyName("name")]
        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        [JsonActionLogPropertyName("tooltip")]
        [ColumnMapping("tooltip")]
        public string Tooltip { get; set; } = "";

        [JsonActionLogPropertyName("metalCost")]
        [ColumnMapping("metal_cost")]
        public double MetalCost { get; set; } = 0;

        [JsonActionLogPropertyName("energyCost")]
        [ColumnMapping("energy_cost")]
        public double EnergyCost { get; set; }

        [JsonActionLogPropertyName("health")]
        [ColumnMapping("health")]
        public double Health { get; set; }

        [JsonActionLogPropertyName("speed")]
        [ColumnMapping("speed")]
        public double Speed { get; set; }

        [JsonActionLogPropertyName("sizeX")]
        [ColumnMapping("size_x")]
        public double SizeX { get; set; }

        [JsonActionLogPropertyName("sizeZ")]
        [ColumnMapping("size_z")]
        public double SizeZ { get; set; }

        [JsonActionLogPropertyName("buildTime")]
        [ColumnMapping("build_time")]
        public double BuildTime { get; set; }

        [JsonActionLogPropertyName("unitGroup")]
        [ColumnMapping("unit_group")]
        public string UnitGroup { get; set; } = "";

        [JsonActionLogPropertyName("buildPower")]
        [ColumnMapping("build_power")]
        public double BuildPower { get; set; }

        [JsonActionLogPropertyName("metalMake")]
        [ColumnMapping("metal_make")]
        public double MetalMake { get; set; }

        [JsonActionLogPropertyName("isMetalExtractor")]
        [ColumnMapping("is_metal_extractor")]
        public bool IsMetalExtractor { get; set; }

        [JsonActionLogPropertyName("extractsMetal")]
        [ColumnMapping("extracts_metal")]
        public double ExtractsMetal { get; set; }

        [JsonActionLogPropertyName("metalStorage")]
        [ColumnMapping("metal_storage")]
        public double MetalStorage { get; set; }

        [JsonActionLogPropertyName("windGenerator")]
        [ColumnMapping("wind_generator")]
        public double WindGenerator { get; set; }

        [JsonActionLogPropertyName("tidalGenerator")]
        [ColumnMapping("tidal_generator")]
        public double TidalGenerator { get; set; }

        [JsonActionLogPropertyName("energyProduction")]
        [ColumnMapping("energy_production")]
        public double EnergyProduction { get; set; }

        [JsonActionLogPropertyName("energyUpkeep")]
        [ColumnMapping("energy_upkeep")]
        public double EnergyUpkeep { get; set; }

        [JsonActionLogPropertyName("energyStorage")]
        [ColumnMapping("energy_storage")]
        public double EnergyStorage { get; set; }

        [JsonActionLogPropertyName("energyConversionCapacity")]
        [ColumnMapping("energy_conversion_capacity")]
        public double EnergyConversionCapacity { get; set; }

        [JsonActionLogPropertyName("energyConversionEfficiency")]
        [ColumnMapping("energy_conversion_efficiency")]
        public double EnergyConversionEfficiency { get; set; }

        [JsonActionLogPropertyName("sightDistance")]
        [ColumnMapping("sight_distance")]
        public double SightDistance { get; set; }

        [JsonActionLogPropertyName("airSightDistance")]
        [ColumnMapping("air_sight_distance")]
        public double AirSightDistance { get; set; }

        [JsonActionLogPropertyName("attackRange")]
        [ColumnMapping("attack_range")]
        public double AttackRange { get; set; }

        [JsonActionLogPropertyName("radarDistance")]
        [ColumnMapping("radar_distance")]
        public double RadarDistance { get; set; }

        [JsonActionLogPropertyName("isCommander")]
        [ColumnMapping("is_commander")]
        public bool IsCommander { get; set; }

        [JsonActionLogPropertyName("isReclaimer")]
        [ColumnMapping("is_reclaimer")]
        public bool IsReclaimer { get; set; }

        [JsonActionLogPropertyName("isFactory")]
        [ColumnMapping("is_factory")]
        public bool IsFactory { get; set; }

        [JsonActionLogPropertyName("weaponCount")]
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
            hash ^= (int)(IsCommander ? 0xAAAAAAAA : 0x55555555);
            hash ^= GetDoubleHash(MetalCost);
            hash ^= GetDoubleHash(EnergyCost);
            hash ^= GetDoubleHash(Health);
            hash ^= GetDoubleHash(Speed);
            hash ^= GetDoubleHash(SizeX);
            hash ^= GetDoubleHash(SizeZ);
            hash ^= GetDoubleHash(BuildTime);
            hash ^= GetStringHash(UnitGroup);
            hash ^= GetDoubleHash(BuildPower);
            hash ^= GetDoubleHash(MetalMake);
            hash ^= (int)(IsMetalExtractor ? 0xAAAAAAAA : 0x55555555);
            hash ^= GetDoubleHash(ExtractsMetal);
            hash ^= GetDoubleHash(MetalStorage);
            hash ^= GetDoubleHash(WindGenerator);
            hash ^= GetDoubleHash(TidalGenerator);
            hash ^= (int)(IsReclaimer ? 0xAAAAAAAA : 0x55555555);
            hash ^= GetDoubleHash(EnergyProduction);
            hash ^= GetDoubleHash(EnergyUpkeep);
            hash ^= GetDoubleHash(EnergyStorage);
            hash ^= GetDoubleHash(EnergyConversionCapacity);
            hash ^= GetDoubleHash(EnergyConversionEfficiency);
            hash ^= GetDoubleHash(SightDistance);
            hash ^= GetDoubleHash(AirSightDistance);
            hash ^= GetDoubleHash(RadarDistance);
            hash ^= GetDoubleHash(AttackRange);
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
