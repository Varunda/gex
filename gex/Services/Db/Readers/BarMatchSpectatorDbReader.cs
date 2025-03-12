using gex.Models.Db;
using Npgsql;
using System.Data;

namespace gex.Services.Db.Readers {

    public class BarMatchSpectatorDbReader : IDataReader<BarMatchSpectator> {

        public override BarMatchSpectator? ReadEntry(NpgsqlDataReader reader) {
            BarMatchSpectator spec = new();

            spec.GameID = reader.GetString("game_id");
            spec.UserID = reader.GetInt64("user_id");
            spec.Name = reader.GetString("user_name");

            return spec;
        }

    }
}
