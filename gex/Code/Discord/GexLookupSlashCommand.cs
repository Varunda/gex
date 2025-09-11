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
using gex.Services.Db.Event;
using gex.Services.Db.Match;
using gex.Services.Db.UserStats;
using gex.Services.Lobby;
using gex.Services.Parser;
using gex.Services.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Discord {

    [SlashCommandGroup("gex", "Gex commands")]
    public class GexLookupSlashCommand : ApplicationCommandModule {

        public ILogger<GexLookupSlashCommand> _Logger { set; private get; } = default!;
        public IOptions<DiscordOptions> _DiscordOptions { set; private get; } = default!;

        private static readonly NumberFormatInfo _NumberFormatter = new() {
            NumberGroupSeparator = "'",
            NumberGroupSizes = [ 3 ],
            NumberDecimalDigits = 0
        };

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
        public ILobbyClient _LobbyClient { set; private get; } = default!;
        public LobbyAlertDb _LobbyAlertDb { set; private get; } = default!;
        public BarMapRepository _MapRepository { set; private get; } = default!;
        public IGithubDownloadRepository _UnitGithubRepository { set; private get; } = default!;
        public BarUnitParser _BarUnitParser { set; private get; } = default!;
        public GameEventUnitDefDb _UnitDefDb { set; private get; } = default!;
        public BarWeaponDefinitionRepository _WeaponDefinitionRepository { set; private get; } = default!;
        public BarUnitRepository _UnitRepository { set; private get; } = default!;
        public BarI18nRepository _I18nRepository { set; private get; } = default!;

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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            Stopwatch timer = Stopwatch.StartNew();

            DiscordWebhookBuilder builder = new();
            DiscordEmbedBuilder embed = new();
            embed.Title = $"Lobby lookup: `{name}`";

            Result<UserSearchResult, string> player = await _GetPlayer(name, cancel);

            do {
                // no player
                if (player.IsOk == false) {
                    embed.Description = $"{player.Error}\n\n"
                        + $"-# Gex only processes public PvP games, if this user plays only PvE games, or only private games, Gex does not know about them";
                    embed.Color = DiscordColor.Red;
                    break;
                }

                UserSearchResult user = player.Value;
                embed.Title = $"Lobby lookup: `{name}`";

                // user not online
                LobbyUser? lobbyUser = _LobbyManager.GetUser(user.Username);
                if (lobbyUser == null) {
                    embed.Description = $"This user currently not online";
                    embed.Color = DiscordColor.Yellow;
                    break;
                }

                // user not in a lobby
                LobbyBattle? userBattle = _LobbyManager.GetBattles().FirstOrDefault(iter => iter.Users.Contains(user.UserID));
                if (userBattle == null) {
                    embed.Description = $"This user is not in a lobby";
                    embed.Color = DiscordColor.Yellow;
                    break;
                }

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

                // don't give player names
                if (userBattle.BattleStatus == null 
                    || ((DateTime.UtcNow - userBattle.BattleStatus.Timestamp) > TimeSpan.FromMinutes(5))
                    || userBattle.BattleStatus.Clients.Count > 16) {

                    break;
                }

                // only show teams if the game has started
                if (founder?.InGame == false) {
                    foreach (LobbyBattleStatusClient client in userBattle.BattleStatus.Clients) {
                        embed.Description += $"{client.Username} - `[{client.Skill}]`\n";
                    }
                    embed.Description += "\n";
                    break;
                }

                // this fun bit of codes guesses the ally teams based on player IDs
                int teamCount = userBattle.TeamCount;
                int maxPlayerId = userBattle.BattleStatus.Clients.Select(iter => iter.PlayerID).Max();
                int perTeam = userBattle.TeamSize;

                List<KeyValuePair<int, List<LobbyBattleStatusClient>>> allyTeams = [];

                List<LobbyBattleStatusClient> clients = userBattle.BattleStatus.Clients;
                KeyValuePair<int, List<LobbyBattleStatusClient>>? currentTeam = null;
                // player ID is 1 indexed
                for (int i = 1; i <= clients.Count; ++i) {
                    currentTeam ??= new KeyValuePair<int, List<LobbyBattleStatusClient>>(teamCount - allyTeams.Count, []);

                    LobbyBattleStatusClient? client = clients.FirstOrDefault(iter => iter.PlayerID == i);
                    if (client != null) {
                        currentTeam.Value.Value.Add(client);
                    } else {
                        _Logger.LogWarning($"missing player ID while generating ally teams [battleID={userBattle.BattleID}] [playerID={i}]");
                    }

                    if (currentTeam.Value.Value.Count >= perTeam) {
                        allyTeams.Add(currentTeam.Value);
                        currentTeam = null;
                    }
                }

                foreach (KeyValuePair<int, List<LobbyBattleStatusClient>> iter in allyTeams.OrderBy(iter => iter.Key)) {
                    embed.Description += $"**Team {iter.Key}**\n";
                    foreach (LobbyBattleStatusClient client in iter.Value.OrderByDescending(iter => iter.Skill)) {
                        embed.Description += $"{client.Username.Replace("_", "\\_")} - `[{client.Skill:F2}]`\n";
                    }
                    embed.Description += "\n";
                }

            } while (false);

            embed.WithFooter($"generated in {timer.ElapsedMilliseconds}ms");

            builder.AddEmbed(embed);

            await ctx.EditResponseAsync(builder);
        }

        /// <summary>
        ///     print unit info
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        [SlashCommand("unit", "Print unit information")]
        public async Task UnitLookupCommand(InteractionContext ctx,
            [Autocomplete(typeof(UnitNameProvider))]
            [Option("unit", "Unit name")] string name) {

            static string _N(double v) {
                if (v < 1d && v > 0d) {
                    return v.ToString("F2");
                }
                return v.ToString("N", _NumberFormatter);
            }

            static string _D(double v) {
                return v.ToString("F2");
            }

            await ctx.CreateDeferred(ephemeral: false);
            Stopwatch timer = Stopwatch.StartNew();

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

            if (_UnitRepository.HasUnit(name) == false) {
                // if the definition name was not found, try to be helpful and find the unit based on display name
                List<BarUnitName> names = await _UnitRepository.GetAllNames(cts.Token);
                List<BarUnitName> nameMatch = names.Where(iter => iter.DisplayName.Equals(name, StringComparison.CurrentCultureIgnoreCase)).ToList();
                if (nameMatch.Count == 1) {
                    name = nameMatch[0].DefinitionName;
                } else {
                    await ctx.EditResponseEmbed(new DiscordEmbedBuilder()
                        .WithTitle($"Unit lookup: {name}")
                        .WithDescription($"Unit does not exist")
                        .WithColor(DiscordColor.Red));
                    return;
                }
            }

            Result<BarUnit, string> unitResult = await _UnitRepository.GetByDefinitionName(name, cts.Token);
            if (unitResult.IsOk == false) {
                await ctx.EditResponseEmbed(new DiscordEmbedBuilder()
                    .WithTitle($"Unit lookup: {name}")
                    .WithDescription($"Failed to load unit information: {unitResult.Error}")
                    .WithColor(DiscordColor.Red));
                return;
            }

            DiscordEmbedBuilder embed = new();

            string? unitName = await _I18nRepository.GetString("units", $"units.names.{name}", cts.Token);
            if (string.IsNullOrEmpty(unitName) == false) {
                embed.Title = $"{unitName} (`{name}`)";
            } else {
                embed.Title = $"{name}";
            }

            BarUnit unit = unitResult.Value;

            embed.Color = DiscordColor.Gray;
            embed.WithThumbnail($"https://{_Instance.GetHost()}/image-proxy/UnitPic?defName={name}");
            embed.WithUrl($"https://{_Instance.GetHost()}/unit/{name}");
            embed.Description = $"";

            string? desc = await _I18nRepository.GetString("units", $"units.descriptions.{name}", cts.Token);
            if (string.IsNullOrEmpty(desc) == false) {
                embed.Description += $"### {desc.Replace("_", "\\_")}\n\n";
            }

            if (name.StartsWith("arm")) {
                embed.Description += $"**Faction**: {_GetEmoji("armada")} Armada\n";
                embed.Color = DiscordColor.Cyan;
            } else if (name.StartsWith("cor")) {
                embed.Description += $"**Faction**: {_GetEmoji("cortex")} Cortex\n";
                embed.Color = DiscordColor.IndianRed;
            } else if (name.StartsWith("leg")) {
                embed.Description += $"**Faction**: {_GetEmoji("legion")} Legion\n";
                embed.Color = DiscordColor.SpringGreen;
            } else if (name.StartsWith("raptor_")) {
                embed.Color = DiscordColor.Brown;
            }

            embed.Description += $"**Health**: {_N(unit.Health)}\n"
                + $"**Cost**: {_N(unit.MetalCost)} M / {_N(unit.EnergyCost)} E / {_N(unit.BuildTime)} B\n"
                + $"**Speed**: {_N(unit.Speed)} / {_N(900d * unit.Acceleration)} accel / {_N(30d * unit.TurnRate * (180d / 32768d))}° per sec turning\n"
                + $"**Vision**: {_N(unit.SightDistance)} ";

            if (unit.AirSightDistance > 0) { embed.Description += $" / {_N(unit.AirSightDistance)} (air) "; }
            embed.Description += "\n";

            if (unit.RadarDistance > 0 && unit.SonarDistance > 0) {
                embed.Description += $"**Radar**: {_N(unit.RadarDistance)}";
                if (unit.RadarDistance != unit.SonarDistance) {
                    embed.Description += $" / {_N(unit.SonarDistance)} (sonar)";
                }
                embed.Description += "\n";
            } else if (unit.RadarDistance > 0 && unit.SonarDistance == 0) {
                embed.Description += $"**Radar**: {_N(unit.RadarDistance)}\n";
            } else if (unit.RadarDistance == 0 && unit.SonarDistance > 0) {
                embed.Description += $"**Sonar**: {_N(unit.SonarDistance)}\n";
            }

            if (unit.JamDistance > 0) {
                embed.Description += $"**Jamming**: {_N(unit.JamDistance)}\n";
            }

            if (unit.EnergyProduced != 0) {
                embed.Description += $"**Energy made**: {_N(unit.EnergyProduced)}\n";
            }
            if (unit.WindGenerator != 0) {
                embed.Description += $"**Energy made** (wind): {_N(unit.WindGenerator)}\n";
            }
            if (unit.MetalExtractor == true) {
                embed.Description += $"**Metal extractor?**: Yes\n";
            }
            if (unit.EnergyUpkeep != 0) {
                embed.Description += $"**Energy upkeep**: {_N(unit.EnergyUpkeep)}\n";
            }
            if (unit.MetalProduced != 0) {
                embed.Description += $"**Metal made**: {_N(unit.MetalProduced)}\n";
            }
            if (unit.EnergyStorage != 0) {
                embed.Description += $"**E store**: {_N(unit.EnergyStorage)}\n";
            }
            if (unit.MetalStorage != 0) {
                embed.Description += $"**M store**: {_N(unit.MetalStorage)}\n";
            }

            if (unit.CloakCostStill != 0 || unit.CloakCostMoving != 0) {
                embed.Description += $"**Cloaking**: {_N(unit.CloakCostStill)} / {_N(unit.CloakCostMoving)}\n";
            }

            if (unit.SelfDestructCountdown != 5d) {
                embed.Description += $"**Self-D**: ";

                Result<BarWeaponDefinition, string> selfDWeapon = await _WeaponDefinitionRepository.GetWeaponDefinition(unit.SelfDestructWeapon, cts.Token);
                if (selfDWeapon.IsOk) {
                    BarWeaponDefinition def = selfDWeapon.Value;
                    double dmg = def.Damages.GetValueOrDefault("default");
                    embed.Description += $"{_N(dmg)} max dmg, range {_N(def.AreaOfEffect)}{(def.IsParalyzer ? $" (EMP for {def.ParalyzerTime}s)" : "")}\n";
                } else {
                    embed.Description += $"missing weapon {unit.SelfDestructWeapon}\n";
                }
            }

            if (unit.BuildPower != 0) {
                if (unit.CanResurrect == true) {
                    embed.Description += $"### Resurrection\n";
                } else {
                    embed.Description += $"### Builder\n";
                }

                embed.Description += $"**Build power**: {_N(unit.BuildPower)}\n";
                embed.Description += $"**Range**: {_N(unit.BuildDistance)}\n";
            }

            List<BarUnitWeapon> interestingWeapons = unit.Weapons.Where(iter => {
                return iter.WeaponDefinition.IsBogus == false
                    && iter.Count > 0
                    && (iter.WeaponDefinition.WeaponType == "Shield"
                        || iter.WeaponDefinition.CarriedUnit != null
                        || iter.GetDefaultDamage() > 0d
                    );
            }).ToList();

            if (interestingWeapons.Count > 0) {
                embed.Description += $"## Weapons\n";

                foreach (BarUnitWeapon wep in interestingWeapons) {
                    BarWeaponDefinition weapon = wep.WeaponDefinition;
                    embed.Description += $"**{(wep.Count != 1 ? $"[{wep.Count}x] " : "")}{weapon.Name}** ({weapon.DefinitionName})\n";

                    if (weapon.WeaponType == "Shield") {
                        if (weapon.ShieldData == null) {
                            embed.Description += $"missing shield data!\n\n";
                        } else {
                            double amt = weapon.ShieldData.Power;
                            embed.Description += $"Amount: {_N(amt)}\n"
                                + $"Recharge: {_N(weapon.ShieldData.PowerRegen)}\n\n";
                        }
                    } else {
                        double damage = wep.GetDefaultDamage();

                        if (weapon.CarriedUnit != null) {
                            embed.Description += $"Drone: {weapon.CarriedUnit.DefinitionName}\n";
                            Result<BarUnit, string> carriedUnit = await _UnitRepository.GetByDefinitionName(weapon.CarriedUnit.DefinitionName, cts.Token);
                            if (carriedUnit.IsOk == true && carriedUnit.Value.Weapons.Count > 0) {
                                weapon = carriedUnit.Value.Weapons[0].WeaponDefinition;
                                damage = carriedUnit.Value.Weapons[0].GetDefaultDamage();
                            }
                        }

                        if (weapon.SweepFire != 0) {
                            damage *= weapon.SweepFire;
                        }

                        double dps = damage / Math.Max(0.01, weapon.ReloadTime);
                        if (weapon.Burst != 0) { dps *= weapon.Burst; }
                        if (weapon.Projectiles != 1) { dps *= weapon.Projectiles; }

                        embed.Description += $"DPS: {_N(dps)} {(weapon.IsParalyzer ? "(EMP)" : "")} "
                            + $"({(weapon.Burst != 0 ? $"{weapon.Burst}x burst, " : "")}"
                            + $"{(weapon.Projectiles != 1 ? $"{weapon.Projectiles}x pellets, " : "")}"
                            + $"{_N(damage)} dmg, {_D(weapon.ReloadTime)}s reload)\n";

                        string range = $"{_N(weapon.Range)}";
                        // if the weapon was changed, this means a carrier weapon is being shown
                        if (weapon.DefinitionName != wep.WeaponDefinition.DefinitionName) {
                            if (wep.WeaponDefinition.CarriedUnit == null) {
                                _Logger.LogWarning($"bad state, expected there to be a carried unit if the weapon in iteration was changed");
                            } else {
                                double engRng = wep.WeaponDefinition.CarriedUnit.EngagementRange;
                                range = $"{_N(weapon.Range + engRng)} ({_N(engRng)} + {_N(weapon.Range)})";
                            }
                        }

                        embed.Description += $"Range: {range}";
                        if (weapon.ImpactOnly == false && weapon.AreaOfEffect >= 12d) {
                            // https://springrts.com/wiki/Gamedev:WeaponDefs#edgeEffectiveness
                            double edgeRange = weapon.AreaOfEffect * 0.99d;
                            double minDamage = damage * ((weapon.AreaOfEffect - edgeRange) / (weapon.AreaOfEffect - (edgeRange * weapon.EdgeEffectiveness)));
                            embed.Description += $" ({_N(weapon.AreaOfEffect)} splash)";
                        }
                        embed.Description += "\n";

                        if (wep.TargetCategory == "NOTSUB") {
                            embed.Description += $"Targets: Air, Boats, Ground";
                        } else if (wep.TargetCategory == "SURFACE") {
                            embed.Description += $"Targets: Boats, Ground";
                        } else if (wep.TargetCategory == "NOTAIR" && weapon.WaterWeapon == false) {
                            embed.Description += $"Targets: Boats, Air";
                        } else if (wep.TargetCategory == "NOTAIR" && weapon.WaterWeapon == true) {
                            embed.Description += $"Targets: Boats, Subs";
                        } else if (wep.TargetCategory == "VTOL") {
                            embed.Description += $"Targets: Air";
                        } else {
                            embed.Description += $"Targets: {wep.TargetCategory}";
                        }

                        embed.Description += "\n\n";
                    }
                }
            }

            embed.WithFooter($"{(unit.ModelAuthor != null ? $"Model by: {unit.ModelAuthor} | " : "")}generated in {timer.ElapsedMilliseconds}ms | updated every 4 hours");

            DiscordWebhookBuilder builder = new();
            builder.AddEmbed(embed);
            builder.AddComponents(
                new DiscordLinkButtonComponent($"https://{_Instance.GetHost()}/unit/{name}", "View on Gex")
            );

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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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
            alert.Timestamp = DateTime.UtcNow;
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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

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

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
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
                embed.Description += $"**Time playing**: {TimeSpan.FromSeconds(allGames.Sum(iter => iter.DurationMs / 1000)).GetRelativeFormat()}\n";
                embed.Description += $"Last seen: {allGames.MaxBy(iter => iter.StartTime)!.StartTime.GetDiscordTimestamp("D")}\n\n";

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

                    embed.Description += $"**{map.Key}** - ";
                    embed.Description += $"{Math.Truncate((decimal)winCount / playCount * 100m)}% won of {playCount} played\n";
                }
                embed.Description += "\n";
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

                if (teamPlayers.Count == 0) {
                    continue;
                }

                if (match.Gamemode == BarGamemode.FFA) {
                    embed.Description += $"**Team {allyTeam.AllyTeamID + 1}** - ";
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
                }
            }

            if (match.Gamemode == BarGamemode.FFA) {
                BarMatchAllyTeam? winningTeam = allyTeams.FirstOrDefault(iter => iter.Won == true);
                if (winningTeam != null) {
                    List<BarMatchPlayer> teamPlayers = players.Where(iter => iter.AllyTeamID == winningTeam.AllyTeamID)
                        .OrderByDescending(iter => iter.Skill).ToList();

                    int longestName = players.OrderByDescending(iter => iter.Name.Length).First().Name.Length;

                    if (teamPlayers.Count == 0) {
                        embed.Description += $"\n**Winner**: ||no player to win?||";
                    } else {
                        embed.Description += $"\n**Winner**: ||`{teamPlayers[0].Name.PadRight(longestName)}`||\n-# names are padded to better hide winner";
                    }
                } else {
                    embed.Description += $"\n**Winner**: no one won!";
                }
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

        /// <summary>
        ///     provider for unit names
        /// </summary>
        public class UnitNameProvider : IAutocompleteProvider {

            private const string CACHE_KEY = "Gex.Discord.UnitNameProvider.Data";

            public async Task<IEnumerable<DiscordAutoCompleteChoice>> Provider(AutocompleteContext ctx) {

                GameEventUnitDefDb unitDefDb = ctx.Services.GetRequiredService<GameEventUnitDefDb>();
                IMemoryCache memoryCache = ctx.Services.GetRequiredService<IMemoryCache>();
                IGithubDownloadRepository githubUnitRepository = ctx.Services.GetRequiredService<IGithubDownloadRepository>();
                BarI18nRepository i18n = ctx.Services.GetRequiredService<BarI18nRepository>();
                ILogger<UnitNameProvider> logger = ctx.Services.GetRequiredService<ILogger<UnitNameProvider>>();

                string value = ctx.OptionValue.ToString() ?? "";

                try {
                    List<DiscordAutoCompleteChoice> choices = await _GetChoices(memoryCache, githubUnitRepository, i18n, logger);

                    List<DiscordAutoCompleteChoice> matches = choices.Where(iter => {
                        return iter.Name.StartsWith(value, StringComparison.OrdinalIgnoreCase);
                    }).ToList();

                    if (matches.Count > 25) {
                        return matches[..25];
                    }
                    return matches;
                } catch (Exception ex) {
                    logger.LogError(ex, "failed to provide unit names");

                    return [];
                }
            }

            private async Task<List<DiscordAutoCompleteChoice>> _GetChoices(IMemoryCache cache,
                IGithubDownloadRepository unitGithubRepo, BarI18nRepository i18n, ILogger<UnitNameProvider> logger
            ) {

                using CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));

                if (cache.TryGetValue(CACHE_KEY, out List<DiscordAutoCompleteChoice>? opts) == false || opts == null) {
                    opts = new List<DiscordAutoCompleteChoice>();

                    List<KeyValuePair<string, string>> i18nNames = await i18n.GetKeysStartingWith("units", "units.names.", cts.Token);
                    if (i18nNames.Count == 0) {
                        throw new Exception($"no units found?");
                    }

                    // create a list of unique names to all of the definition names that use that name
                    // <display name, unit defs[]>
                    Dictionary<string, List<string>> nameSets = [];
                    foreach (KeyValuePair<string, string> iter in i18nNames) {
                        List<string> defNames = nameSets.GetValueOrDefault(iter.Value) ?? new List<string>();
                        defNames.Add(iter.Key["units.names.".Length..]); // remove the prefix units.names. from all entries
                        nameSets[iter.Value] = defNames;
                    }

                    foreach (KeyValuePair<string, List<string>> set in nameSets) {
                        if (set.Value.Count > 1) {

                            // if the definition names change in just faction prefix (e.g. armalab, coralab)
                            // then gex can safely just suffix the label name with the faction,
                            // else just include the full definition name
                            bool justFactionPrefixChanges = set.Value.Select(iter => {
                                if (iter.StartsWith("cor") || iter.StartsWith("arm") || iter.StartsWith("leg")) {
                                    return iter[3..];
                                }
                                return iter;
                            }).Distinct().Count() == 1;

                            if (justFactionPrefixChanges == true) {
                                foreach (string defName in set.Value) {
                                    if ((await unitGithubRepo.GetFile("units", $"{defName}.lua", cts.Token)).IsOk == false) {
                                        logger.LogDebug($"missing unit from units folder [defName={defName}]");
                                        continue;
                                    }

                                    string labelName = set.Key + " ";
                                    if (defName.StartsWith("cor")) {
                                        labelName += "(Cortex)";
                                    } else if (defName.StartsWith("arm")) {
                                        labelName += "(Armada)";
                                    } else if (defName.StartsWith("leg")) {
                                        labelName += "(Legion)";
                                    } else {
                                        labelName += $"({defName})";
                                    }

                                    opts.Add(new DiscordAutoCompleteChoice($"{labelName}", defName));
                                }
                            } else {
                                foreach (string defName in set.Value) {
                                    if ((await unitGithubRepo.GetFile("units", $"{defName}.lua", cts.Token)).IsOk == false) {
                                        logger.LogDebug($"missing unit from units folder [defName={defName}]");
                                        continue;
                                    }

                                    opts.Add(new DiscordAutoCompleteChoice($"{set.Key} ({defName})", defName));
                                }
                            }
                        } else {
                            if ((await unitGithubRepo.GetFile("units", $"{set.Value[0]}.lua", cts.Token)).IsOk == false) {
                                logger.LogDebug($"missing unit from units folder [defName={set.Value[0]}]");
                                continue;
                            }
                            opts.Add(new DiscordAutoCompleteChoice(set.Key, set.Value[0]));
                        }
                    }

                    cache.Set(CACHE_KEY, opts, new MemoryCacheEntryOptions() {
                        SlidingExpiration = TimeSpan.FromHours(1)
                    });
                }

                return opts;
            }

        }

    }
}
