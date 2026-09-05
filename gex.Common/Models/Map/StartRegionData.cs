using gex.Common.Services.Util;
using System.Collections.Generic;

namespace gex.Common.Models.Map {

    public class StartRegionData {

        public int AllyTeamID { get; set; }

        public List<StartRegion> Regions { get; set; } = [];

    }

    public class StartRegion {

        public List<PolygonStartboxUtil.Pair> Vertices { get; set; } = [];

    }
}
