using gex.Common.Models.Match;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Services.Db.Match {

    public interface IBarMatchPlayerWidgetDb {

        /// <summary>
        ///     get the <see cref="BarMatchPlayerWidget"/>s used in a match
        /// </summary>
        /// <param name="gameID">ID of the match</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<List<BarMatchPlayerWidget>> GetByGameID(string gameID, CancellationToken cancel);

        /// <summary>
        ///     get the <see cref="BarMatchPlayerWidget"/>s used in many matches
        /// </summary>
        /// <param name="gameIDs"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<List<BarMatchPlayerWidget>> GetByGameIDs(IEnumerable<string> gameIDs, CancellationToken cancel);

        /// <summary>
        ///     insert a new <see cref="BarMatchPlayerWidget"/>
        /// </summary>
        /// <param name="widget"></param>
        /// <returns></returns>
        Task Insert(BarMatchPlayerWidget widget, CancellationToken cancel);

        /// <summary>
        ///     delete all <see cref="BarMatchPlayerWidget"/>s by a game
        /// </summary>
        /// <param name="gameID"></param>
        /// <returns></returns>
        Task DeleteByGameID(string gameID, CancellationToken cancel);

    }

}
