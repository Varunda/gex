using gex.Models.Db;
using Npgsql;
using System.Data;

namespace gex.Services.Db.Readers {

    public class BarMatchChatMessageDbReader : IDataReader<BarMatchChatMessage> {

        public override BarMatchChatMessage? ReadEntry(NpgsqlDataReader reader) {
            BarMatchChatMessage msg = new();

            msg.GameID = reader.GetString("game_id");
            msg.GameTimestamp = reader.GetFloat("game_timestamp");
            msg.ToId = reader.GetByte("to_id");
            msg.FromId = reader.GetByte("from_id");
            msg.Message = reader.GetString("message");

            return msg;
        }

    }
}
