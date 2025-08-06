using DSharpPlus;
using DSharpPlus.Entities;
using gex.Code.ExtensionMethods;
using gex.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace gex.Services.Discord {

    /// <summary>
    ///     A wrapper around a <see cref="DiscordClient"/>, used as a singleton so multiple services can share a client
    /// </summary>
    public class DiscordWrapper {

        private readonly ILogger<DiscordWrapper> _Logger;
        private readonly DiscordClient _Discord;
        private IOptions<DiscordOptions> _DiscordOptions;

        /// <summary>
        ///     Cache of what guild a user is found in
        /// </summary>
        private Dictionary<ulong, ulong> _CachedMembership = new(); // <user id, guild id>

        public DiscordWrapper(ILogger<DiscordWrapper> logger, ILoggerFactory loggerFactory,
                IOptions<DiscordOptions> discordOptions) {

            _Logger = logger;

            _DiscordOptions = discordOptions;

            try {
                _Discord = new DiscordClient(new DiscordConfiguration() {
                    Token = _DiscordOptions.Value.Key,
                    TokenType = TokenType.Bot,
                    LoggerFactory = loggerFactory,
                    Intents = DiscordIntents.AllUnprivileged
                });
            } catch (Exception) {
                throw;
            }
        }

        public DiscordClient Get() {
            return _Discord;
        }

        /// <summary>
        ///     Get a <see cref="DiscordMember"/> from an ID
        /// </summary>
        /// <param name="memberID">ID of the Discord member to get</param>
        /// <returns>
        ///     The <see cref="DiscordMember"/> with the corresponding ID, or <c>null</c>
        ///     if the user could not be found in any guild the bot is a part of
        /// </returns>
        public async Task<DiscordMember?> GetDiscordMember(ulong memberID) {
            // check if cached
            if (_CachedMembership.TryGetValue(memberID, out ulong guildID) == true) {
                DiscordGuild? guild = await _Discord.TryGetGuild(guildID);
                if (guild == null) {
                    _Logger.LogWarning($"Failed to get guild {guildID} from cached membership for member {memberID}");
                } else {
                    DiscordMember? member = await guild.TryGetMember(memberID);
                    // if the member is null, and was cached, then cache is bad
                    if (member == null) {
                        _Logger.LogWarning($"Failed to get member {memberID} from guild {guildID}");
                        _CachedMembership.Remove(memberID);
                    } else {
                        _Logger.LogDebug($"Found member {memberID} from guild {guildID}/{guild.Name} (cached)");
                        return member;
                    }
                }
            }

            // check each guild and see if it contains the target member
            foreach (KeyValuePair<ulong, DiscordGuild> entry in _Discord.Guilds) {
                DiscordMember? member = await entry.Value.TryGetMember(memberID);

                if (member != null) {
                    _Logger.LogDebug($"Found member {memberID} from guild {entry.Value.Id}/{entry.Value.Name}");
                    _CachedMembership[memberID] = entry.Value.Id;
                    return member;
                }
            }

            _Logger.LogWarning($"Cannot get member {memberID}, not cached and not in any guilds");

            return null;
        }

    }
}
