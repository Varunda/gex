using gex.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace gex.Services.Db.Implementations {

    public class DefaultDbCreator : IDbCreator {

        private readonly ILogger<DefaultDbCreator> _Logger;
        private readonly IDbHelper _DbHelper;
        private readonly IOptions<InstanceOptions> _InstanceOptions;

        private readonly bool _RunDb = true;

        public DefaultDbCreator(ILogger<DefaultDbCreator> logger,
            IDbHelper dbHelper, IOptions<InstanceOptions> instanceOptions) {

            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _DbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
            _InstanceOptions = instanceOptions;
        }

        public async Task Execute() {
            if (_RunDb == false) {
                return;
            }

            // Ensure the extension is loaded, as some of the patches may use the things the extension provides
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            if (_InstanceOptions.Value.SplitDatabases == true) {
                _Logger.LogInformation($"split databases are enabled, loading postgres_fdw in events db");
                using NpgsqlConnection evConn = _DbHelper.Connection(Dbs.EVENT);
                using NpgsqlCommand evCmd = await _DbHelper.Command(evConn, @"CREATE EXTENSION IF NOT EXISTS postgres_fdw;");
            }

            _Logger.LogTrace($"Getting current DB version");
            int version = await GetVersion();
            _Logger.LogInformation($"got current DB version [version={version}]");

            List<IDbPatch> patches = GetPatches();
            foreach (IDbPatch patch in patches) {
                _Logger.LogTrace($"checking patch [name='{patch.Name}'] [min version={patch.MinVersion}]");

                if (version < patch.MinVersion) {
                    _Logger.LogDebug($"apply patch [name='{patch.Name}'] [min version={patch.MinVersion}] [current version={version}]");
                    await patch.Execute(_DbHelper);

                    await UpdateVersion(patch.MinVersion);
                }
            }
        }

        /// <summary>
        ///     Get all the patches loaded in the currently assembly
        /// </summary>
        private List<IDbPatch> GetPatches() {
            List<IDbPatch> patches = new List<IDbPatch>();

            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (Type type in types) {
                if (typeof(IDbPatch).IsAssignableFrom(type)
                    && type.GetCustomAttribute<PatchAttribute>() != null) {

                    object? patch = Activator.CreateInstance(type);
                    if (patch != null) {
                        patches.Add((IDbPatch)patch);
                    } else {
                        _Logger.LogWarning($"Failed to create type {type.Name}");
                    }
                }
            }

            return patches.OrderBy(iter => iter.MinVersion).ToList();
        }

        /// <summary>
        ///     Update the DB version
        /// </summary>
        private async Task UpdateVersion(int version) {
            _Logger.LogTrace($"Updating version [version={version}]");
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                INSERT INTO metadata (name, value)
                    VALUES ('app_id', @ID)
                ON CONFLICT (name) DO
                    UPDATE SET value = @ID;
            ");
            cmd.Parameters.AddWithValue("@ID", version);

            await cmd.ExecuteNonQueryAsync();

            _Logger.LogTrace($"updated version [version={version}]");
        }

        /// <summary>
        ///     Get the current DB version, or -1 if no tables have been created, or an error occurs
        /// </summary>
        private async Task<int> GetVersion() {
            if (await DoesMetadataTableExist() == false) {
                _Logger.LogInformation($"no metadata table");
                return -1;
            }

            _Logger.LogTrace($"DB version metadata key: 'app_id'");

            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT value
                    FROM metadata
                    WHERE name = 'app_id'
            ");

            object? value = await cmd.ExecuteScalarAsync();
            if (value == null) {
                return -1;
            }

            if (int.TryParse(value.ToString(), out int version) == true) {
                return version;
            }

            _Logger.LogWarning($"Failed to part {value} to a valid Int32");

            return -1;
        }

        /// <summary>
        ///     Check if the metadata table exists
        /// </summary>
        private async Task<bool> DoesMetadataTableExist() {
            using NpgsqlConnection conn = _DbHelper.Connection();
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"
                SELECT EXISTS (
                    SELECT FROM pg_tables
                    WHERE tablename  = 'metadata'
               );
            ");

            object? value = await cmd.ExecuteScalarAsync();
            if (value == null) {
                return false;
            }

            return (bool)value;
        }

    }
}
