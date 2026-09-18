using gex.Common.Models.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Config {

    public class UserOptions {

        public string InstallFolder { get; set; } = "";

        public bool CheckForUpdates { get; set; } = true;

        public bool AutoUpdate { get; set; } = false;

        public CovenRepositoryOptions Repository { get; set; } = new();

    }
}
