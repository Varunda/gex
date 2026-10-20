using gex.Common.Services.Db;
using Npgsql;
using System.Data.Common;
using System.Threading.Tasks;

namespace gex.Services.Db.Patches {

    [Patch]
    public class Patch72AddBarUserSkillPercentile : IDbPatch {
        public int MinVersion => 72;
        public string Name => "add skill_percentile";

        public async Task Execute(IDbHelper helper) {
            using DbConnection conn = helper.Connection(Dbs.MAIN);
            using DbCommand cmd = await helper.Command(conn, @"
                CREATE TABLE IF NOT EXISTS bar_user_skill_percentile (
                    gamemode smallint NOT NULL PRIMARY KEY,
                    timestamp timestamptz NOT NULL DEFAULT (NOW() at time zone 'utc'),

                    p00 double precision NOT NULL,
                    p05 double precision NOT NULL,
                    p10 double precision NOT NULL,
                    p15 double precision NOT NULL,
                    p20 double precision NOT NULL,
                    p25 double precision NOT NULL,
                    p30 double precision NOT NULL,
                    p35 double precision NOT NULL,
                    p40 double precision NOT NULL,
                    p45 double precision NOT NULL,
                    p50 double precision NOT NULL,
                    p55 double precision NOT NULL,
                    p60 double precision NOT NULL,
                    p65 double precision NOT NULL,
                    p70 double precision NOT NULL,
                    p75 double precision NOT NULL,
                    p80 double precision NOT NULL,
                    p85 double precision NOT NULL,
                    p90 double precision NOT NULL,
                    p95 double precision NOT NULL,
                    p98 double precision NOT NULL,
                    p99 double precision NOT NULL,
                    p100 double precision NOT NULL
                );
            ");

            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();
        }

    }
}
