using Npgsql;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

	[Patch]
	public class Patch19AddBarUserUsername : IDbPatch {
		public int MinVersion => 19;
		public string Name => "add user search index to bar_user";

		public async Task Execute(IDbHelper helper) {
			using NpgsqlConnection conn = helper.Connection(Dbs.MAIN);
			using NpgsqlCommand cmd = await helper.Command(conn, @"
				CREATE INDEX IF NOT EXISTS idx_bar_user_username ON bar_user (lower(username));
			");

			await cmd.ExecuteNonQueryAsync();
			await conn.CloseAsync();
		}

	}
}
