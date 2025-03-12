﻿using System.Collections.Generic;

namespace gex.Models.Health {

    public class CensusRealtimeHealthOptions {

        /// <summary>
        ///     Will both the Death and Exp streams be required to have failed to count as a failed world?
        /// </summary>
        public bool RequireFailureOnBoth = false;

        /// <summary>
        ///     Tolerances for the realtime 'Death' event
        /// </summary>
        public List<CensusRealtimeHealthTolerance> Death { get; set; } = new List<CensusRealtimeHealthTolerance>();

        /// <summary>
        ///     Tolerances for the realtime 'GainExperience' event
        /// </summary>
        public List<CensusRealtimeHealthTolerance> Exp { get; set; } = new List<CensusRealtimeHealthTolerance>();

    }

    public class CensusRealtimeHealthTolerance {

        /// <summary>
        ///     ID of the world this tolerance applies to
        /// </summary>
        public short WorldID { get; set; }

        /// <summary>
        ///     How many seconds this health record can go un-updated before being considered unhealth. Null for ignore
        /// </summary>
        public int? Tolerance { get; set; } = null;

    }

}
