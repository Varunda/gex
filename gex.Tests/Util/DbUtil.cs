using gex.Models.Options;
using gex.Services.Db;
using gex.Services.Db.Implementations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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

            DbHelper dbHelper = new DbHelper(
                logger: new TestLogger<DbHelper>(log),
                config: new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>() {
                    { "ConnectionStrings:gex", container.GetConnectionString() },
                    { "ConnectionStrings:event", container.GetConnectionString() },
                }).Build()
            );

            DefaultDbCreator creator = new DefaultDbCreator(
                logger: new TestLogger<DefaultDbCreator>(log),
                dbHelper: dbHelper,
                instanceOptions: Options.Create<InstanceOptions>(new InstanceOptions() {
                    SplitDatabases = false
                })
            );

            await creator.Execute();

            return dbHelper;
        }

    }
}
