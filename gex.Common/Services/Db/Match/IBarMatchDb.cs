using gex.Common.Models;
using gex.Common.Models.Match;
using gex.Common.Models.Search;

namespace gex.Common.Services.Db.Match {
    public interface IBarMatchDb {

        Task Delete(string gameID);

        Task<List<BarMatch>> GetAll(CancellationToken cancel);

        Task<List<BarMatch>> GetAllByMap(string mapFilename, CancellationToken cancel);

        Task<BarMatch?> GetByID(string ID, CancellationToken cancel);

        Task<List<BarMatch>> GetByIDs(IEnumerable<string> IDs, CancellationToken cancel);

        Task<List<BarMatch>> GetByTimePeriod(DateTime start, DateTime end, CancellationToken cancel);

        Task<List<BarMatch>> GetByUserID(long userID, CancellationToken cancel);

        Task<BarMatch?> GetOldestMatch(CancellationToken cancel);

        Task<List<string>> GetUniqueEngines(CancellationToken cancel);

        Task<List<string>> GetUniqueGameVersions(CancellationToken cancel);

        Task Insert(BarMatch match, CancellationToken cancel);

        Task<List<BarMatch>> Search(BarMatchSearchParameters parms, int offset, int limit, long? currentUserID, CancellationToken cancel);

        /// <summary>
        ///     perform a search with a search AST
        /// </summary>
        /// <param name="ast"><see cref="SearchAst"/> used to perform the search</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        Task<Result<List<BarMatch>, string>> Search(SearchAst ast, CancellationToken cancel);

        Task UpdateStartOffset(BarMatch match, CancellationToken cancel);

        Task UpdateStartSpotDataVersion(BarMatch match, CancellationToken cancel);

        Task UpdateWrongSkillValues(BarMatch match, CancellationToken cancel);

    }
}