using gex.Common.Code.ExtensionMethods;
using gex.Common.Models.Match;
using gex.Common.Services.Db;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Reader {

    public class BarMatchDbReader : IDataReader<BarMatch> {

        public override BarMatch? ReadEntry(DbDataReader reader) {
            BarMatch match = new();

            match.ID = reader.GetString("id");
            match.Engine = reader.GetString("engine");
            match.GameVersion = reader.GetString("game_version");
            match.FileName = reader.GetString("file_name");
            match.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(reader.GetInt64("start_time")).UtcDateTime;
            match.StartOffset = reader.GetFloat("start_offset");
            match.DurationMs = reader.GetInt64("duration_ms");
            match.DurationFrameCount = reader.GetInt64("duration_frame_count");
            match.Map = reader.GetString("map");
            match.MapName = reader.GetString("map_name");
            match.Gamemode = reader.GetByte("gamemode");
            match.PlayerCount = reader.GetInt32("player_count");
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

            return match;
        }
    }
}
