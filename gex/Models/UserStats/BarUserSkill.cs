using Dapper.ColumnMapper;
using gex.Code;
using gex.Common.Code.Constants;
using System;

namespace gex.Models.UserStats {

    [DapperColumnsMapped]
    public class BarUserSkill {

        [ColumnMapping("user_id")]
        public long UserID { get; set; }

        [ColumnMapping("gamemode")]
        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        [ColumnMapping("skill")]
        public double Skill { get; set; }

        [ColumnMapping("skill_uncertainty")]
        public double SkillUncertainty { get; set; }

        [ColumnMapping("last_updated")]
        public DateTime LastUpdated { get; set; }

    }
}
