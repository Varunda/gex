using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using gex.Code.Constants;
using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Db;
using gex.Models.Discord;
using gex.Models.Lobby;
using gex.Models.Options;
using gex.Models.UserStats;
using gex.Services;
using gex.Services.Db;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
using gex.Services.Lobby;
using gex.Services.Lobby.Implementations;
using gex.Services.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Discord {

    [SlashCommandGroup("gex", "Gex commands")]
    public class GexLookupSlashCommand : ApplicationCommandModule {

        public ILogger<GexLookupSlashCommand> _Logger { set; private get; } = default!;
        public IOptions<DiscordOptions> _DiscordOptions { set; private get; } = default!;

        public InstanceInfo _Instance { set; internal get; } = default!;
        public BarUserRepository _UserRepository { set; internal get; } = default!;
        public BarUserSkillDb _SkillDb { set; private get; } = default!;
        public BarUserMapStatsDb _MapStatsDb { set; private get; } = default!;
        public BarUserFactionStatsDb _FactionStatsDb { set; private get; } = default!;
        public BarMatchRepository _MatchRepository { set; private get; } = default!;
        public BarMatchPlayerRepository _PlayerRepository { set; private get; } = default!;
        public BarMatchAllyTeamDb _AllyTeamDb { set; private get; } = default!;
        public DiscordBarUserLinkDb _LinkDb { set; private get; } = default!;
        public DiscordSubscriptionMatchProcessedDb _SubscriptionDb { set; private get; } = default!;
        public LobbyManager _LobbyManager { set; private get; } = default!;
        public LobbyClient _LobbyClient { set; private get; } = default!;
        public LobbyAlertDb _LobbyAlertDb { set; private get; } = default!;
        public BarMapRepository _MapRepository { set; private get; } = default!;

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

            List<UserSearchResult> users = (await _UserRepository.SearchByName(name, false, cancel))
                .OrderByDescending(iter => iter.LastUpdated).ToList();

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            embed.Title = $"Player lookup: `{name}`";

            if (users.Count == 0) {
                embed.Description = $"Failed to find any users with this name\n\n"
                    + $"-# Gex only processes public PvP games, if this user plays only PvE games, or only private games, Gex does not know about them";
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
        ///     print the lobby a player is currently in
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [SlashCommand("lobby", "Display lobby info about an online player")]
        public async Task PlayerLobbyCommand(InteractionContext ctx,
            [Option("player", "player name")] string name) {

            await ctx.CreateDeferred(false);

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            Stopwatch timer = Stopwatch.StartNew();

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();
            embed.Title = $"Lobby lookup: `{name}`";

            Result<UserSearchResult, string> player = await _GetPlayer(name, cancel);
            if (player.IsOk == false) {
                embed.Description = $"{player.Error}\n\n"
                    + $"-# Gex only processes public PvP games, if this user plays only PvE games, or only private games, Gex does not know about them";
                embed.Color = DiscordColor.Red;
            } else if (player.IsOk == true) {
                UserSearchResult user = player.Value;

                LobbyUser? lobbyUser = _LobbyManager.GetUser(user.Username);
                if (lobbyUser == null) {
                    embed.Description = $"This user currently not online";
                    embed.Color = DiscordColor.Yellow;
                } else {
                    LobbyBattle? userBattle = _LobbyManager.GetBattles().FirstOrDefault(iter => iter.Users.Contains(user.UserID));
                    if (userBattle == null) {
                        embed.Description = $"This user is not in a lobby";
                        embed.Color = DiscordColor.Yellow;
                    } else {
                        embed.Color = DiscordColor.Green;
                        embed.WithThumbnail($"https://api.bar-rts.com/maps/{userBattle.Map.Replace(" ", "%20")}/texture-lq.jpg");

                        List<string> parts = [..userBattle.Title.Split("|")];
                        string title = parts.First();
                        string conds = string.Join(", ", parts[1..].Select(iter => iter.Trim()));

                        int playerCount = userBattle.Users.Count - userBattle.SpectatorCount + 1; // +1 for the host

                        LobbyUser? founder = _LobbyManager.GetUser(userBattle.FounderUsername);

                        embed.Description = $"**{title}**\n{conds}\n\n";
                        embed.Description += $"**Map**: {userBattle.Map}\n";
                        if (founder != null) {
                            embed.Description += $"**Status**: {(founder.InGame == true ? "Running" : "Idle")}\n";
                        }
                        if (userBattle.Passworded == true) {
                            embed.Description += $"**Passworded**: Yes\n";
                        }
                        if (userBattle.Locked == true) {
                            embed.Description += $"**Locked**: Yes\n";
                        }

                        embed.Description += $"**Players**: {playerCount}/{userBattle.MaxPlayers} ({userBattle.SpectatorCount} specs)\n\n";

                        // update battle status if it is not given, or if the status is over 5 minutes old
                        if (userBattle.BattleStatus == null || ((DateTime.UtcNow - userBattle.BattleStatus.Timestamp) > TimeSpan.FromMinutes(5))) {
                            _Logger.LogDebug($"refreshing battle status for lobby client [battleID={userBattle.BattleID}] [timestamp={userBattle.BattleStatus?.Timestamp:u}]");
                            CancellationTokenSource statusCts = new(TimeSpan.FromSeconds(1));
                            Result<LobbyBattleStatus, string> battleStatus = await _LobbyClient.BattleStatus(userBattle.BattleID, statusCts.Token);

                            if (battleStatus.IsOk == true) {
                                userBattle.BattleStatus = battleStatus.Value;
                            }
                        }

                        if (userBattle.BattleStatus != null && userBattle.BattleStatus.Clients.Count <= 16) {
                            List<int> allyTeams = userBattle.BattleStatus.Clients.Where(iter => iter.AllyTeamID != null)
                                .Select(iter => iter.AllyTeamID!.Value)
                                .Distinct().Order().ToList();

                            foreach (int allyTeamID in allyTeams) {
                                embed.Description += $"**Team {allyTeamID}**\n";
                                foreach (LobbyBattleStatusClient client in userBattle.BattleStatus.Clients) {
                                    if (client.AllyTeamID != allyTeamID) {
                                        continue;
                                    }

                                    embed.Description += $"{client.Username} - `[{client.Skill}]`\n";
                                }
                                embed.Description += "\n";
                            }
                        }
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

            List<UserSearchResult> users = (await _UserRepository.SearchByName(name, false, cancel))
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
        /// <param name="player">name of the player</param>
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
                List<UserSearchResult> users = (await _UserRepository.SearchByName(player, false, cancel))
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

            List<UserSearchResult> users = (await _UserRepository.SearchByName(player, false, cancel))
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

        /// <summary>
        ///     unsubscribe command
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        [SlashCommand("unsubscribe", "Remove a subscription to a player")]
        public async Task Unsubscribe(InteractionContext ctx,
            [Option("player", "Player to unsubscribe to match processed notifications to")] string player) {

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();

            Stopwatch timer = Stopwatch.StartNew();

            List<UserSearchResult> users = (await _UserRepository.SearchByName(player, false, cancel))
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

        [SlashCommand("alert", "Create a new alert about a lobby being open")]
        public async Task CreateAlert(InteractionContext ctx,
            [Choice("5 minutes", 300)]
            [Choice("10 minutes", 600)]
            [Choice("15 minutes", 900)]
            [Choice("30 minutes", 1800)]
            [Choice("1 hour", 3600)]
            [Option("alert_cooldown", "How long to wait between alerts being sent")] long cooldownSeconds,

            [Option("role", "role to ping if role pinging is enabled")] DiscordRole role,
            [Option("ping_role", "will the role get pinged when alerts are sent?")] bool rolePing,

            [Option("min_os", "Minimum OS of all players in the lobby")] long minOS = -1,
            [Option("max_os", "Maximum OS of all players in the lobby")] long maxOS = -1,
            [Option("min_avg_os", "Minimum average OS of players in the lobby")] long minAvgOS = -1,
            [Option("max_avg_os", "Maximum average OS of players in the lobby")] long maxAvgOS = -1,
            [Option("min_player_count", "Minimum number of players playing in the lobby")] long minPlayerCount = -1,
            [Option("max_player_count", "Maximum number of players playing in the lobby")] long maxPlayerCount = -1,
            [Option("map", "Map")] string map = "",

            [Choice("Duel", BarGamemode.DUEL)]
            [Choice("Small team", BarGamemode.SMALL_TEAM)]
            [Choice("Large team", BarGamemode.LARGE_TEAM)]
            [Choice("FFA", BarGamemode.FFA)]
            [Choice("Team FFA", BarGamemode.TEAM_FFA)]
            [Option("gamemode", "What gamemode is being played")] long gamemode = -1
        ) {

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            if (ctx.Member == null || ctx.Channel == null || ctx.Guild == null) {
                await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                    .WithTitle("Must be used in a Discord server")
                    .WithColor(DiscordColor.Red)
                    .WithDescription("Lobby alerts can only be created in discord servers, not DMs")
                , ephemeral: true);
                return;
            }

            Permissions userPerms = ctx.Member.PermissionsIn(ctx.Channel);
            if (userPerms.HasPermission(Permissions.Administrator) == false
                && userPerms.HasPermission(Permissions.ManageChannels) == false
                && userPerms.HasPermission(Permissions.ManageGuild) == false) {

                await ctx.CreateImmediateText("No permission (requires administrator, manage channels or manage guild)", ephemeral: true);
                return;
            }

            if (map != "") {
                BarMap? mapObj = await _MapRepository.GetByName(map, cancel);
                if (mapObj == null) {
                    await ctx.CreateImmediateText($"Failed to find a map named `{map}`");
                    return;
                }
            }

            await ctx.CreateDeferred(ephemeral: false);

            LobbyAlert alert = new();
            alert.CreatedByID = ctx.User.Id;
            alert.ChannelID = ctx.Channel.Id;
            alert.GuildID = ctx.Guild.Id;
            alert.TimeBetweenAlertsSeconds = (int)cooldownSeconds;
            if (rolePing == true) {
                alert.RoleID = role.Id;
            }

            DiscordEmbedBuilder embed = new();
            embed.Title = "Lobby alert created";
            embed.Color = DiscordColor.Green;
            embed.Description = $"Whenever an idle lobby that matches the following conditions is opened, "
                + $"a {(rolePing == true ? "ping" : "message")} will be sent in this channel\n\n";

            if (map != "") {
                alert.Map = map;
                embed.AddField("Map", map);
            }
            if (minOS != -1) {
                alert.MinimumOS = (int)minOS;
                embed.AddField("Minimum OS", $"{alert.MinimumOS}");
            }
            if (maxOS != -1) {
                alert.MaximumOS = (int)maxOS;
                embed.AddField("Maximum OS", $"{alert.MaximumOS}");
            }
            if (minAvgOS != -1) {
                alert.MinimumAverageOS = (int)minAvgOS;
                embed.AddField("Min average OS", $"{alert.MinimumAverageOS}");
            }
            if (maxAvgOS != -1) {
                alert.MaximumAverageOS = (int)maxAvgOS;
                embed.AddField("Max average OS", $"{alert.MaximumAverageOS}");
            }
            if (minPlayerCount != -1) {
                alert.MinimumPlayerCount = (int)minPlayerCount;
                embed.AddField("Min player count", $"{alert.MinimumPlayerCount}");
            }
            if (maxPlayerCount != -1) {
                alert.MaximumPlayerCount = (int)maxPlayerCount;
                embed.AddField("Max player count", $"{alert.MaximumPlayerCount}");
            }

            long alertID = await _LobbyAlertDb.Insert(alert, cancel);
            embed.Description += $"Use `/gex remove-alert {alertID}` to remove this alert";

            await ctx.EditResponseEmbed(embed);
        }

        /// <summary>
        ///     discord command to list all lobby alerts in this channel
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        [SlashCommand("list-alerts", "List all lobby alerts that will be sent in this channel")]
        public async Task ListLobbyAlerts(InteractionContext ctx) {
            if (ctx.Channel == null) {
                await ctx.CreateImmediateText("This command can only be used in channels for a server");
                return;
            }

            await ctx.CreateDeferred(ephemeral: true);

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            List<LobbyAlert> alerts = await _LobbyAlertDb.GetByChannelID(ctx.Channel.Id, cts.Token);

            DiscordEmbedBuilder embed = new();
            embed.Title = "Lobby alerts";
                
            if (alerts.Count == 0) {
                embed.Description = "No lobby alerts for this channel";
            } else {
                embed.Description = "Alerts that will be sent in this channel\n";

                foreach (LobbyAlert alert in alerts) {
                    embed.Description += $"Alert ID {alert.ID} by <@{alert.CreatedByID}>\n";
                }
            }

            await ctx.EditResponseEmbed(embed);
        }

        /// <summary>
        ///     discord command to remove a lobby alert
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="alertID"></param>
        /// <returns></returns>
        [SlashCommand("remove-alert", "Delete a lobby alert")]
        public async Task DeleteLobbyAlert(InteractionContext ctx,
            [Option("alert_id", "ID of the alert")] long alertID) {

            Permissions userPerms = ctx.Member.PermissionsIn(ctx.Channel);
            if (userPerms.HasPermission(Permissions.Administrator) == false
                && userPerms.HasPermission(Permissions.ManageChannels) == false
                && userPerms.HasPermission(Permissions.ManageGuild) == false) {

                await ctx.CreateImmediateText("No permission (requires administrator, manage channels or manage guild)", ephemeral: true);
                return;
            }

            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            LobbyAlert? alert = await _LobbyAlertDb.GetByID(alertID, cts.Token);

            if (alert == null) {
                await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                    .WithTitle("Alert not found")
                    .WithDescription($"Failed to find alert `{alertID}`")
                , ephemeral: true);
                return;
            }

            if (ctx.Channel == null || ctx.Guild == null) {
                await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                    .WithTitle("Invalid usage")
                    .WithDescription($"Command be used in a channel within a server")
                , ephemeral: true);
                return;
            }

            if (ctx.Channel.Id != alert.ChannelID) {
                await ctx.CreateImmediateText("Alerts can only be removed in the channel it was created it.\n" +
                    $"Channel: https://discord.com/channels/{alert.GuildID}/{alert.ChannelID}", true);
                return;
            }

            _Logger.LogInformation($"deleting lobby alert [alertID={alertID}] [user={ctx.User.Id}/{ctx.User.Username}]");
            await _LobbyAlertDb.DeleteByID(alertID, cts.Token);

            await ctx.CreateResponseAsync(new DiscordEmbedBuilder()
                .WithTitle("Alert deleted")
                .WithDescription($"Alert {alertID} was successfully deleted")
                .WithColor(DiscordColor.Green)
            );
        }

        /// <summary>
        ///     used to generate play stats for player lookups
        /// </summary>
        /// <param name="user"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<DiscordEmbedBuilder> _GetUserInfo(UserSearchResult user, CancellationToken cancel) {
            DiscordEmbedBuilder embed = new();

            string oldestMatch = $"{(await _MatchRepository.GetOldestMatch(cancel))?.StartTime.GetDiscordTimestamp("D") ?? "no matches in DB??"}";
            embed.Description = $"-# Only includes public PvP games since {oldestMatch}\n\n";
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
                    embed.Description += $"**{_GetEmoji(BarFaction.GetName(faction.Key))}{BarFaction.GetName(faction.Key)}** - {Math.Truncate((decimal)winCount / playCount * 100m)}% won of {playCount} played\n";
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
        /// <param name="match">match to generate the info for</param>
        /// <param name="focusedUser">if given, will bold this user ID in the match data</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        private async Task<DiscordEmbedBuilder> _GetMatchInfo(BarMatch match, long? focusedUser, CancellationToken cancel) {
            DiscordEmbedBuilder embed = new();

            embed.Title = await _GetMatchTitle(match, cancel);
            embed.Url = $"https://{_Instance.GetHost()}/match/{match.ID}";
            embed.Timestamp = match.StartTime;
            embed.Color = DiscordColor.Green;

            embed.WithThumbnail($"https://api.bar-rts.com/maps/{match.MapName.Replace(" ", "%20")}/texture-lq.jpg");

            List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(match.ID, cancel);
            List<BarMatchAllyTeam> allyTeams = (await _AllyTeamDb.GetByGameID(match.ID, cancel))
                .OrderBy(iter => iter.AllyTeamID).ToList();

            embed.Description += $"**Map**: {match.Map}\n";
            embed.Description += $"**Start time**: {match.StartTime.GetDiscordFullTimestamp()}\n";
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
                        title = $"{BarGamemode.GetName(match.Gamemode)}: " + string.Join(" v ", allyTeams.Select(iter => iter.PlayerCount));
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
        private string? _GetEmoji(string name) => _DiscordOptions.Value.Emojis.GetValueOrDefault(name.ToLower()); 

        private async Task<Result<UserSearchResult, string>> _GetPlayer(string name, CancellationToken cancel) {
            List<UserSearchResult> users = (await _UserRepository.SearchByName(name, false, cancel))
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
