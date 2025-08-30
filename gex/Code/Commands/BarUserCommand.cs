using gex.Code.Constants;
using gex.Commands;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Queues;
using gex.Models.UserStats;
using gex.Services.Db.UserStats;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Commands {

    [Command]
    public class BarUserCommand {

        private readonly ILogger<BarUserCommand> _Logger;
        private readonly BarUserRepository _UserRepository;
        private readonly BarMapRepository _MapRepository;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BaseQueue<UserMapStatUpdateQueueEntry> _MapStatUpdateQueue;
        private readonly BaseQueue<UserFactionStatUpdateQueueEntry> _FactionStatUpdateQueue;

        public BarUserCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<BarUserCommand>>();
            _UserRepository = services.GetRequiredService<BarUserRepository>();
            _MapRepository = services.GetRequiredService<BarMapRepository>();
            _MatchRepository = services.GetRequiredService<BarMatchRepository>();
            _MapStatUpdateQueue = services.GetRequiredService<BaseQueue<UserMapStatUpdateQueueEntry>>();
            _FactionStatUpdateQueue = services.GetRequiredService<BaseQueue<UserFactionStatUpdateQueueEntry>>();
        }

        public Task Fix(long userID) {
            _Logger.LogInformation($"fixing user stats for {userID}");

            new Task(async () => {
                List<BarMap> maps = await _MapRepository.GetAll(CancellationToken.None);
                _InsertUser(userID, maps);
            }).Start();

            return Task.CompletedTask;
        }
        
        public async Task FixAll() {
            List<long> users = (await _UserRepository.GetAll(CancellationToken.None)).Select(iter => iter.UserID).ToList();
            _Logger.LogInformation($"fixing user stats for {users.Count} users");

            new Task(async () => {
                List<BarMap> maps = await _MapRepository.GetAll(CancellationToken.None);
                foreach (long userID in users) {
                    _InsertUser(userID, maps);
                }
            }).Start();
        }

        public async Task FixFaction() {
            List<long> users = (await _UserRepository.GetAll(CancellationToken.None)).Select(iter => iter.UserID).ToList();
            _Logger.LogInformation($"fixing faction user stats for {users.Count} users");

            new Task(() => {
                // how many levels of foreach loops are you on my dude
                foreach (long userID in users) {
                    foreach (byte gamemode in BarGamemode.List) {
                        foreach (byte faction in BarFaction.List) {
                            _FactionStatUpdateQueue.Queue(new UserFactionStatUpdateQueueEntry() {
                                UserID = userID,
                                Faction = faction,
                                Gamemode = gamemode,
                                MaybeNone = true
                            });
                        }
                    }
                }
            }).Start();
        }

        private void _InsertUser(long userID, List<BarMap> maps) {
            // these queues will ignore entries that result in 0 plays
            // (either 0 plays for map//gamemode, or 0 plays for faction//gamemode)
            foreach (BarMap map in maps) {
                foreach (byte faction in BarFaction.List) {
                    _MapStatUpdateQueue.Queue(new UserMapStatUpdateQueueEntry() {
                        UserID = userID,
                        Map = map.Name,
                        Gamemode = faction,
                        MaybeNone = true
                    });
                }
            }

            foreach (byte gamemode in BarGamemode.List) {
                foreach (byte faction in BarFaction.List) {
                    _FactionStatUpdateQueue.Queue(new UserFactionStatUpdateQueueEntry() {
                        UserID = userID,
                        Faction = faction,
                        Gamemode = gamemode,
                        MaybeNone = true
                    });
                }
            }
        }

    }
}
