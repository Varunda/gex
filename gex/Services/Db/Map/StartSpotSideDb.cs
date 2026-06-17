using gex.Code.ExtensionMethods;
using gex.Models.Map;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public class StartSpotSideDb : BaseStartSpotDb<StartSpotSide> {

        public StartSpotSideDb(ILoggerFactory loggerFactory, IDbHelper helper)
            : base(loggerFactory, helper, "start_spot_side") {
        }

        protected override void InsertSetup(NpgsqlCommand cmd, StartSpotSide inst) {
            cmd.CommandText = @"
                INSERT INTO start_spot_side (
                    map_filename, version, players_per_team, team_count, index
                ) VALUES (
                    @MapFilename, @Version, @PlayersPerTeam, @TeamCount, @Index
                );
            ";

            cmd.AddParameter("MapFilename", inst.MapFilename);
            cmd.AddParameter("Version", inst.Version);
            cmd.AddParameter("PlayersPerTeam", inst.PlayersPerTeam);
            cmd.AddParameter("TeamCount", inst.TeamCount);
            cmd.AddParameter("Index", inst.Index);
        }

    }
}
