using CommunityToolkit.Mvvm.ComponentModel;
using gex.Coven.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Chart {

    public class ChartSeriesCollection {

        public List<string> Labels { get; set; } = [];

        public List<ChartSeries> Series { get; set; } = [];

    }
}
