using gex.Code.ExtensionMethods;
using gex.Models.Map;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public class StartSpotPositionDb : BaseStartSpotDb<StartSpotPosition> {

        public StartSpotPositionDb(ILoggerFactory loggerFactory, IDbHelper helper)
            : base(loggerFactory, helper, "start_spot_position") {
        }

        protected override void InsertSetup(NpgsqlCommand cmd, StartSpotPosition inst) {
            cmd.CommandText = @"
                INSERT INTO start_spot_position (
                    map_filename, version, name, x, y
                ) VALUES (
                    @MapFilename, @Version, @Name, @X, @Y
                );
            ";

            cmd.AddParameter("MapFilename", inst.MapFilename);
            cmd.AddParameter("Version", inst.Version);
            cmd.AddParameter("Name", inst.Name);
            cmd.AddParameter("X", inst.X);
            cmd.AddParameter("Y", inst.Y);
        }

    }
}
