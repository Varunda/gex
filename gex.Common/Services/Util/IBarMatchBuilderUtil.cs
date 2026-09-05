using gex.Common.Models;
using gex.Common.Models.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Services.Util {

    public interface IBarMatchBuilderUtil {

        /// <summary>
        ///     build a <see cref="BarMatch"/> from ID
        /// </summary>
        /// <param name="gameID">ID of the bar match</param>
        /// <param name="options">options about what to include in the built <see cref="BarMatch"/></param>
        /// <param name="currentUserID">ID of user who is trying to build this match. may not be used in all implementations</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public Task<Result<Maybe<BarMatch>, string>> BuildMatch(string gameID,
            BuildOptions options, long? currentUserID,
            CancellationToken cancel
        );

        /// <summary>
        ///     options used when calling <see cref="BuildMatch(string, BuildOptions, long?, CancellationToken)"/>
        /// </summary>
        public class BuildOptions {
            public bool IncludeTeams { get; set; } = false;
            public bool IncludeAllyTeams { get; set; } = false;
            public bool IncludePlayers { get; set; } = false;
            public bool IncludeChat { get; set; } = false;
            public bool IncludeSpectators { get; set; } = false;
            public bool IncludeTeamDeaths { get; set; } = false;
            public bool IncludePlayerLeaves { get; set; } = false;
            public bool IncludeMapDraws { get; set; } = false;
            public bool IncludeLabeledPings { get; set; } = false;
            public bool IncludeCommands { get; set; } = false;
            public bool IncludeSelfDCommands { get; set; } = false;
            public bool IncludeStartRegionData { get; set; } = false;
        }

    }

}
