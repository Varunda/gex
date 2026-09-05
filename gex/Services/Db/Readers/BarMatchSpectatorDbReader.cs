using gex.Common.Models.Match;
using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Data;

namespace gex.Services.Db.Readers {

    public class BarMatchSpectatorDbReader : IDataReader<BarMatchSpectator> {

        public override BarMatchSpectator? ReadEntry(DbDataReader reader) {
            BarMatchSpectator spec = new();

            spec.GameID = reader.GetString("game_id");
            spec.UserID = reader.GetInt64("user_id");
            spec.Name = reader.GetString("user_name");

            return spec;
        }

    }
}
