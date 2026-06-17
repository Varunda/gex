using gex.Models.Bar;
using Npgsql;

namespace gex.Services.Db.Readers {

    public class BarMapDbReader : IDataReader<BarMap> {
        public override BarMap? ReadEntry(NpgsqlDataReader reader) {
            throw new System.NotImplementedException();
        }
    }
}
