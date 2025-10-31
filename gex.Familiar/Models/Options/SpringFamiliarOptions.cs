using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Familiar.Models.Options {

    public class SpringFamiliarOptions {

        public string Host { get; set; } = "";

        public int Port { get; set; }

        public string Username { get; set; } = "";

        public string Password { get; set; } = "";

        public string Coordinator { get; set; } = "";

    }
}
