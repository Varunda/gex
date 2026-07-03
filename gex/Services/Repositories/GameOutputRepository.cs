using gex.Common.Models;
using gex.Models;
using gex.Models.Db;
using gex.Models.Event;
using gex.Services.Db;
using gex.Services.Db.Event;
using gex.Services.Storage;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class GameOutputRepository {

        private readonly ILogger<GameOutputRepository> _Logger;
        private readonly ICurrentAccount _CurrentUser;

        private readonly BarMatchRepository _MatchRepository;
        private readonly GameEventTeamStatsDb _TeamStatsDb;
        private readonly GameEventUnitCreatedDb _UnitCreatedDb;
        private readonly GameEventUnitKilledDb _UnitKilledDb;
        private readonly UnitSetToGameIdDb _GameToHashDb;
        private readonly GameEventUnitDefDb _UnitDefDb;
        private readonly GameEventExtraStatsDb _ExtraStatsDb;
        private readonly GameEventWindUpdateDb _WindUpdateDb;
        private readonly GameEventCommanderPositionUpdateDb _CommanderPositionDb;
        private readonly GameEventFactoryUnitCreatedDb _FactoryUnitCreatedDb;
        private readonly GameEventUnitGivenDb _UnitGivenDb;
        private readonly GameEventUnitTakenDb _UnitTakenDb;
        private readonly GameEventTransportLoadedDb _TransportLoadedDb;
        private readonly GameEventTransportUnloadedDb _TransportUnloadedDb;
        private readonly GameEventTeamDiedDb _TeamDiedDb;
        private readonly GameEventUnitResourcesDb _UnitResourcesDb;
        private readonly GameEventUnitDamageDb _UnitDamageDb;
        private readonly GameEventUnitPositionDb _UnitPositionDb;
        private readonly UnitPositionFileStorage _UnitPositionStorage;
        private readonly MatchPoolRepository _MatchPoolRepository;
        private readonly MatchPoolEntryDb _MatchPoolEntryDb;

        public GameOutputRepository(ILogger<GameOutputRepository> logger,
            ICurrentAccount currentUser, BarMatchRepository matchRepository,
            GameEventTeamStatsDb teamStatsDb, GameEventUnitCreatedDb unitCreatedDb,
            GameEventUnitKilledDb unitKilledDb, UnitSetToGameIdDb gameToHashDb,
            GameEventUnitDefDb unitDefDb, GameEventExtraStatsDb extraStatsDb,
            GameEventWindUpdateDb windUpdateDb, GameEventCommanderPositionUpdateDb commanderPositionDb,
            GameEventFactoryUnitCreatedDb factoryUnitCreatedDb, GameEventUnitGivenDb unitGivenDb,
            GameEventUnitTakenDb unitTakenDb, GameEventTransportLoadedDb transportLoadedDb,
            GameEventTransportUnloadedDb transportUnloadedDb, GameEventTeamDiedDb teamDiedDb,
            GameEventUnitResourcesDb unitResourcesDb, GameEventUnitDamageDb unitDamageDb,
            GameEventUnitPositionDb unitPositionDb, UnitPositionFileStorage unitPositionStorage,
            MatchPoolRepository matchPoolRepository, MatchPoolEntryDb matchPoolEntryDb) {

            _Logger = logger;
            _CurrentUser = currentUser;
            _MatchRepository = matchRepository;
            _TeamStatsDb = teamStatsDb;
            _UnitCreatedDb = unitCreatedDb;
            _UnitKilledDb = unitKilledDb;
            _GameToHashDb = gameToHashDb;
            _UnitDefDb = unitDefDb;
            _ExtraStatsDb = extraStatsDb;
            _WindUpdateDb = windUpdateDb;
            _CommanderPositionDb = commanderPositionDb;
            _FactoryUnitCreatedDb = factoryUnitCreatedDb;
            _UnitGivenDb = unitGivenDb;
            _UnitTakenDb = unitTakenDb;
            _TransportLoadedDb = transportLoadedDb;
            _TransportUnloadedDb = transportUnloadedDb;
            _TeamDiedDb = teamDiedDb;
            _UnitResourcesDb = unitResourcesDb;
            _UnitDamageDb = unitDamageDb;
            _UnitPositionDb = unitPositionDb;
            _UnitPositionStorage = unitPositionStorage;
            _MatchPoolRepository = matchPoolRepository;
            _MatchPoolEntryDb = matchPoolEntryDb;
        }

        /// <summary>
        ///     build the <see cref="GameOutput"/> of a match
        /// </summary>
        /// <param name="gameID"></param>
        /// <param name="options"></param>
        /// <param name="currentUser"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Result<GameOutput?, string>> Build(string gameID, BuildOptions options,
            AppAccount? currentUser, CancellationToken cancel) {

            BarMatch? match = await _MatchRepository.GetByID(gameID, cancel);
            if (match == null) {
                return Result<GameOutput?, string>.Ok(null);
            }

            List<MatchPoolEntry> poolEntries = await _MatchPoolEntryDb.GetByMatchID(gameID, cancel);
            if (poolEntries.Count > 0) {
                bool canView = false;
                foreach (MatchPoolEntry entry in poolEntries) {
                    canView |= await _MatchPoolRepository.CanView(entry.PoolID, currentUser?.ID, cancel);
                    if (canView == true) {
                        break;
                    }
                }

                if (canView == false) {
                    return $"this match is hidden";
                }
            }

            GameOutput output = new();
            output.GameID = gameID;

            if (options.IncludeTeamStats == true) {
                output.TeamStats = await _TeamStatsDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeUnitsKilled == true) {
                output.UnitsKilled = await _UnitKilledDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeUnitsCreated == true) {
                output.UnitsCreated = await _UnitCreatedDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeExtraStats == true) {
                output.ExtraStats = await _ExtraStatsDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeWindUpdates == true) {
                output.WindUpdates = await _WindUpdateDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeCommanderPositionUpdates == true) {
                output.CommanderPositionUpdates = await _CommanderPositionDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeFactoryUnitCreate == true) {
                output.FactoryUnitCreated = await _FactoryUnitCreatedDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeUnitsGiven == true) {
                output.UnitsGiven = await _UnitGivenDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeUnitsTaken == true) {
                output.UnitsTaken = await _UnitTakenDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeTransportLoads == true) {
                output.TransportLoaded = await _TransportLoadedDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeTransportUnloads == true) {
                output.TransportUnloaded = await _TransportUnloadedDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeTeamDiedEvents == true) {
                output.TeamDiedEvents = await _TeamDiedDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeUnitResources == true) {
                output.UnitResources = await _UnitResourcesDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeUnitDamage == true) {
                output.UnitDamage = await _UnitDamageDb.GetByGameID(gameID, cancel);
            }

            if (options.IncludeUnitPosition == true) {
                Result<List<GameEventUnitPosition>, string> unitPos = await _UnitPositionStorage.GetByGameID(gameID, cancel);
                if (unitPos.IsOk == true) {
                    output.UnitPosition = unitPos.Value;
                } else {
                    _Logger.LogWarning($"failed to get unit position from storage [gameID={gameID}] [error={unitPos.Error}]");
                }
            }

            if (options.IncludeUnitDefs == true) {
                GameIdToUnitDefHash? hash = await _GameToHashDb.GetByGameID(gameID);
                if (hash != null) {
                    output.UnitDefinitions = await _UnitDefDb.GetByHash(hash.Hash);
                } else {
                    _Logger.LogWarning($"game hash is not set! [gameID={gameID}]");
                }
            }

            return output;
        }

        public class BuildOptions {
            public bool IncludeUnitsKilled { get; set; } = false;
            public bool IncludeUnitsCreated { get; set; } = false;
            public bool IncludeTeamStats { get; set; } = false;
            public bool IncludeUnitDefs { get; set; } = false;
            public bool IncludeExtraStats { get; set; } = false;
            public bool IncludeWindUpdates { get; set; } = false;
            public bool IncludeCommanderPositionUpdates { get; set; } = false;
            public bool IncludeFactoryUnitCreate { get; set; } = false;
            public bool IncludeUnitsGiven { get; set; } = false;
            public bool IncludeUnitsTaken { get; set; } = false;
            public bool IncludeTransportLoads { get; set; } = false;
            public bool IncludeTransportUnloads { get; set; } = false;
            public bool IncludeTeamDiedEvents { get; set; } = false;
            public bool IncludeUnitResources { get; set; } = false;
            public bool IncludeUnitDamage { get; set; } = false;
            public bool IncludeUnitPosition { get; set; } = false;
        }

    }
}
