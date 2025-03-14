using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Bar;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db {

    public class BarMapDb {

        private readonly ILogger<BarMapDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarMapDb(ILogger<BarMapDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task Upsert(BarMap map) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_map (
                    id, name, filename, description,
                    tidal_strength, max_metal, extractor_radius, minimum_wind, maximum_wind,
                    width, height, author
                ) VALUES (
                    @ID, @Name, @FileName, @Description,
                    @TidalStrength, @MaxMetal, @ExtractorRadius, @MinimumWind, @MaximumWind,
                    @Width, @Height, @Author
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
                        author = @Author;
            ");

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
            await cmd.PrepareAsync();

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

        public async Task<BarMap?> GetByID(int mapID) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryFirstOrDefaultAsync<BarMap>(
                "SELECT * FROM bar_map WHERE id = @ID",
                new { ID = mapID }
            );
        }

        public async Task<BarMap?> GetByFileName(string mapName) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryFirstOrDefaultAsync<BarMap>(
                "SELECT * FROM bar_map WHERE filename = @FileName",
                new { FileName = mapName }
            );
        }

    }
}
