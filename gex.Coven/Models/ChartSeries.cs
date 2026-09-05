using Avalonia.Media;
using LiveChartsCore.Painting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models {

    public class ChartSeries {

        public string Name { get; set; } = "default change me";

        public List<decimal> Values { get; set; } = [];

        public Paint Color { get; set; } = Paint.Parse("#ff00ff")!;

        public Paint? GeometryFill { get; set; } = null;

        public Paint? GeometryStroke { get; set; } = null;

    }
}
