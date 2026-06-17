using gex.Code.ExtensionMethods;
using gex.Models.Map;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public class StartSpotConfigurationDb : BaseStartSpotDb<StartSpotConfiguration> {

        public StartSpotConfigurationDb(ILoggerFactory loggerFactory, IDbHelper helper)
            : base(loggerFactory, helper, "start_spot_configuration") {

        }

        protected override void InsertSetup(NpgsqlCommand cmd, StartSpotConfiguration inst) {
            cmd.CommandText = @"
                INSERT INTO start_spot_configuration (
                    map_filename, version, players_per_team, team_count
                ) VALUES (
                    @MapFilename, @Version, @PlayersPerTeam, @TeamCount
                );
            ";

            cmd.AddParameter("MapFilename", inst.MapFilename);
            cmd.AddParameter("Version", inst.Version);
            cmd.AddParameter("PlayersPerTeam", inst.PlayersPerTeam);
            cmd.AddParameter("TeamCount", inst.TeamCount);
        }

    }
}
