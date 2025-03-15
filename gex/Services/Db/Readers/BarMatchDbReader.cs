using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Npgsql;
using System.Data;
using System.Text.Json;

namespace gex.Services.Db.Readers {

    public class BarMatchDbReader : IDataReader<BarMatch> {

        public override BarMatch? ReadEntry(NpgsqlDataReader reader) {
            BarMatch match = new();

            match.ID = reader.GetString("id");
            match.Engine = reader.GetString("engine");
            match.GameVersion = reader.GetString("game_version");
            match.FileName = reader.GetString("file_name");
            match.StartTime = reader.GetDateTime("start_time");
            match.DurationMs = reader.GetInt64("duration_ms");
            match.Map = reader.GetString("map");
            match.MapName = reader.GetString("map_name");

            match.HostSettings = reader.GetJsonb("host_settings");
            match.GameSettings = reader.GetJsonb("game_settings");
            match.MapSettings = reader.GetJsonb("map_settings");
            match.SpadsSettings = reader.GetJsonb("spads_settings");
            match.Restrictions = reader.GetJsonb("restrictions");

            return match;
        }

    }
}
