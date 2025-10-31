using gex.Common.Models.Familiar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Familiar.Services {

    public class StatusHolder {

        private readonly ILogger<StatusHolder> _Logger;

        private static FamiliarStatus _Status = new();

        public StatusHolder(ILogger<StatusHolder> logger) {
            _Logger = logger;
        }

        public void Update(FamiliarStatus status) {
            lock (_Status) {
                _Status = status;
            }
        }

        public FamiliarStatus Get() {
            lock (_Status) {
                return _Status;
            }
        }

    }
}
