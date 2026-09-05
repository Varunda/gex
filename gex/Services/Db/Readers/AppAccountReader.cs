using gex.Code.ExtensionMethods;
using gex.Common.Services.Db;
using gex.Models.Internal;
using Npgsql;
using System.Data.Common;
using gex.Common.Code.ExtensionMethods;
using System.Data;

namespace gex.Services.Db.Readers {

    public class AppAccountReader : IDataReader<AppAccount> {

        public override AppAccount? ReadEntry(DbDataReader reader) {
            AppAccount acc = new AppAccount();

            acc.ID = reader.GetInt64("id");
            acc.Name = reader.GetString("name");
            acc.Timestamp = reader.GetDateTime("timestamp");
            acc.DiscordID = reader.GetUInt64("discord_id");
            acc.DeletedOn = reader.GetNullableDateTime("deleted_on");
            acc.DeletedBy = reader.GetNullableInt64("deleted_by");

            return acc;
        }

    }
}
