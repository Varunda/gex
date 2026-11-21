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

        /// <summary>
        ///     get all <see cref="BarTweakEntry"/>s from the DB
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <returns>a list of <see cref="BarTweakEntry"/>s</returns>
        public async Task<List<BarTweakEntry>> GetAll(CancellationToken cancel) {
            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<BarTweakEntry>(@"SELECT * FROM bar_tweak_entry;", cancel);
        }

        /// <summary>
        ///     generate the <see cref="BarTweakEntry"/>s for a specific day
        /// </summary>
        /// <param name="day">day to use</param>
        /// <param name="cancel">cancellation token</param>
        public async Task Generate(DateTime day, CancellationToken cancel) {
            List<BarTweakEntry> tweaks = await GetTweakKind(day, "unit", cancel);
            tweaks.AddRange(await GetTweakKind(day, "def", cancel));

            foreach (BarTweakEntry entry in tweaks) {
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
                    hash, name, type, base64, day, count
                ) VALUES (
                    @Hash, @Name, @Type, @Base64, @Day, @Count
                ) ON CONFLICT (hash, day, type) DO UPDATE
                    SET count = @Count;
            ", cancel);

            cmd.AddParameter("Hash", entry.Hash);
            cmd.AddParameter("Name", entry.Name);
            cmd.AddParameter("Type", entry.Type);
            cmd.AddParameter("Base64", entry.Base64);
            cmd.AddParameter("Day", entry.Day);
            cmd.AddParameter("Count", entry.Count);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        private async Task<List<BarTweakEntry>> GetTweakKind(DateTime day, string type, CancellationToken cancel) {
            if (type != "unit" && type != "def") {
                throw new ArgumentException($"expected {nameof(type)} to be either 'unit' or 'def'");
            }

            using DbConnection conn = _DbHelper.Connection(Dbs.MAIN);
            List<BarTweakEntry> entries = await conn.QueryListAsync<BarTweakEntry>(@$"
                    WITH tweaks AS (
                        select array_agg(array[
                            game_settings->>'tweak{type}s',
                            game_settings->>'tweak{type}s1',
                            game_settings->>'tweak{type}s2',
                            game_settings->>'tweak{type}s3',
                            game_settings->>'tweak{type}s4',
                            game_settings->>'tweak{type}s5',
                            game_settings->>'tweak{type}s6',
                            game_settings->>'tweak{type}s7',
                            game_settings->>'tweak{type}s8',
                            game_settings->>'tweak{type}s9',
                            game_settings->>'tweak{type}s10',
                            game_settings->>'tweak{type}s11',
                            game_settings->>'tweak{type}s12',
                            game_settings->>'tweak{type}s13',
                            game_settings->>'tweak{type}s14',
                            game_settings->>'tweak{type}s15',
                            game_settings->>'tweak{type}s16',
                            game_settings->>'tweak{type}s17',
                            game_settings->>'tweak{type}s18',
                            game_settings->>'tweak{type}s19',
                            game_settings->>'tweak{type}s20',
                            game_settings->>'tweak{type}s21',
                            game_settings->>'tweak{type}s22',
                            game_settings->>'tweak{type}s23',
                            game_settings->>'tweak{type}s24',
                            game_settings->>'tweak{type}s25',
                            game_settings->>'tweak{type}s26',
                            game_settings->>'tweak{type}s27',
                            game_settings->>'tweak{type}s28',
                            game_settings->>'tweak{type}s29'
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
                    WHERE tweak <> '' AND tweak <> '0' AND tweak <> 'Ow'
                    GROUP BY 1, 2;
                ",
                new {
                    Day = day.Date
                },
                cancel
            );

            foreach (BarTweakEntry entry in entries) {
                entry.Day = day.Date;
                entry.Type = type;

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
            }

            return entries;
        }

    }
}
