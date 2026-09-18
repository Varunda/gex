using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models {

    public class CovenVersion {

        public string Tag { get; set; } = "";

        public DateTime PublishedAt { get; set; }

        public string WindowsDownload { get; set; } = "";

        public string LinuxDownload { get; set; } = "";

    }
}
