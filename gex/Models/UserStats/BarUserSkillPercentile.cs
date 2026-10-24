using Dapper.ColumnMapper;
using gex.Common.Code;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gex.Models.UserStats {

    [DapperColumnsMapped]
    public class BarUserSkillPercentile {

        [ColumnMapping("gamemode")]
        public short Gamemode { get; set; }

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        [ColumnMapping("p0")]
        public double P0 { get; set; }

        [ColumnMapping("p5")]
        public double P5 { get; set; }

        [ColumnMapping("p10")]
        public double P10 { get; set; }

        [ColumnMapping("p15")]
        public double P15 { get; set; }

        [ColumnMapping("p20")]
        public double P20 { get; set; }

        [ColumnMapping("p25")]
        public double P25 { get; set; }

        [ColumnMapping("p30")]
        public double P30 { get; set; }

        [ColumnMapping("p35")]
        public double P35 { get; set; }

        [ColumnMapping("p40")]
        public double P40 { get; set; }

        [ColumnMapping("p45")]
        public double P45 { get; set; }

        [ColumnMapping("p50")]
        public double P50 { get; set; }

        [ColumnMapping("p55")]
        public double P55 { get; set; }

        [ColumnMapping("p60")]
        public double P60 { get; set; }

        [ColumnMapping("p65")]
        public double P65 { get; set; }

        [ColumnMapping("p70")]
        public double P70 { get; set; }

        [ColumnMapping("p75")]
        public double P75 { get; set; }

        [ColumnMapping("p80")]
        public double P80 { get; set; }

        [ColumnMapping("p85")]
        public double P85 { get; set; }

        [ColumnMapping("p90")]
        public double P90 { get; set; }

        [ColumnMapping("p95")]
        public double P95 { get; set; }

        [ColumnMapping("p98")]
        public double P98 { get; set; }

        [ColumnMapping("p99")]
        public double P99 { get; set; }

        [ColumnMapping("p100")]
        public double P100 { get; set; }
        

        public double CalculatePercentile(double skill) {
            if (skill <= P0) {
                return 0;
            }

            if (skill >= P100) {
                return 100;
            }

            double min = P95;
            double max = P100;

            List<double> gaps = [
                5, 5, 5, 5, 5,
                5, 5, 5, 5, 5,
                5, 5, 5, 5, 5,
                5, 5, 5, 5, 3,
                1, 1, 0
            ];

            List<double> values = [
                P0, P5, P10, P15, P20, 
                P25, P30, P35, P40, P45,
                P50, P55, P60, P65, P70,
                P75, P80, P85, P90, P95,
                P98, P99, P100
            ];

            int i;
            for (i = 1; i < values.Count; ++i) {
                double iter = values[i];
                if (skill < iter) {
                    min = values[i - 1];
                    max = values[i];
                    break;
                }
            }

            double range = max - min;
            double offset = range - skill + min;
            double percent = 1d - (offset / range);
            double gap = gaps[i - 1];

            double gapSum = gaps.Take(i - 1).Sum();

            double percentile = gapSum + (gap * percent);
            return percentile;
        }

    }
}
