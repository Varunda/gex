using gex.Code.ExtensionMethods;
using gex.Common.Models.Map;
using gex.Common.Services.Db;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public class StartSpotSideStartDb : BaseStartSpotDb<StartSpotSideStart> {

        public StartSpotSideStartDb(ILoggerFactory loggerFactory, IDbHelper helper)
            : base(loggerFactory, helper, "start_spot_side_start") {

        }

        protected override void InsertSetup(DbCommand cmd, StartSpotSideStart inst) {
            cmd.CommandText = @"
                INSERT INTO start_spot_side_start (
                    map_filename, version, side_index, role, spawn_point, base_center
                ) VALUES (
                    @MapFilename, @Version, @SideIndex, @Role, @SpawnPoint, @BaseCenter
                );
            ";

            cmd.AddParameter("MapFilename", inst.MapFilename);
            cmd.AddParameter("Version", inst.Version);
            cmd.AddParameter("SideIndex", inst.SideIndex);
            cmd.AddParameter("Role", inst.Role);
            cmd.AddParameter("SpawnPoint", inst.SpawnPoint);
            cmd.AddParameter("BaseCenter", inst.BaseCenter);
        }

    }
}
