using Dapper;
using gex.Code.ExtensionMethods;
using gex.Models.Internal;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Account {

    public class AppGroupDb {

        private readonly ILogger<AppGroupDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public AppGroupDb(ILogger<AppGroupDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;

            _DbHelper = dbHelper;
        }

        public async Task<List<AppGroup>> GetAll(CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
			return await conn.QueryListAsync<AppGroup>(
				"SELECT * from app_group",
				cancellationToken: cancel
			);
        }

        public async Task<AppGroup?> GetByID(long groupID, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
			return await conn.QuerySingleAsync<AppGroup>(
				"SELECT * FROM app_group WHERE id = @ID",
				new { ID = groupID },
				cancellationToken: cancel
			);
        }

        public async Task<long> Insert(AppGroup group, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_group (
                    name, hex_color
                ) VALUES (
                    @Name, @HexColor
                )
                RETURNING id;
            ", cancel);

            cmd.AddParameter("Name", group.Name);
            cmd.AddParameter("HexColor", group.HexColor);

            await cmd.PrepareAsync(cancel);

            long id = await cmd.ExecuteInt64(cancel);

            await conn.CloseAsync();

            return id;
        }

        public async Task<long> Upsert(AppGroup group, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO app_group (
                    name, hex_color
                ) VALUES (
                    @Name, @HexColor
                ) ON CONFLICT (id) DO UPDATE
                    SET name = @Name
                RETURNING id;
            ", cancel);

            cmd.AddParameter("Name", group.Name);
            cmd.AddParameter("HexColor", group.HexColor);

            await cmd.PrepareAsync(cancel);
            long id = await cmd.ExecuteInt64(cancel);

            await conn.CloseAsync();

            return id;
        }

    }
}
