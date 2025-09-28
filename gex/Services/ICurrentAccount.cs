using DSharpPlus.SlashCommands;
using gex.Models;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services {

    public interface ICurrentAccount {

        /// <summary>
        ///     get the current user based on the http context
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<AppAccount?> Get(CancellationToken cancel = default);

        /// <summary>
        ///     get a specific claim value based on the claim type
        /// </summary>
        /// <param name="claimType"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<string?> GetClaim(string claimType, CancellationToken cancel = default);

        /// <summary>
        ///     get the account based on the context used in DSharpPlus
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        Task<AppAccount?> GetDiscord(BaseContext ctx);

        /// <summary>
        ///     get the discord ID of the user based on the http context
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        Task<ulong?> GetDiscordID(CancellationToken cancel = default);

    }
}