using gex.Common.Models.Match;
using gex.Common.Services.Db.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Coven.Services.Db.Match {
    public class SqLiteBarMatchTeamDeathDb : IBarMatchTeamDeathDb {

        public Task<List<BarMatchTeamDeath>> GetByGameID(string gameID, CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public Task Insert(BarMatchTeamDeath death, CancellationToken cancel) {
            throw new NotImplementedException();
        }

        public Task DeleteByGameID(string gameID) {
            throw new NotImplementedException();
        }
    }
}
