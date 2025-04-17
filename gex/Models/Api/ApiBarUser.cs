using gex.Models.UserStats;
using System;
using System.Collections.Generic;

namespace gex.Models.Api {

    public class ApiBarUser {

        public long UserID { get; set; }

        public string Username { get; set; } = "";

		public DateTime LastUpdated { get; set; }

        public List<BarUserSkill> Skill { get; set; } = [];

        public List<BarUserMapStats> MapStats { get; set; } = [];

        public List<BarUserFactionStats> FactionStats { get; set; } = [];

    }
}
