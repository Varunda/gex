using gex.Models.Db;
using gex.Models.Event;
using gex.Models.UserStats;
using System;
using System.Collections.Generic;

namespace gex.Models.Api {

    public class ApiBarUser {

        public long UserID { get; set; }

        public string Username { get; set; } = "";

        public DateTime LastUpdated { get; set; }

        public string? CountryCode { get; set; }

        public List<BarUserSkill> Skill { get; set; } = [];

        public List<BarUserMapStats> MapStats { get; set; } = [];

        public List<BarUserFactionStats> FactionStats { get; set; } = [];

        public List<UserPreviousName> PreviousNames { get; set; } = [];

        public List<GameUnitsCreated> UnitsMade { get; set; } = [];

    }
}
