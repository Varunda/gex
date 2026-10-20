using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Bar {

    public class BarFeatureInstance {

        public string FeatureName { get; set; } = "";

        public double X { get; set; }

        public double Z { get; set; }

        public double Rotation { get; set; }

        public double Scale { get; set; } = 1d;

        public string? ResurrectAs { get; set; } = null;

        public double ReclaimLeft { get; set; } = 0d;

    }
}
