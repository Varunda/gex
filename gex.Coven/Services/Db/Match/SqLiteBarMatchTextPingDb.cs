using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match {

    public class SqLiteBarMatchTextPingDb : IBarMatchTextPingDb {

        public Task<List<BarMatchMapDrawPoint>> GetByGameID(string gameID, CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public Task Insert(BarMatchMapDrawPoint point, CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public Task DeleteByGameID(string gameID) {
            throw new NotImplementedException();
        }
    }
}
