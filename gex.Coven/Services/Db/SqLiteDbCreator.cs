using Dapper;
using Dapper.ColumnMapper;
using gex.Common;
using gex.Common.Code;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Services.Db;
using gex.Coven.Code;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db {

    public class SqLiteDbCreator : IDbCreator {

        private readonly ILogger<SqLiteDbCreator> _Logger;
        private readonly IDbHelper _DbHelper;

        public SqLiteDbCreator(ILogger<SqLiteDbCreator> logger,
            IDbHelper dbHelper) {

            _Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _DbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        public async Task Execute() {
            using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
            if (conn is SqliteConnection sqConn) {
                _Logger.LogInformation($"starting sqlite db creator [version={sqConn.ServerVersion}]");
            }

            using DbCommand cmd = await _DbHelper.Command(conn, @"
                PRAGMA journal_mode=WAL;
            ");
            await cmd.ExecuteNonQueryAsync();
            await conn.CloseAsync();

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

            List<Type> types = Assembly.GetAssembly(typeof(GexCommon))!.GetTypes()
                .Where(iter => iter.GetCustomAttribute<DapperColumnsMappedAttribute>() != null).ToList();

            types.AddRange(Assembly.GetExecutingAssembly().GetTypes()
                .Where(iter => iter.GetCustomAttribute<DapperColumnsMappedAttribute>() != null));

            foreach (Type t in types) {
                SqlMapper.SetTypeMap(t, new ColumnTypeMapper(t));
            }

            SqlMapper.AddTypeHandler(new DapperSqlMappers.UIntHandler());
            SqlMapper.AddTypeHandler(new DapperSqlMappers.ULongHandler());
            SqlMapper.AddTypeHandler(new DapperSqlMappers.JsonbHandler());
            SqlMapper.AddTypeHandler(new DapperSqlMappers.MapSymmetryAxisHandler());
            SqlMapper.AddTypeHandler(new DapperSqlMappers.HashSetStringHandler());

            SqlMapper.AddTypeHandler(new SqLiteDapperTypeMapper.JsonElementHandler());
        }

        /// <summary>
        ///     Get all the patches loaded in the currently assembly
        /// </summary>
        private List<IDbPatch> GetPatches() {
            List<IDbPatch> patches = [];

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
            using DbConnection conn = _DbHelper.Connection(SqLiteDb.WRITE);
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                INSERT INTO metadata (name, value)
                    VALUES ('app_id', @ID)
                ON CONFLICT (name) DO
                    UPDATE SET value = @ID;
            ");
            cmd.AddParameter("@ID", version);

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

            using DbConnection conn = _DbHelper.Connection();
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                SELECT value
                    FROM metadata
                    WHERE name = 'app_id';
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
            using DbConnection conn = _DbHelper.Connection();
            using DbCommand cmd =  await _DbHelper.Command(conn, @"
                SELECT EXISTS (
                    SELECT 1 FROM sqlite_master
                    WHERE type = 'table' AND name = 'metadata'
               );
            ");

            object? value = await cmd.ExecuteScalarAsync();
            if (value == null) {
                return false;
            }

            if (value is long l) {
                return l != 0;
            }

            throw new InvalidOperationException($"unchecked state of value [type={value.GetType().Name}]");
        }

    }
}
