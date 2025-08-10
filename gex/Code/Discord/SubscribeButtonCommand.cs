using DSharpPlus;
using DSharpPlus.ButtonCommands;
using DSharpPlus.Entities;
using gex.Code.ExtensionMethods;
using gex.Models.Db;
using gex.Models.UserStats;
using gex.Services.Db;
using gex.Services.Db.UserStats;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Code.Discord {

    public class SubscribeButtonCommand : ButtonCommandModule {

        public static DiscordButtonComponent REMOVE_PLAYER_SUB(long userID) => new(ButtonStyle.Danger, $"@sub-player-remove.{userID}", "Unsubscribe");

        public ILogger<SubscribeButtonCommand> _Logger { set; private get; } = default!;
        public DiscordSubscriptionMatchProcessedDb _SubscriptionDb { set; private get; } = default!;
        public BarUserDb _UserDb { set; private get; } = default!;

        [ButtonCommand("sub-player-remove")]
        public async Task UnsubscribePlayerEnd(ButtonContext ctx, long userID) {
            CancellationTokenSource cts = new(TimeSpan.FromSeconds(5));
            CancellationToken cancel = cts.Token;

            await ctx.Interaction.CreateDeferred(true);

            BarUser? user = await _UserDb.GetByID(userID, cancel);
            List<DiscordSubscriptionMatchProcessed> subs = await _SubscriptionDb.GetByUserID(userID, cancel);
            DiscordSubscriptionMatchProcessed? sub = subs.FirstOrDefault(iter => iter.DiscordID == ctx.User.Id);

            DiscordEmbedBuilder embed = new();
            if (sub == null) {
                embed.Title = $"Subscription not found";
                embed.Description = $"No subscription to {user?.Username} found";
                embed.Color = DiscordColor.Red;

                await ctx.Interaction.EditResponseEmbed(embed);
                return;
            }

            await _SubscriptionDb.Remove(sub.ID, cancel);

            embed.Title = "Subscription removed";
            embed.Description = $"Subscription to `{user?.Username}` removed. No more messages will be sent when games they are in is processed";
            embed.Color = DiscordColor.Green;

            await ctx.Interaction.EditResponseEmbed(embed);
        }

    }
}
