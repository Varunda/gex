using gex.Common.Services.Db;
using gex.Models.Options;
using gex.Services.Db.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Testcontainers.PostgreSql;

namespace gex.Tests.Util {

    public class DbUtil {

        public static async Task<IDbHelper> Create(bool log = false) {
            PostgreSqlContainer container = new PostgreSqlBuilder("postgres:15.1")
                .Build();
            await container.StartAsync();

            PgDbHelper dbHelper = new PgDbHelper(
                logger: new TestLogger<PgDbHelper>(log),
                config: new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>() {
                    { "ConnectionStrings:gex", container.GetConnectionString() + ";Include Error Detail=true" },
                    { "ConnectionStrings:event", container.GetConnectionString() + ";Include Error Detail=true" },
                }).Build()
            );

            PgDbCreator creator = new PgDbCreator(
                logger: new TestLogger<PgDbCreator>(log),
                dbHelper: dbHelper,
                instanceOptions: Options.Create<InstanceOptions>(new InstanceOptions() {
                    SplitDatabases = false
                })
            );

            await creator.Execute();

            return dbHelper;
        }

        public static async Task TruncateTables(IDbHelper helper) {

            using DbConnection conn = helper.Connection();
            using DbCommand cmd = await helper.Command(conn, @"
                TRUNCATE bar_match;
                TRUNCATE bar_match_player;
                TRUNCATE bar_match_ally_team;
                TRUNCATE bar_match_team;
                TRUNCATE bar_match_spectator;
                TRUNCATE bar_match_chat_message;
                TRUNCATE bar_map;
                TRUNCATE bar_match_team_death;
                TRUNCATE bar_match_player_left;
                TRUNCATE bar_match_map_draw_point;
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
