using gex.Code.ExtensionMethods;
using gex.Models.Map;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public class StartSpotSideStartRoleOverrideDb : BaseStartSpotDb<StartSpotSideStartRoleOverride> {

        public StartSpotSideStartRoleOverrideDb(ILoggerFactory loggerFactory, IDbHelper helper)
            : base(loggerFactory, helper, "start_spot_side_start_role_override") {
        }

        protected override void InsertSetup(NpgsqlCommand cmd, StartSpotSideStartRoleOverride inst) {
            throw new NotImplementedException("use Upsert instead");
        }

        public async Task Upsert(StartSpotSideStartRoleOverride @override, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO start_spot_side_start_role_override (
                    map_filename, version, position, role, max_radius, timestamp
                ) VALUES (
                    @MapFilename, @Version, @Position, @Role, @MaxRadius, NOW() at time zone 'utc'
                ) ON CONFLICT (map_filename, version, position) DO UPDATE
                    SET role = @Role,
                        max_radius = @MaxRadius,
                        timestamp = NOW() at time zone 'utc'; 
            ", cancel);

            cmd.AddParameter("MapFilename", @override.MapFilename);
            cmd.AddParameter("Version", @override.Version);
            cmd.AddParameter("Position", @override.Position);
            cmd.AddParameter("Role", @override.Role);
            cmd.AddParameter("MaxRadius", @override.MaxRadius);
            cmd.AddParameter("Timestamp", @override.Timestamp);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
