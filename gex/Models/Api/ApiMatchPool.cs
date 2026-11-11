using gex.Models.Apm;
using gex.Models.Db;
using System;
using System.Collections.Generic;

namespace gex.Models.Api {

    public class ApiMatchPool {

        public ApiMatchPool() { }

        public ApiMatchPool(MatchPool pool) {
            ID = pool.ID;
            Name = pool.Name;
            Timestamp = pool.Timestamp;
            CreatedByID = pool.CreatedByID;
            Unlisted = pool.Unlisted;
            HideUntil = pool.HideUntil;
        }

        public long ID { get; set; }

        public string Name { get; set; } = "";

        public DateTime Timestamp { get; set; }

        public long CreatedByID { get; set; }

        public bool Unlisted { get; set; }

        public DateTime? HideUntil { get; set; }

        public List<ApmTournamentInfo> TournamentInfos { get; set; } = [];

    }
}
