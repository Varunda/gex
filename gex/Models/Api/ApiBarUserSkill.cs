using gex.Common.Code.Constants;
using gex.Common.Models.User;
using System;

namespace gex.Models.Api {

    public class ApiBarUserSkill {

        public long UserID { get; set; }

        public byte Gamemode { get; set; } = BarGamemode.DEFAULT;

        public double Skill { get; set; }

        public double SkillUncertainty { get; set; }

        public double? Percentile { get; set; }

        public DateTime LastUpdated { get; set; }

        public ApiBarUserSkill() { }

        public ApiBarUserSkill(BarUserSkill skill) {
            UserID = skill.UserID;
            Gamemode = skill.Gamemode;
            Skill = skill.Skill;
            SkillUncertainty = skill.SkillUncertainty;
            LastUpdated = skill.LastUpdated;
        }

    }
}
