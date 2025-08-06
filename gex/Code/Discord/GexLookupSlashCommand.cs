using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using gex.Code.Constants;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
using gex.Models.UserStats;
using gex.Services;
using gex.Services.Db.Match;
using gex.Services.Db.Patches;
using gex.Services.Db.Readers;
using gex.Services.Db.UserStats;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Npgsql.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Discord {

    [SlashCommandGroup("gex", "Gex commands")]
    public class GexLookupSlashCommand : ApplicationCommandModule {

        public ILogger<GexLookupSlashCommand> _Logger { set; private get; } = default!;
        public InstanceInfo _Instance { set; private get; } = default!;

        public BarUserDb _UserDb { set; private get; } = default!;
        public BarUserSkillDb _SkillDb { set; private get; } = default!;
        public BarUserMapStatsDb _MapStatsDb { set; private get; } = default!;
        public BarUserFactionStatsDb _FactionStatsDb { set; private get; } = default!;
        public BarMatchRepository _MatchRepository { set; private get; } = default!;
        public BarMatchPlayerRepository _PlayerRepository { set; private get; } = default!;
        public BarMatchAllyTeamDb _AllyTeamDb { set; private get; } = default!;

        [SlashCommand("player", "Player lookup")]
        public async Task PlayerLookupCommand(InteractionContext ctx,
            [Option("name", "Player name")] string name) {

            await ctx.CreateDeferred(false);

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            List<UserSearchResult> users = (await _UserDb.SearchByName(name, false, cancel))
                .OrderByDescending(iter => iter.LastUpdated).ToList();

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            embed.Title = $"Player lookup: `{name}`";

            if (users.Count == 0) {
                embed.Description = $"Failed to find any users with this name";
                embed.Color = DiscordColor.Red;
            } else if (users.Count == 1) {
                UserSearchResult user = users[0];

                embed = await _GetUserInfo(user.UserID, cancel);
                embed.Title = $"Player lookup: `{user.Username}`";
                embed.Url = $"https://{_Instance.GetHost()}/user/{user.UserID}";
                embed.Color = DiscordColor.Green;

                builder.AddComponents(
                    new DiscordLinkButtonComponent($"https://{_Instance.GetHost()}/user/{user.UserID}", "View on Gex")
                );
            } else if (users.Count > 1) {
                embed.Color = DiscordColor.Yellow;
                embed.Description = $"Found {users.Count} users that match `{name}`, please provide more characters\n-# Sorted based on last updated\n\n";

                for (int i = 0; i < users.Count; ++i) {
                    UserSearchResult user = users[i];
                    string line = $"{user.Username}\n";

                    if (i > 20 || embed.Description.Length + line.Length >= 1500) {
                        embed.Description += $"and {users.Count - i + 1} more...";
                        break;
                    }

                    embed.Description += line;
                }
            }

            builder.AddEmbed(embed);

            await ctx.EditResponseAsync(builder);
        }

        private async Task<DiscordEmbedBuilder> _GetUserInfo(long userID, CancellationToken cancel) {
            DiscordEmbedBuilder embed = new();
            embed.Description = "";

            List<BarUserSkill> skill = await _SkillDb.GetByUserID(userID, cancel);
            if (skill.Count > 0) {
                BarUserSkill highestSkill = skill.MaxBy(iter => iter.Skill) ?? throw new Exception($"why is there no highestSkill");
                embed.Description += $"**Highest OS**\n";
                embed.Description += $"{BarGamemode.GetName(highestSkill.Gamemode)} - `{highestSkill.Skill} ± {highestSkill.SkillUncertainty}`\n\n";
            } else {
                embed.AddField("Highest OS", "no skill stats found!");
            }

            List<BarUserFactionStats> faction = await _FactionStatsDb.GetByUserID(userID, cancel);
            if (faction.Count > 0) {
                List<IGrouping<byte, BarUserFactionStats>> grouped = faction.GroupBy(iter => iter.Faction).ToList();
                IGrouping<byte, BarUserFactionStats> mostPlayed = grouped.MaxBy(iter => iter.Sum(i2 => i2.PlayCount)) ?? throw new Exception("a");

                int playCount = mostPlayed.Sum(iter => iter.PlayCount);
                int winCount = mostPlayed.Sum(iter => iter.WinCount);

                embed.Description += "**Faction**\n";
                embed.Description += $"{BarFaction.GetName(mostPlayed.Key)} - {Math.Truncate((decimal)winCount / playCount * 100m)}% won of {playCount} played\n\n";
            } else {
                embed.AddField("Faction", "no faction stats found!");
            }

            List<BarUserMapStats> maps = await _MapStatsDb.GetByUserID(userID, cancel);
            if (maps.Count > 0) {
                List<IGrouping<string, BarUserMapStats>> grouped = maps.GroupBy(iter => iter.Map)
                    .OrderByDescending(iter => iter.Sum(i2 => i2.PlayCount))
                    .Take(3).ToList();

                embed.Description += $"**Maps:**\n";
                foreach (IGrouping<string, BarUserMapStats> map in grouped) {
                    int playCount = map.Sum(iter => iter.PlayCount);
                    int winCount = map.Sum(iter => iter.WinCount);

                    embed.Description += $"**{map.Key}**\n";
                    embed.Description += $"{Math.Truncate((decimal)winCount / playCount * 100m)}% won of {playCount} played\n\n";
                }

            } else {
                embed.AddField("Maps", "no map stats found");
            }

            List<BarMatch> recentGames = (await _MatchRepository.GetByUserID(userID, cancel)).Take(4).ToList();
            if (recentGames.Count > 0) {
                embed.Description += $"**Recent public PvP games:**\n";

                foreach (BarMatch match in recentGames) {

                    string title;
                    if (match.PlayerCount == 2) {
                        List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(match.ID, cancel);

                        if (players.Count != 2) {
                            title = $"ERROR: expected 2 players, got {players.Count} instead";
                        } else {
                            title = $"Duel: {players[0].Name} v {players[1].Name}";
                        }
                    } else {
                        List<BarMatchAllyTeam> allyTeams = await _AllyTeamDb.GetByGameID(match.ID, cancel);
                        if (allyTeams.Count == 0) {
                            title = $"ERROR: got 0 ally teams";
                        } else {
                            int biggestTeam = allyTeams.Select(iter => iter.PlayerCount).Max();
                            // FFA
                            if (biggestTeam == 1) {
                                title = $"{allyTeams.Count}-way FFA";
                            } else {
                                title = $"{(biggestTeam >= 4 ? "Large team" : "Small team")}: " + string.Join(" v ", allyTeams.Select(iter => iter.PlayerCount));
                            }
                        }
                    }

                    title = $"{match.StartTime.GetDiscordTimestamp("D")} - {title} on {match.Map}";

                    embed.Description += $"[{title}](<https://{_Instance.GetHost()}/match/{match.ID}>)\n";
                }
            }

            return embed;
        }

    }
}
