using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using LiveChartsCore.Painting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Chart {

    public partial class ChartSeries : ObservableObject {

        public string Name { get; set; } = "default change me";

        public List<decimal> Values { get; set; } = [];

        public Paint Color { get; set; } = Paint.Parse("#ff00ff")!;

        public Paint? GeometryFill { get; set; } = null;

        public Paint? GeometryStroke { get; set; } = null;

        [ObservableProperty]
        private bool _Visible = true;

    }
}
