using gex.Code.ExtensionMethods;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Db.Map {

    public abstract class BaseStartSpotDb<T> {

        protected readonly ILogger _Logger;
        protected readonly IDbHelper _DbHelper;

        protected readonly string _TableName;

        public BaseStartSpotDb(ILoggerFactory loggerFactory, IDbHelper helper, string tableName) {
            _Logger = loggerFactory.CreateLogger($"gex.Services.Db.Map.{typeof(T).Name}");
            _DbHelper = helper;

            _TableName = tableName;
        }

        public async Task<List<T>> GetLatestByMapFilename(string mapFilename, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<T>(@$"
                SELECT 
                    * 
                FROM 
                    {_TableName} 
                WHERE 
                    map_filename = @MapFilename 
                    AND version = (SELECT MAX(version) FROM start_spot_data WHERE map_filename = @MapFilename)",
                new { MapFilename = mapFilename },
                cancel
            );
        }

        public async Task<List<T>> GetByVersionAndMapFilename(string mapFilename, int version, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            return await conn.QueryListAsync<T>(@$"
                SELECT 
                    * 
                FROM 
                    {_TableName} 
                WHERE 
                    map_filename = @MapFilename 
                    AND version = @Version
                ",
                new { MapFilename = mapFilename, Version = version },
                cancel
            );
        }

        public async Task Insert(T inst, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @"", cancel);

            InsertSetup(cmd, inst);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

        protected abstract void InsertSetup(NpgsqlCommand cmd, T inst);

        public async Task DeleteByMapFilename(string mapFilename, int version, CancellationToken cancel) {
            using NpgsqlConnection conn = _DbHelper.Connection(Dbs.MAIN);
            using NpgsqlCommand cmd = await _DbHelper.Command(conn, @$"
                DELETE FROM {_TableName}
                    WHERE map_filename = @MapFilename AND version = @Version;
            ", cancel);

            cmd.AddParameter("MapFilename", mapFilename);
            cmd.AddParameter("Version", version);
            await cmd.PrepareAsync(cancel);

            await cmd.ExecuteNonQueryAsync(cancel);
            await conn.CloseAsync();
        }

    }
}
