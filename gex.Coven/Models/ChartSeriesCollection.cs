using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models {

    public class ChartSeriesCollection {

        public List<string> Labels { get; set; } = [];

        public List<ChartSeries> Series { get; set; } = [];

    }
}
