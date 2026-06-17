using gex.Code.ExtensionMethods;
using gex.Models.Map;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public class StartSpotDataDb : BaseStartSpotDb<StartSpotData> {

        public StartSpotDataDb(ILoggerFactory loggerFactory, IDbHelper helper)
            : base(loggerFactory, helper, "start_spot_data") {

        }

        protected override void InsertSetup(NpgsqlCommand cmd, StartSpotData inst) {
            cmd.CommandText = @"
                INSERT INTO start_spot_data (
                    map_filename, version, timestamp, min_timestamp, raw, max_timestamp
                ) VALUES (
                    @MapFilename, @Version, @Timestamp, @MinTimestamp, @Raw, null
                );
            ";

            cmd.AddParameter("MapFilename", inst.MapFilename);
            cmd.AddParameter("Version", inst.Version);
            cmd.AddParameter("Timestamp", inst.Timestamp);
            cmd.AddParameter("MinTimestamp", inst.MinTimestamp);
            cmd.AddParameter("Raw", inst.Raw);
        }

        public async Task UpdateMaxTimestamp(StartSpotData data, DateTime timestamp, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                UPDATE start_spot_data
                    SET max_timestamp = @MaxTimestamp
                    WHERE map_filename = @MapFilename
                        AND version = @Version;
            ", cancel);

            cmd.AddParameter("MaxTimestamp", timestamp);
            cmd.AddParameter("MapFilename", data.MapFilename);
            cmd.AddParameter("Version", data.Version);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
