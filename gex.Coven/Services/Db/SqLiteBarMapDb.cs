using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Map;
using gex.Common.Services.Db;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db {

    public class SqLiteBarMapDb : IBarMapDb {

        private readonly ILogger<SqLiteBarMapDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public SqLiteBarMapDb(ILogger<SqLiteBarMapDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<BarMap>> GetAll(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            return await conn.QueryListAsync<BarMap>("SELECT * FROM bar_map", cancel);
        }

        public async Task<BarMap?> GetByFileName(string mapName, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            return await conn.QuerySingleAsync<BarMap>(
                "SELECT * FROM bar_map WHERE filename = @MapName",
                new { MapName = mapName },
                cancel
            );
        }

        public async Task<BarMap?> GetByID(int mapID, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            return await conn.QuerySingleAsync<BarMap>(
                "SELECT * FROM bar_map WHERE id = @ID",
                new { ID = mapID },
                cancel
            );
        }

        public async Task<BarMap?> GetByName(string name, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection();
            return await conn.QuerySingleAsync<BarMap>(
                "SELECT * FROM bar_map WHERE name = @Name",
                new { Name = name },
                cancel
            );
        }

        public async Task Upsert(BarMap map, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_map (
                    id, name, filename, description,
                    tidal_strength, max_metal, extractor_radius, minimum_wind, maximum_wind,
                    width, height, author, timestamp, symmetry_axis
                ) VALUES (
                    @ID, @Name, @FileName, @Description,
                    @TidalStrength, @MaxMetal, @ExtractorRadius, @MinimumWind, @MaximumWind,
                    @Width, @Height, @Author, @Now, @SymmetryAxis
                ) ON CONFLICT (id) DO UPDATE
                    SET name = @Name,
                        filename = @FileName,
                        description = @Description,
                        tidal_strength = @TidalStrength,
                        max_metal = @MaxMetal,
                        extractor_radius = @ExtractorRadius,
                        minimum_wind = @MinimumWind,
                        maximum_wind = @MaximumWind,
                        width = @Width,
                        height = @Height,
                        author = @Author,
                        timestamp = @Now,
                        symmetry_axis = @SymmetryAxis;
            ");

            cmd.AddParameter("Now", DateTime.UtcNow);
            cmd.AddParameter("ID", map.ID);
            cmd.AddParameter("Name", map.Name);
            cmd.AddParameter("FileName", map.FileName);
            cmd.AddParameter("Description", map.Description);
            cmd.AddParameter("TidalStrength", map.TidalStrength);
            cmd.AddParameter("MaxMetal", map.MaxMetal);
            cmd.AddParameter("ExtractorRadius", map.ExtractorRadius);
            cmd.AddParameter("MinimumWind", map.MinimumWind);
            cmd.AddParameter("MaximumWind", map.MaximumWind);
            cmd.AddParameter("Width", map.Width);
            cmd.AddParameter("Height", map.Height);
            cmd.AddParameter("Author", map.Author);
            cmd.AddParameter("SymmetryAxis", (int?)map.SymmetryAxis);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
