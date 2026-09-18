using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Options {

    public class CovenRepositoryOptions {

        public string Instance { get; set; } = "https://git.honu.pw/";

        public string RepositoryOwner { get; set; } = "daratine";

        public string RepositoryName { get; set; } = "gex";

    }
}
