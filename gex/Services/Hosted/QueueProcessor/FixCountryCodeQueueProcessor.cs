using gex.Common.Models;
using gex.Models.Db;
using gex.Models.Queues;
using gex.Models.UserStats;
using gex.Services.Db.UserStats;
using gex.Services.Parser;
using gex.Services.Queues;
using gex.Services.Repositories;
using gex.Services.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    /// <summary>
    ///     queue that adds country codes to users based on previous games
    /// </summary>
    public class FixCountryCodeQueueProcessor : BaseQueueProcessor<FixCountryCodeQueueEntry> {

        private readonly BarMatchRepository _MatchRepository;
        private readonly BarUserRepository _UserRepository;
        private readonly BarDemofileParser _DemofileParser;
        private readonly DemofileStorage _DemofileStorage;

        public FixCountryCodeQueueProcessor(ILoggerFactory factory,
            BaseQueue<FixCountryCodeQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            BarMatchRepository matchRepository, BarDemofileParser demofileParser,
            DemofileStorage demofileStorage, BarUserRepository userRepository)
        : base("fix_country_code_queue", factory, queue, serviceHealthMonitor) {

            _MatchRepository = matchRepository;
            _DemofileParser = demofileParser;
            _DemofileStorage = demofileStorage;
            _UserRepository = userRepository;
        }

        protected override async Task<bool> _ProcessQueueEntry(FixCountryCodeQueueEntry entry, CancellationToken cancel) {

            _Logger.LogDebug($"fixing country code for user [userID={entry.UserID}]");

            BarUser? user = await _UserRepository.GetByID(entry.UserID, cancel);
            if (user == null) {
                _Logger.LogWarning($"cannot fix country code for user, not in db [userID={entry.UserID}]");
                return false;
            }

            if (user.CountryCode != null) {
                _Logger.LogWarning($"user already has a country code, why are they here [userID={entry.UserID}]");
                return false;
            }

            List<BarMatch> matches = await _MatchRepository.GetByUserID(entry.UserID, cancel);

            foreach (BarMatch match in matches) {
                Result<byte[], string> demofile = await _DemofileStorage.GetDemofileByFilename(match.FileName, cancel);
                if (demofile.IsOk == false) {
                    _Logger.LogError($"failed to get demofile from storage [gameID={match.ID}] [filename={match.FileName}] [error={demofile.Error}]");
                    continue;
                }

                Result<BarMatch, string> parsed = await _DemofileParser.Parse(match.FileName, demofile.Value, new DemofileParserOptions(), cancel);
                if (parsed.IsOk == false) {
                    _Logger.LogError($"failed to parse demofile [gameID={match.ID}] [error={parsed.Error}]");
                    continue;
                }

                BarMatch m = parsed.Value;
                foreach (BarMatchPlayer player in m.Players) {
                    if (player.UserID != entry.UserID) {
                        continue;
                    }

                    user.CountryCode = player.CountryCode;
                    await _UserRepository.Upsert(user.UserID, user, cancel);
                    _Logger.LogDebug($"fixed country code of user [userID={entry.UserID}]");
                    return true;
                }
            }

            return true;
        }

    }
}
