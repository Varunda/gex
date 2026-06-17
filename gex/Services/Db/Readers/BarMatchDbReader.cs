using gex.Code.ExtensionMethods;
using gex.Models.Db;
using Npgsql;
using Npgsql.Schema;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;

namespace gex.Services.Db.Readers {

    public class BarMatchDbReader : IDataReader<BarMatch> {

        public override BarMatch? ReadEntry(NpgsqlDataReader reader) {
            BarMatch match = new();

            match.ID = reader.GetString("id");
            match.Engine = reader.GetString("engine");
            match.GameVersion = reader.GetString("game_version");
            match.FileName = reader.GetString("file_name");
            match.StartTime = reader.GetDateTime("start_time");
            match.StartOffset = reader.GetFloat("start_offset");
            match.DurationMs = reader.GetInt64("duration_ms");
            match.DurationFrameCount = reader.GetInt64("duration_frame_count");
            match.Map = reader.GetString("map");
            match.MapName = reader.GetString("map_name");
            match.Gamemode = reader.GetByte("gamemode");
            match.PlayerCount = reader.GetInt32("player_count");
            match.UploadedByID = reader.GetNullableInt32("uploaded_by");
            match.WrongSkillValues = reader.GetBoolean("wrong_skill_values");
            match.AverageOS = reader.GetFloat("average_os");
            match.MinOS = reader.GetFloat("min_os");
            match.MaxOS = reader.GetFloat("max_os");
            match.StartSpotVersion = reader.GetNullableInt32("start_spot_version");

            match.HostSettings = reader.GetJsonb("host_settings");
            match.GameSettings = reader.GetJsonb("game_settings");
            match.MapSettings = reader.GetJsonb("map_settings");
            match.SpadsSettings = reader.GetJsonb("spads_settings");
            match.Restrictions = reader.GetJsonb("restrictions");

            ReadOnlyCollection<NpgsqlDbColumn> columns = reader.GetColumnSchema();

            bool hasDesc = columns.FirstOrDefault(iter => iter.ColumnName == "description") != null;
            if (hasDesc == true) {
                match.MatchPoolEntryNote = reader.GetNullableString("description");
            }

            bool hasHideUntil = columns.FirstOrDefault(iter => iter.ColumnName == "hide_until") != null;
            if (hasHideUntil == true) {
                DateTime? matchPoolHideUntil = reader.GetNullableDateTime("hide_until");
                match.MatchPoolIsHidden = matchPoolHideUntil != null && DateTime.UtcNow < matchPoolHideUntil.Value;
            }

            return match;
        }

    }
}
