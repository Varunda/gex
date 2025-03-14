using Npgsql;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    public class Patch03AddBarMap : IDbPatch {

        public int MinVersion => 3;
        public string Name => "add bar_map";

        public async Task Execute(IDbHelper helper) {
            using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_map (
                    id int NOT NULL PRIMARY KEY,
                    name varchar NOT NULL,
                    filename varchar NOT NULL,
                    description varchar NOT NULL,
                    tidal_strength double precision NOT NULL,
                    max_metal double precision NOT NULL,
                    extractor_radius double precision NOT NULL,
                    minimum_wind double precision NOT NULL,
                    maximum_wind double precision NOT NULL,
                    width double precision NOT NULL,
                    height double precision NOT NULL,
                    author varchar NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
