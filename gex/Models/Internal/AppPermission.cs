using System.Collections.Generic;

namespace gex.Models.Internal {

    public class AppPermission {

        /// <summary>
        ///     List of all <see cref="AppPermission"/>s that exist
        /// </summary>
        public readonly static List<AppPermission> All = new();

        /// <summary>
        ///     Unique ID of the permission
        /// </summary>
        public string ID { get; }

        /// <summary>
        ///     What this permission grants
        /// </summary>
        public string Description { get; }

        public AppPermission(string ID, string desc) {
            this.ID = ID;
            this.Description = desc;

            AppPermission.All.Add(this);
        }

        public const string APP_ACCOUNT_ADMIN = "App.Account.Admin";
        public readonly static AppPermission AppAccountAdmin = new(APP_ACCOUNT_ADMIN, "Manage all accounts");

        public const string APP_DISCORD_ADMIN = "App.Discord.Admin";
        public readonly static AppPermission AppDiscordAdmin = new(APP_DISCORD_ADMIN, "Manage the Discord bot");

        public const string APP_ACCOUNT_GETALL = "App.Account.GetAll";
        public readonly static AppPermission AppAccountGetAll = new(APP_ACCOUNT_GETALL, "Get all accounts");

        public const string GEX_MATCH_UPLOAD = "Gex.Match.Upload";
        public readonly static AppPermission GexMatchUpload = new(GEX_MATCH_UPLOAD, "Upload a match to Gex");

        public const string GEX_MATCH_FORCE_REPLAY = "Gex.Match.ForceReplay";
        public readonly static AppPermission GexMatchForceReplay = new(GEX_MATCH_FORCE_REPLAY, "Force a match to be replayed on Gex");

        public const string GEX_DEV = "Gex.Dev";
        public readonly static AppPermission GexDev = new(GEX_DEV, "Developer permissions");

        public const string GEX_MATCH_POOL_CREATE = "Gex.MatchPool.Create";
        public readonly static AppPermission GexMatchPoolCreate = new(GEX_MATCH_POOL_CREATE, "Create a match pool");

        public const string GEX_MATCH_POOL_DELETE = "Gex.MatchPool.Delete";
        public readonly static AppPermission GexMatchPoolDelete = new(GEX_MATCH_POOL_DELETE, "Delete a match pool");

        public const string GEX_MATCH_POOL_ENTRY_ADDREMOVE = "Gex.MatchPoolEntry.AddRemove";
        public readonly static AppPermission GexMatchPoolEntryAddRemove = new(GEX_MATCH_POOL_ENTRY_ADDREMOVE, "Add and remove entries to a match pool");

    }
}
