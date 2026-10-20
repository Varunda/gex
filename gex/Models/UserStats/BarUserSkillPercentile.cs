using Dapper.ColumnMapper;
using gex.Common.Code;
using System;

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
        
    }
}
