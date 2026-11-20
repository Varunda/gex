using gex.Common.Code.ExtensionMethods;
using gex.Common.Services.Db;
using gex.Models.Bar;
using Lua.CodeAnalysis.Syntax.Nodes;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Bar {

    public class BarTweakEntryDb {

        private readonly ILogger<BarTweakEntryDb> _Logger;
        private readonly IDbHelper _DbHelper;

        public BarTweakEntryDb(ILogger<BarTweakEntryDb> logger,
            IDbHelper dbHelper) {

            _Logger = logger;
            _DbHelper = dbHelper;
        }

        public async Task<List<BarTweakEntry>> GetAll(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarTweakEntry>(@"SELECT * FROM bar_tweak_entry;", cancel);
        }

        public async Task Generate(DateTime day, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            List<BarTweakEntry> entries = await conn.QueryListAsync<BarTweakEntry>(@"
                    WITH tweaks AS (
                        select array_agg(array[
                            game_settings->>'tweakunits',
                            game_settings->>'tweakunits1',
                            game_settings->>'tweakunits2',
                            game_settings->>'tweakunits3',
                            game_settings->>'tweakunits4',
                            game_settings->>'tweakunits5',
                            game_settings->>'tweakunits6',
                            game_settings->>'tweakunits7',
                            game_settings->>'tweakunits8',
                            game_settings->>'tweakunits9',
                            game_settings->>'tweakunits10',
                            game_settings->>'tweakunits11',
                            game_settings->>'tweakunits12',
                            game_settings->>'tweakunits13',
                            game_settings->>'tweakunits14',
                            game_settings->>'tweakunits15',
                            game_settings->>'tweakunits16',
                            game_settings->>'tweakunits17',
                            game_settings->>'tweakunits18',
                            game_settings->>'tweakunits19',
                            game_settings->>'tweakunits20',
                            game_settings->>'tweakunits21',
                            game_settings->>'tweakunits22',
                            game_settings->>'tweakunits23',
                            game_settings->>'tweakunits24',
                            game_settings->>'tweakunits25',
                            game_settings->>'tweakunits26',
                            game_settings->>'tweakunits27',
                            game_settings->>'tweakunits28',
                            game_settings->>'tweakunits29',
                            game_settings->>'tweakdefs',
                            game_settings->>'tweakdefs1',
                            game_settings->>'tweakdefs2',
                            game_settings->>'tweakdefs3',
                            game_settings->>'tweakdefs4',
                            game_settings->>'tweakdefs5',
                            game_settings->>'tweakdefs6',
                            game_settings->>'tweakdefs7',
                            game_settings->>'tweakdefs8',
                            game_settings->>'tweakdefs9',
                            game_settings->>'tweakdefs10',
                            game_settings->>'tweakdefs11',
                            game_settings->>'tweakdefs12',
                            game_settings->>'tweakdefs13',
                            game_settings->>'tweakdefs14',
                            game_settings->>'tweakdefs15',
                            game_settings->>'tweakdefs16',
                            game_settings->>'tweakdefs17',
                            game_settings->>'tweakdefs18',
                            game_settings->>'tweakdefs19',
                            game_settings->>'tweakdefs20',
                            game_settings->>'tweakdefs21',
                            game_settings->>'tweakdefs22',
                            game_settings->>'tweakdefs23',
                            game_settings->>'tweakdefs24',
                            game_settings->>'tweakdefs25',
                            game_settings->>'tweakdefs26',
                            game_settings->>'tweakdefs27',
                            game_settings->>'tweakdefs28',
                            game_settings->>'tweakdefs29'
                        ]) ""b64s""
                        from bar_match
                        where date_trunc('day', start_time) = @Day
                    ), unnested AS (
                        select unnest(b64s) ""tweak"" from tweaks
                    )
                    SELECT md5(tweak) ""hash"",
                        tweak ""base64"",
                        count(*)
                    FROM unnested 
                    WHERE tweak <> '' AND tweak <> '0'
                    GROUP BY 1, 2;
                ",
                new {
                    Day = day.Date
                },
                cancel
            );

            foreach (BarTweakEntry entry in entries) {
                entry.Day = day.Date;

                byte[] decoded;
                try {
                    decoded = Base64Url.DecodeFromChars(entry.Base64.AsSpan());
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to decode base64 tweak");
                    continue;
                }

                string str = Encoding.UTF8.GetString(decoded).Trim();

                if (str.StartsWith("--")) {
                    int end = str.IndexOf('\n');
                    if (end == -1 || end > 75) {
                        end = 75;
                    }

                    entry.Name = str[2..end];
                }

                try {
                    await Upsert(entry, cancel);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to update entry [hash={entry.Hash}] [day={entry.Day}] [name={entry.Name}]");
                }
            }
        }

        private async Task Upsert(BarTweakEntry entry, CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using DbCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO bar_tweak_entry (
                    hash, name, base64, day, count
                ) VALUES (
                    @Hash, @Name, @Base64, @Day, @Count
                ) ON CONFLICT (hash, day) DO UPDATE
                    SET count = @Count;
            ", cancel);

            cmd.AddParameter("Hash", entry.Hash);
            cmd.AddParameter("Name", entry.Name);
            cmd.AddParameter("Base64", entry.Base64);
            cmd.AddParameter("Day", entry.Day);
            cmd.AddParameter("Count", entry.Count);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
