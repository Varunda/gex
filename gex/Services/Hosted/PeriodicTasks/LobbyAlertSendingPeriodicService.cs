using DSharpPlus.Entities;
using gex.Code.Constants;
using gex.Models.Db;
using gex.Models.Discord;
using gex.Models.Lobby;
using gex.Models.UserStats;
using gex.Services.Db;
using gex.Services.Db.UserStats;
using gex.Services.Lobby;
using gex.Services.Queues;
using gex.Services.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.PeriodicTasks {

    public class LobbyAlertSendingPeriodicService : AppBackgroundPeriodicService {

        private const string SERVICE_NAME = "lobby_alert_sending";

        private readonly LobbyManager _LobbyManager;
        private readonly BarUserRepository _UserRepository;
        private readonly LobbyAlertDb _LobbyAlertDb;
        private readonly BaseQueue<AppDiscordMessage> _DiscordMessageQueue;
        private readonly BarUserSkillDb _UserSkillDb;

        private static Dictionary<long, DateTime> _LastAlertSent = [];

        public LobbyAlertSendingPeriodicService(ILoggerFactory loggerFactory,
            ServiceHealthMonitor healthMon, LobbyManager lobbyManager,
            BarUserRepository userRepository, LobbyAlertDb lobbyAlertDb,
            BaseQueue<AppDiscordMessage> discordMessageQueue, BarUserSkillDb userSkillDb)
        : base(SERVICE_NAME, TimeSpan.FromMinutes(1), loggerFactory, healthMon) {

            _LobbyManager = lobbyManager;
            _UserRepository = userRepository;
            _LobbyAlertDb = lobbyAlertDb;
            _DiscordMessageQueue = discordMessageQueue;
            _UserSkillDb = userSkillDb;
        }

        protected override async Task<string?> PerformTask(CancellationToken cancel) {

            List<LobbyAlert> alerts = await _LobbyAlertDb.GetAll(cancel);
            List<LobbyBattle> battles = _LobbyManager.GetBattles();

            _Logger.LogInformation($"running lobby alert task");

            if (alerts.Count == 0) {
                return "no alerts to run thru";
            }

            int alertsSent = 0;

            Dictionary<int, LobbySkillValues> skillValues = [];

            foreach (LobbyAlert alert in alerts) {

                DateTime? lastAlertSent = _LastAlertSent.GetValueOrDefault(alert.ID);
                TimeSpan delay = TimeSpan.FromSeconds(Math.Max(59, alert.TimeBetweenAlertsSeconds - 1)); // -1 to allow for some processing time

                if (lastAlertSent != null) {
                    TimeSpan diff = DateTime.UtcNow - lastAlertSent.Value;
                    if (diff < delay) {
                        _Logger.LogTrace($"not sending lobby alert due to minimum delay not met [ID={alert.ID}] [delay={delay}] [diff={diff}]");
                        continue;
                    }
                }

                List<LobbyBattle> match = [];
                foreach (LobbyBattle battle in battles) {
                    if ((battle.Passworded == true) // ignored private lobbies
                        || (battle.TeamSize == 0 && battle.TeamCount == 0) // ignore empty lobbies
                        || (battle.TeamCount == 1) // ignore pve lobbies
                        || (battle.Gamemode == 0)
                        || (battle.Users.Count - battle.SpectatorCount + 1 <= 0)
                    ){
                        continue;
                    }

                    LobbyUser? founder = _LobbyManager.GetUser(battle.FounderUsername);
                    if (founder == null || founder.InGame == true) {
                        continue;
                    }

                    if (alert.Map != null && battle.Map != alert.Map) {
                        continue;
                    }

                    if (alert.Gamemode != null && battle.Gamemode != alert.Gamemode.Value) {
                        continue;
                    }

                    if (alert.MinimumPlayerCount != null && battle.PlayerCount < alert.MinimumPlayerCount.Value) {
                        continue;
                    }
                    if (alert.MaximumPlayerCount != null && battle.PlayerCount > alert.MaximumPlayerCount.Value) {
                        continue;
                    }

                    double avgOs = 0d;
                    double minOs = 999d;
                    double maxOs = 0d;

                    if (alert.MinimumAverageOS != null || alert.MaximumAverageOS != null
                        || alert.MinimumOS != null || alert.MaximumOS != null) {

                        if (battle.BattleStatus == null) {
                            _Logger.LogWarning($"missing battle status for lobby alert [battleID={battle.BattleID}] [alertID={alert.ID}]");
                            continue;
                        }

                        foreach (LobbyBattleStatusClient client in battle.BattleStatus.Clients) {
                            List<BarUserSkill> userSkill = await _UserSkillDb.GetByUserID(client.UserID, cancel);

                            LobbyUser? lobbyUser = _LobbyManager.GetUser(client.UserID);

                            double skill = userSkill.FirstOrDefault(iter => iter.Gamemode == battle.Gamemode)?.Skill ?? 16.67;
                            avgOs += skill;

                            if (skill < minOs) {
                                minOs = skill;
                            }
                            if (skill > maxOs) {
                                maxOs = skill;
                            }
                        }

                        avgOs /= Math.Max(1, battle.Users.Count);

                        skillValues[battle.BattleID] = new LobbySkillValues() {
                            AverageSkill = avgOs,
                            MinimumSkill = minOs,
                            MaximumSkill = maxOs
                        };
                    }

                    if (alert.MinimumAverageOS != null && avgOs < alert.MinimumAverageOS.Value) {
                        continue;
                    }
                    if (alert.MaximumAverageOS != null && avgOs > alert.MaximumAverageOS.Value) {
                        continue;
                    }

                    if (alert.MinimumOS != null && minOs < alert.MinimumOS.Value) {
                        continue;
                    }
                    if (alert.MaximumOS != null && maxOs < alert.MaximumOS.Value) {
                        continue;
                    }

                    match.Add(battle);
                }

                if (match.Count > 0) {
                    AppDiscordMessage msg = new();
                    msg.ChannelID = alert.ChannelID;
                    msg.GuildID = alert.GuildID;
                    if (alert.RoleID != null) {
                        msg.Mentions.Add(new RoleMention(alert.RoleID.Value));
                        msg.Message = $"<@&{alert.RoleID.Value}>";
                    }

                    DiscordEmbedBuilder embed = new();
                    embed.Title = $"Lobby alert";
                    embed.Description = $"Lobby(s) available\n\n";
                    embed.Color = DiscordColor.Purple;

                    int index = 0;
                    foreach (LobbyBattle battle in match) {
                        if (battle.Gamemode == 0) {
                            _Logger.LogWarning($"failed to check for gamemode [battleID={battle.BattleID}] [teamCount={battle.TeamCount}] [teamSize={battle.TeamSize}]");
                        }

                        List<string> parts = [..battle.Title.Split("|")];
                        string title = parts.First();
                        string conds = string.Join(", ", parts[1..].Select(iter => iter.Trim()));

                        int playerCount = battle.Users.Count - battle.SpectatorCount + 1; // +1 for autohost user

                        string desc = $"**{title}**\n";

                        desc += $"{battle.Map}: {playerCount}/{battle.MaxPlayers} ({battle.SpectatorCount} specs)";
                        if (alert.MinimumAverageOS != null || alert.MaximumAverageOS != null
                            || alert.MinimumOS != null || alert.MaximumOS != null) {

                            LobbySkillValues? svs = skillValues.GetValueOrDefault(battle.BattleID);

                            if (svs != null) {
                                desc += $" [μ {svs.MinimumSkill:F2} - {svs.MaximumSkill:F2}]";
                            } else {
                                _Logger.LogWarning($"missing lobby skill values for battle [battleID={battle.BattleID}]");
                            }
                        }
                        desc += "\n";

                        if (conds != "") {
                            desc += $"`{conds}`\n\n";
                        }

                        if (embed.Description.Length > 500) {
                            embed.Description += $"-# plus {match.Count - index - 1} more...\n";
                            break;
                        }

                        embed.Description += desc;

                        //_Logger.LogDebug($"battle matched alert [alert={JsonSerializer.Serialize(alert)}] [battle={JsonSerializer.Serialize(battle)}]");
                        ++index;
                    }

                    embed.Description += $"Use `/gex remove-alert {alert.ID}` to remove this alert";
                    msg.Embeds.Add(embed);

                    _DiscordMessageQueue.Queue(msg);

                    ++alertsSent;
                    _LastAlertSent[alert.ID] = DateTime.UtcNow;
                }
            }

            return $"sent {alertsSent} alerts";
        }

        private class LobbySkillValues {

            public int BattleID { get; set; }

            public double AverageSkill { get; set; }

            public double MinimumSkill { get; set; }

            public double MaximumSkill { get; set; }

        }

    }
}
