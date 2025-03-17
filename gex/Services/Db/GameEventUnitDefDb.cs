using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class GameEventUnitDefDb {

        private readonly ILogger<GameEventUnitDefDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public GameEventUnitDefDb(ILogger<GameEventUnitDefDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Insert(GameEventUnitDef ev) {
            if (string.IsNullOrEmpty(ev.Hash)) {
                throw new System.Exception($"missing hash from unit def");
            }

            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO unit_def_set_entry (
                    hash, definition_id, definition_name, name, tooltip, size_x, size_z,
                    metal_cost, energy_cost, health, speed, build_time, unit_group, build_power,
                    metal_make, is_metal_extractor, extracts_metal, metal_storage,
                    wind_generator, tidal_generator, energy_production, energy_upkeep, energy_storage,
                    energy_conversion_capacity, energy_conversion_efficiency,
                    sight_distance, air_sight_distance, attack_range, radar_distance,
                    is_commander, is_reclaimer, is_factory, weapon_count
                ) VALUES (
                    @Hash, @DefinitionID, @DefinitionName, @Name, @Tooltip, @SizeX, @SizeZ,
                    @MetalCost, @EnergyCost, @Health, @Speed, @BuildTime, @UnitGroup, @BuildPower,
                    @MetalMake, @IsMetalExtractor, @ExtractsMetal, @MetalStorage,
                    @WindGenerator, @TidalGenerator, @EnergyProduction, @EnergyUpkeep, @EnergyStorage,
                    @EnergyConversionCapacity, @EnergyConversionEfficiency,
                    @SightDistance, @AirSightDistance, @AttackRange, @RadarDistance,
                    @IsCommander, @IsReclaimer, @IsFactory, @WeaponCount
                );
            ");

            cmd.AddParameter("Hash", ev.Hash);
            cmd.AddParameter("DefinitionID", ev.DefinitionID);
            cmd.AddParameter("DefinitionName", ev.DefinitionName);
            cmd.AddParameter("Name", ev.Name);
            cmd.AddParameter("Tooltip", ev.Tooltip);
            cmd.AddParameter("MetalCost", ev.MetalCost);
            cmd.AddParameter("EnergyCost", ev.EnergyCost);
            cmd.AddParameter("Health", ev.Health);
            cmd.AddParameter("Speed", ev.Speed);
            cmd.AddParameter("SizeX", ev.SizeX);
            cmd.AddParameter("SizeZ", ev.SizeZ);
            cmd.AddParameter("BuildTime", ev.BuildTime);
            cmd.AddParameter("UnitGroup", ev.UnitGroup);
            cmd.AddParameter("BuildPower", ev.BuildPower);
            cmd.AddParameter("MetalMake", ev.MetalMake);
            cmd.AddParameter("IsMetalExtractor", ev.IsMetalExtractor);
            cmd.AddParameter("ExtractsMetal", ev.ExtractsMetal);
            cmd.AddParameter("MetalStorage", ev.MetalStorage);
            cmd.AddParameter("WindGenerator", ev.WindGenerator);
            cmd.AddParameter("TidalGenerator", ev.TidalGenerator);
            cmd.AddParameter("EnergyProduction", ev.EnergyProduction);
            cmd.AddParameter("EnergyUpkeep", ev.EnergyUpkeep);
            cmd.AddParameter("EnergyStorage", ev.EnergyStorage);
            cmd.AddParameter("EnergyConversionCapacity", ev.EnergyConversionCapacity);
            cmd.AddParameter("EnergyConversionEfficiency", ev.EnergyConversionEfficiency);
            cmd.AddParameter("SightDistance", ev.SightDistance);
            cmd.AddParameter("AirSightDistance", ev.AirSightDistance);
            cmd.AddParameter("AttackRange", ev.AttackRange);
            cmd.AddParameter("RadarDistance", ev.RadarDistance);
            cmd.AddParameter("IsCommander", ev.IsCommander);
            cmd.AddParameter("IsReclaimer", ev.IsReclaimer);
            cmd.AddParameter("IsFactory", ev.IsFactory);
            cmd.AddParameter("WeaponCount", ev.WeaponCount);
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<List<GameEventUnitDef>> GetByHash(string hash) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);

            return (await conn.QueryAsync<GameEventUnitDef>(
                "SELECT * FROM unit_def_set_entry WHERE hash = @Hash",
                new { Hash = hash }
            )).ToList();
        }

    }
}
