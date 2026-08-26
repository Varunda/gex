using gex.Services.Util;
using System.Collections.Generic;

namespace gex.Models.Bar {

    public class StartRegionData {

        public int AllyTeamID { get; set; }

        public List<StartRegion> Regions { get; set; } = [];

    }

    public class StartRegion {

        public List<PolygonStartboxUtil.Pair> Vertices { get; set; } = [];

    }
}
