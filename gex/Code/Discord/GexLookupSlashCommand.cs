using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using gex.Code.Constants;
using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Db;
using gex.Models.Discord;
using gex.Models.Options;
using gex.Models.UserStats;
using gex.Services;
using gex.Services.Db;
using gex.Services.Db.Match;
using gex.Services.Db.Patches;
using gex.Services.Db.Readers;
using gex.Services.Db.UserStats;
using gex.Services.Repositories;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Discord {

    [SlashCommandGroup("gex", "Gex commands")]
    public class GexLookupSlashCommand : ApplicationCommandModule {

        public ILogger<GexLookupSlashCommand> _Logger { set; private get; } = default!;
        public IOptions<DiscordOptions> _DiscordOptions { set; private get; } = default!;

        public InstanceInfo _Instance { set; private get; } = default!;
        public BarUserDb _UserDb { set; private get; } = default!;
        public BarUserSkillDb _SkillDb { set; private get; } = default!;
        public BarUserMapStatsDb _MapStatsDb { set; private get; } = default!;
        public BarUserFactionStatsDb _FactionStatsDb { set; private get; } = default!;
        public BarMatchRepository _MatchRepository { set; private get; } = default!;
        public BarMatchPlayerRepository _PlayerRepository { set; private get; } = default!;
        public BarMatchAllyTeamDb _AllyTeamDb { set; private get; } = default!;
        public DiscordBarUserLinkDb _LinkDb { set; private get; } = default!;
        public DiscordSubscriptionMatchProcessedDb _SubscriptionDb { set; private get; } = default!;

        /// <summary>
        ///     look up a user, and if able to find a unique one, print their stats
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [SlashCommand("player", "Player lookup")]
        public async Task PlayerLookupCommand(InteractionContext ctx,
            [Option("name", "Player name")] string name) {

            await ctx.CreateDeferred(false);

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            Stopwatch timer = Stopwatch.StartNew();

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

                embed = await _GetUserInfo(user, cancel);
                builder.AddComponents(
                    new DiscordLinkButtonComponent($"https://{_Instance.GetHost()}/user/{user.UserID}", "View on Gex")
                );
            } else if (users.Count > 1) {

                UserSearchResult? exactMatch = users.FirstOrDefault(iter => iter.Username.ToLower() == name.ToLower());
                if (exactMatch != null) {
                    embed = await _GetUserInfo(exactMatch, cancel);
                    builder.AddComponents(
                        new DiscordLinkButtonComponent($"https://{_Instance.GetHost()}/user/{exactMatch.UserID}", "View on Gex")
                    );
                } else {
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
            }

            embed.WithFooter($"generated in {timer.ElapsedMilliseconds}ms");

            builder.AddEmbed(embed);

            await ctx.EditResponseAsync(builder);
        }

        /// <summary>
        ///     slash command to generate an invite link for the bot
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [SlashCommand("invite", "Generate a link used to invite Gex to another server")]
        public async Task PlayerLookupCommand(InteractionContext ctx) { 
            await ctx.CreateDeferred(true);

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            embed.Title = "Gex Discord invite link";
            string invite = $"https://discord.com/oauth2/authorize?client_id={_DiscordOptions.Value.ClientId}";
            embed.Description = invite;
            embed.Color = DiscordColor.Purple;

            builder.AddEmbed(embed);

            await ctx.EditResponseAsync(builder);
        }

        /// <summary>
        ///     link a discord account to a BAR user
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [SlashCommand("link", "Link a Discord account to a BAR user")]
        public async Task LinkCommand(InteractionContext ctx,
            [Option("name", "Player name")] string name) {

            await ctx.CreateDeferred(true);

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            Stopwatch timer = Stopwatch.StartNew();

            List<UserSearchResult> users = (await _UserDb.SearchByName(name, false, cancel))
                .OrderByDescending(iter => iter.LastUpdated).ToList();

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            embed.Title = $"Discord Bar link: `{name}`";

            // if 1 user, use that one, else try to find the user with the exact match, otherwise didn't find user
            UserSearchResult? exactMatch = users.FirstOrDefault(iter => iter.Username.ToLower() == name.ToLower());
            UserSearchResult? user = (users.Count == 1) ? users[0] : exactMatch;

            if (user == null) {
                if (users.Count == 0) {
                    embed.Description = $"Failed to find any users with this name";
                    embed.Color = DiscordColor.Red;
                } else {
                    embed.Color = DiscordColor.Yellow;
                    embed.Description = $"Found {users.Count} users that match `{name}`, please provide more characters\n-# Sorted based on last updated\n\n";

                    for (int i = 0; i < users.Count; ++i) {
                        UserSearchResult u = users[i];
                        string line = $"{u.Username}\n";

                        if (i > 20 || embed.Description.Length + line.Length >= 1500) {
                            embed.Description += $"and {users.Count - i + 1} more...";
                            break;
                        }

                        embed.Description += line;
                    }
                }
            } else {
                DiscordBarUserLink link = new();
                link.DiscordID = ctx.User.Id;
                link.BarUserID = user.UserID;

                await _LinkDb.Upsert(link, cancel);

                embed.Title = $"Link made!";
                embed.Color = DiscordColor.Green;
                embed.Description = $"Linked `{user.Username}` to this Discord account.\n\nUse `/gex recent` to show latest match";
            }

            embed.WithFooter($"generated in {timer.ElapsedMilliseconds}ms");

            builder.AddEmbed(embed);

            await ctx.EditResponseAsync(builder);
        }

        /// <summary>
        ///     unlink the Discord account using the command from any linked BAR user
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [SlashCommand("unlink", "Unlink a Discord account from a BAR account")]
        public async Task UnlinkCommand(InteractionContext ctx) {
            await ctx.CreateDeferred(true);

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            Stopwatch timer = Stopwatch.StartNew();

            await _LinkDb.Unlink(ctx.User.Id, cancel);

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            embed.Title = $"Discord account unlinked";
            embed.Description = $"This Discord account has been unlinked from the BAR user";

            builder.AddEmbed(embed);

            await ctx.EditResponseAsync(builder);
        }

        /// <summary>
        ///     show the latest match of the linked user or a provided player
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [SlashCommand("latest", "Show the latest match for a linked BAR account")]
        public async Task LatestCommand(InteractionContext ctx,
            [Option("player", "Player to show the latest match of, leave empty to use the linked account")] string player = "") {

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            Stopwatch timer = Stopwatch.StartNew();

            long userID;
            if (player == "") {
                DiscordBarUserLink? link = await _LinkDb.GetByDiscordID(ctx.User.Id, cancel);
                if (link == null) {
                    await ctx.CreateDeferred(true);

                    embed.Title = "Error! No BAR user provided";
                    embed.Description = $"No BAR user is linked to this Discord account, and no player name was given."
                        + $"\n\nUse `/gex link` to link this Discord account";
                    embed.Color = DiscordColor.Red;

                    builder.AddEmbed(embed);
                    await ctx.EditResponseAsync(builder);
                    return;
                }

                userID = link.BarUserID;
            } else {
                List<UserSearchResult> users = (await _UserDb.SearchByName(player, false, cancel))
                    .OrderByDescending(iter => iter.LastUpdated).ToList();

                // if 1 user, use that one, else try to find the user with the exact match, otherwise didn't find user
                UserSearchResult? exactMatch = users.FirstOrDefault(iter => iter.Username.ToLower() == player.ToLower());
                UserSearchResult? user = (users.Count == 1) ? users[0] : exactMatch;

                if (user == null) {
                    await ctx.CreateDeferred(true);

                    if (users.Count == 0) {
                        embed.Title = "Error! Failed to find user";
                        embed.Description = $"Failed to find a user with the name `{player}`";
                        embed.Color = DiscordColor.Red;
                    } else {
                        embed.Title = "Error! Name is not unique enough";
                        embed.Description = $"Found {users.Count} users that match the name `{player}`\n"
                            + $"Please provide more characters to narrow it down";
                        embed.Color = DiscordColor.Red;
                    }

                    await ctx.EditResponseEmbed(embed);
                    return;
                }

                userID = user.UserID;
            }

            await ctx.CreateDeferred(false);

            List<BarMatch> matches = await _MatchRepository.GetByUserID(userID, cancel);
            if (matches.Count == 0) {
                embed.Title = "No matches found!";
                embed.Description = $"Gex only tracks public PvP games, and could not find any matches ";
                if (player != "") {
                    embed.Description += $"for `{player}`";
                } else {
                    embed.Description += $"linked to this Discord account";
                }
                embed.Color = DiscordColor.Yellow;
            } else {
                BarMatch match = matches.OrderByDescending(iter => iter.StartTime).First();
                embed = await _GetMatchInfo(match, userID, cancel);
                builder.AddComponents(
                    new DiscordLinkButtonComponent($"https://{_Instance.GetHost()}/match/{match.ID}", "View on Gex")
                );
            }

            embed.WithFooter($"generated in {timer.ElapsedMilliseconds}ms");

            builder.AddEmbed(embed);

            await ctx.EditResponseAsync(builder);
        }

        /// <summary>
        ///     print out match info based on ID
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="matchID"></param>
        /// <returns></returns>
        [SlashCommand("match", "Print match info based on ID")]
        public async Task LookupMatch(InteractionContext ctx,
            [Option("matchID", "ID of the match")] string matchID) {

            await ctx.CreateDeferred(false);

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            Stopwatch timer = Stopwatch.StartNew();
            BarMatch? match = await _MatchRepository.GetByID(matchID, cancel);

            if (match == null) {
                embed.Title = "No match found";
                embed.Description = $"Failed to find match `{matchID}`";
                embed.Color = DiscordColor.Red;
            } else {
                embed = await _GetMatchInfo(match, null, cancel);
                builder.AddComponents(
                    new DiscordLinkButtonComponent($"https://{_Instance.GetHost()}/match/{match.ID}", "View on Gex")
                );
            }

            embed.WithFooter($"generated in {timer.ElapsedMilliseconds}ms");
            builder.AddEmbed(embed);

            await ctx.EditResponseAsync(builder);
        }

        /// <summary>
        ///     subscribe a discord account to a match being processed
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        [SlashCommand("subscribe", "Get notifications of a match being processed on Gex for a player")]
        public async Task Subscribe(InteractionContext ctx,
            [Option("player", "Player to subscribe to match processed notifications for")] string player) {

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            Stopwatch timer = Stopwatch.StartNew();

            List<UserSearchResult> users = (await _UserDb.SearchByName(player, false, cancel))
                .OrderByDescending(iter => iter.LastUpdated).ToList();

            // if 1 user, use that one, else try to find the user with the exact match, otherwise didn't find user
            UserSearchResult? exactMatch = users.FirstOrDefault(iter => iter.Username.ToLower() == player.ToLower());
            UserSearchResult? user = (users.Count == 1) ? users[0] : exactMatch;

            await ctx.CreateDeferred(true);

            if (user == null) {
                if (users.Count == 0) {
                    embed.Title = "Error! Failed to find user";
                    embed.Description = $"Failed to find a user with the name `{player}`";
                    embed.Color = DiscordColor.Red;
                } else {
                    embed.Title = "Error! Name is not unique enough";
                    embed.Description = $"Found {users.Count} users that match the name `{player}`\n"
                        + $"Please provide more characters to narrow it down";
                    embed.Color = DiscordColor.Red;
                }

                await ctx.EditResponseEmbed(embed);
                return;
            }

            List<DiscordSubscriptionMatchProcessed> subs = await _SubscriptionDb.GetByDiscordID(ctx.User.Id, cancel);
            DiscordSubscriptionMatchProcessed? sub = subs.FirstOrDefault(iter => iter.UserID == user.UserID);
            if (sub != null) {
                embed.Title = $"Error! Subscription already exists";
                embed.Description = $"A subscription for this Discord account already exists for {user.Username}";
                embed.Color = DiscordColor.Red;

                await ctx.EditResponseEmbed(embed);
                return;
            }

            sub = new DiscordSubscriptionMatchProcessed();
            sub.DiscordID = ctx.User.Id;
            sub.UserID = user.UserID;
            sub.Timestamp = DateTime.UtcNow;

            await _SubscriptionDb.Insert(sub, cancel);

            embed.Title = "Success!";
            embed.Description = $"Successfully subscribed to `{user.Username}`\nWhenever Gex fully replays a game with this player in it, a message will be sent";
            embed.Color = DiscordColor.Green;
            builder.AddEmbed(embed);
            await ctx.EditResponseAsync(builder);
        }

        [SlashCommand("unsubscribe", "Remove a subscription to a player")]
        public async Task Unsubscribe(InteractionContext ctx,
            [Option("player", "Player to unsubscribe to match processed notifications to")] string player) {
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            Stopwatch timer = Stopwatch.StartNew();

            List<UserSearchResult> users = (await _UserDb.SearchByName(player, false, cancel))
                .OrderByDescending(iter => iter.LastUpdated).ToList();

            // if 1 user, use that one, else try to find the user with the exact match, otherwise didn't find user
            UserSearchResult? exactMatch = users.FirstOrDefault(iter => iter.Username.ToLower() == player.ToLower());
            UserSearchResult? user = (users.Count == 1) ? users[0] : exactMatch;

            await ctx.CreateDeferred(true);

            if (user == null) {
                if (users.Count == 0) {
                    embed.Title = "Error! Failed to find user";
                    embed.Description = $"Failed to find a user with the name `{player}`";
                    embed.Color = DiscordColor.Red;
                } else {
                    embed.Title = "Error! Name is not unique enough";
                    embed.Description = $"Found {users.Count} users that match the name `{player}`\n"
                        + $"Please provide more characters to narrow it down";
                    embed.Color = DiscordColor.Red;
                }

                await ctx.EditResponseEmbed(embed);
                return;
            }

            List<DiscordSubscriptionMatchProcessed> subs = await _SubscriptionDb.GetByDiscordID(ctx.User.Id, cancel);
            DiscordSubscriptionMatchProcessed? sub = subs.FirstOrDefault(iter => iter.UserID == user.UserID);
            if (sub == null) {
                embed.Title = $"Subscription does not exist";
                embed.Description = $"Failed to find a subscription from this Discord account to `{user.Username}`";
                embed.Color = DiscordColor.Red;

                await ctx.EditResponseEmbed(embed);
                return;
            }

            sub = new DiscordSubscriptionMatchProcessed();
            sub.DiscordID = ctx.User.Id;
            sub.UserID = user.UserID;
            sub.Timestamp = DateTime.UtcNow;

            await _SubscriptionDb.Insert(sub, cancel);

            embed.Title = "Success!";
            embed.Description = $"Removed the subscription to `{user.Username}`";
            embed.Color = DiscordColor.Green;
            builder.AddEmbed(embed);
            await ctx.EditResponseAsync(builder);
        }

        /// <summary>
        ///     used to generate play stats for player lookups
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<DiscordEmbedBuilder> _GetUserInfo(UserSearchResult user, CancellationToken cancel) {
            DiscordEmbedBuilder embed = new();
            embed.Description = "";
            embed.Title = $"Player lookup: `{user.Username}`";
            embed.Url = $"https://{_Instance.GetHost()}/user/{user.UserID}";
            embed.Color = DiscordColor.Green;

            List<BarUserFactionStats> factionStats = await _FactionStatsDb.GetByUserID(user.UserID, cancel);
            List<BarUserSkill> skill = await _SkillDb.GetByUserID(user.UserID, cancel);
            List<BarUserMapStats> maps = await _MapStatsDb.GetByUserID(user.UserID, cancel);
            List<BarMatch> allGames = await _MatchRepository.GetByUserID(user.UserID, cancel);

            List<BarMatchPlayer> playerMatches = await _PlayerRepository.GetByUserID(user.UserID, cancel);
            Dictionary<string, BarMatchPlayer> playerDict = playerMatches.ToDictionary(iter => iter.GameID);

            // show skill, and combine with plays (from faction stats)
            if (skill.Count > 0 && factionStats.Count > 0) {

                embed.Description += $"**Games found**: {factionStats.Sum(iter => iter.PlayCount)}\n";
                embed.Description += $"Last seen: {allGames.MaxBy(iter => iter.StartTime)!.StartTime.GetDiscordTimestamp("D")}\n";

                // group the faction stats to gamemode, to show games played per gamemode
                List<IGrouping<byte, BarUserFactionStats>> factions = factionStats.GroupBy(iter => iter.Gamemode).ToList();
                foreach (IGrouping<byte, BarUserFactionStats> faction in factions.OrderByDescending(iter => iter.Sum(i2 => i2.PlayCount))) {
                    if (faction.Key == 0) {
                        continue;
                    }

                    BarUserSkill? s = skill.FirstOrDefault(iter => iter.Gamemode == faction.Key);
                    if (s == null) {
                        _Logger.LogWarning($"missing {nameof(BarUserSkill)} for faction [userID={user.UserID}] [gamemode={faction.Key}]");
                        continue;
                    }

                    double peakOs = 0d;
                    double peakUncert = 0d;
                    foreach (BarMatch match in allGames) {
                        if (match.Gamemode != faction.Key) {
                            continue;
                        }

                        BarMatchPlayer? matchPlayer = playerDict.GetValueOrDefault(match.ID);
                        if (matchPlayer == null) {
                            _Logger.LogWarning($"user was in a match but lacks a player entry? [match.ID={match.ID}] [userID={user.UserID}");
                            continue;
                        }

                        if (matchPlayer.Skill > peakOs) {
                            peakOs = matchPlayer.Skill;
                            peakUncert = matchPlayer.SkillUncertainty;
                        }
                    }

                    int playCount = faction.Sum(iter => iter.PlayCount);
                    int winCount = faction.Sum(iter => iter.WinCount);
                    decimal winRate = Math.Truncate((decimal)winCount / playCount * 100m);

                    embed.AddField($"{BarGamemode.GetName(s.Gamemode)}",
                        $"current: {s.Skill}±{s.SkillUncertainty}\n"
                        + $"highest: {peakOs}±{peakUncert}\n"
                        + $"{winRate}% won of {playCount}",
                        true
                    );
                }
                embed.Description += "\n";

            } else if (skill.Count == 0) {
                embed.AddField("ERROR", "no skill stats found!");
            } else if (factionStats.Count == 0) {
                embed.AddField("ERROR", "no faction stats found!");
            } else {
                embed.AddField("ERROR", "Missing both skill and faction stats");
            }

            // show faction stats (aggregate over gamemode)
            if (factionStats.Count > 0) {
                embed.Description += "**Factions:**\n";

                List<IGrouping<byte, BarUserFactionStats>> grouped = factionStats.GroupBy(iter => iter.Faction).ToList();
                foreach (IGrouping<byte, BarUserFactionStats> faction in grouped.OrderByDescending(iter => iter.Sum(i2 => i2.PlayCount))) {
                    int playCount = faction.Sum(iter => iter.PlayCount);
                    int winCount = faction.Sum(iter => iter.WinCount);
                    embed.Description += $"**{BarFaction.GetName(faction.Key)}** - {Math.Truncate((decimal)winCount / playCount * 100m)}% won of {playCount} played\n";
                }
                embed.Description += "\n";
            }

            // show 3 most played maps
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

            // get recent games
            List<BarMatch> recentGames = allGames.Take(4).ToList();
            if (recentGames.Count > 0) {
                embed.Description += $"**Recent public PvP games:**\n";

                foreach (BarMatch match in recentGames) {
                    string title = await _GetMatchTitle(match, cancel);
                    embed.Description += $"[{title}](<https://{_Instance.GetHost()}/match/{match.ID}>)\n";
                }
            }

            // the only fields added are the gamemode ones, so this makes it look a bit nicer
            if (embed.Fields.Count > 0) {
                embed.Description += "\n**Gamemodes**:";
            }

            return embed;
        }

        /// <summary>
        ///     get match info about a specific match and turn it into an embed
        /// </summary>
        /// <param name="match"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<DiscordEmbedBuilder> _GetMatchInfo(BarMatch match, long? focusedUser, CancellationToken cancel) {
            DiscordEmbedBuilder embed = new();

            embed.Title = await _GetMatchTitle(match, cancel);
            embed.Url = $"https://{_Instance.GetHost()}/match/{match.ID}";
            embed.Timestamp = match.StartTime;
            embed.Color = DiscordColor.Green;

            embed.WithThumbnail($"https://api.bar-rts.com/maps/{match.MapName}/texture-lq.jpg");

            List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(match.ID, cancel);
            List<BarMatchAllyTeam> allyTeams = (await _AllyTeamDb.GetByGameID(match.ID, cancel))
                .OrderBy(iter => iter.AllyTeamID).ToList();

            embed.Description += $"**Map**: {match.Map}\n";
            embed.Description += $"**Duration**: {TimeSpan.FromMilliseconds(match.DurationMs).GetRelativeFormat()}\n";
            embed.Description += $"**Gamemode**: {BarGamemode.GetName(match.Gamemode)}\n";
            embed.Description += $"**Player count**: {match.PlayerCount}\n";

            embed.Description += $"## Teams\n";
            foreach (BarMatchAllyTeam allyTeam in allyTeams) {
                List<BarMatchPlayer> teamPlayers = players.Where(iter => iter.AllyTeamID == allyTeam.AllyTeamID)
                    .OrderByDescending(iter => iter.Skill).ToList();

                embed.Description += $"**Team {allyTeam.AllyTeamID + 1}** - ";

                if (allyTeam.Won == true) {
                    embed.Description += "||Won!||";
                } else {
                    embed.Description += $"||Lost ||";
                }
                embed.Description += "\n";

                if (teamPlayers.Count <= 8) {
                    foreach (BarMatchPlayer p in teamPlayers) {
                        embed.Description += $"{_GetEmoji(p.Faction.ToLower())}";
                        if (p.UserID == focusedUser) {
                            embed.Description += $" **{p.Name.EscapeDiscordCharacters()}** ";
                        } else {
                            embed.Description += $" {p.Name.EscapeDiscordCharacters()} ";
                        }
                        embed.Description += $"- `[{p.Skill}]`\n";
                    } 
                } else {
                    embed.Description += $"{teamPlayers.Count} players\n";
                }

                embed.Description += "\n";
            }

            return embed;
        }

        /// <summary>
        ///     get the title of a <see cref="BarMatch"/>
        /// </summary>
        /// <param name="match"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<string> _GetMatchTitle(BarMatch match, CancellationToken cancel) {
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

            return title;
        }

        /// <summary>
        ///     get a string representing an emoji based on a name
        /// </summary>
        /// <param name="name">name of the emoji</param>
        /// <returns></returns>
        private string? _GetEmoji(string name) => _DiscordOptions.Value.Emojis.GetValueOrDefault(name); 

        private async Task<Result<UserSearchResult, string>> _GetPlayer(string name, CancellationToken cancel) {
            List<UserSearchResult> users = (await _UserDb.SearchByName(name, false, cancel))
                .OrderByDescending(iter => iter.LastUpdated).ToList();

            // if 1 user, use that one, else try to find the user with the exact match, otherwise didn't find user
            UserSearchResult? exactMatch = users.FirstOrDefault(iter => iter.Username.ToLower() == name.ToLower());
            UserSearchResult? user = (users.Count == 1) ? users[0] : exactMatch;

            if (user != null) {
                return user;
            }

            if (users.Count > 1) {
                return "more than 1 possible user found";
            }

            return "no player found";
        }

    }
}
