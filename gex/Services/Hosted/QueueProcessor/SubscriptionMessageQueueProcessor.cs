using DSharpPlus.Entities;
using gex.Code.Discord;
using gex.Models.Db;
using gex.Models.Discord;
using gex.Models.Queues;
using gex.Services.Db;
using gex.Services.Queues;
using gex.Services.Repositories;
using gex.Services.Util;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Hosted.QueueProcessor {

    public class SubscriptionMessageQueueProcessor : BaseQueueProcessor<SubscriptionMessageQueueEntry> {

        private readonly DiscordSubscriptionMatchProcessedDb _SubscriptionDb;
        private readonly BarMatchRepository _MatchRepository;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BaseQueue<AppDiscordMessage> _MessageQueue;
        private readonly BarMatchTitleUtilService _TitleUtil;
        private readonly InstanceInfo _Instance;

        public SubscriptionMessageQueueProcessor(ILoggerFactory factory,
            BaseQueue<SubscriptionMessageQueueEntry> queue, ServiceHealthMonitor serviceHealthMonitor,
            DiscordSubscriptionMatchProcessedDb subscriptionDb, BarMatchRepository matchRepository,
            BarMatchPlayerRepository playerRepository, BaseQueue<AppDiscordMessage> messageQueue,
            BarMatchTitleUtilService titleUtil, InstanceInfo instance)

        : base("subscription_message_queue", factory, queue, serviceHealthMonitor) {

            _SubscriptionDb = subscriptionDb;
            _MatchRepository = matchRepository;
            _PlayerRepository = playerRepository;
            _MessageQueue = messageQueue;
            _TitleUtil = titleUtil;
            _Instance = instance;
        }

        protected override async Task<bool> _ProcessQueueEntry(SubscriptionMessageQueueEntry entry, CancellationToken cancel) {
            _Logger.LogDebug($"sending discord subscription messages for match [gameID={entry.GameID}] [force={entry.Force}]");

            BarMatch? match = await _MatchRepository.GetByID(entry.GameID, cancel);
            if (match == null) {
                _Logger.LogWarning($"failed to find game to send subscriptions [gameID={entry.GameID}]");
                return false;
            }

            List<BarMatchPlayer> players = await _PlayerRepository.GetByGameID(entry.GameID, cancel);

            foreach (BarMatchPlayer player in players) {
                List<DiscordSubscriptionMatchProcessed> subs = await _SubscriptionDb.GetByUserID(player.UserID, cancel);

                foreach (DiscordSubscriptionMatchProcessed sub in subs) {
                    AppDiscordMessage msg = new();
                    DiscordEmbedBuilder embed = new();
                    embed.Title = await _TitleUtil.GetDiscordTitle(match, cancel);
                    embed.Description = $"View the match on Gex: https://{_Instance.GetHost()}/match/{entry.GameID}\n\n"
                        + $"A game that `{player.Name}` was in was fully replayed on Gex\n\n"
                        + $"-# To stop receiving messages like these, press Unsubscribe below, "
                        + $"or type `/gex unsubscribe {player.Name}`";
                    embed.Url = $"https://{_Instance.GetHost()}/match/{entry.GameID}";
                    embed.WithFooter("Match started at");
                    embed.Timestamp = match.StartTime;
                    embed.Color = DiscordColor.Purple;

                    string thumbnail = $"https://api.bar-rts.com/maps/{match.MapName.Replace(" ", "%20")}/texture-lq.jpg";
                    if (Uri.IsWellFormedUriString(thumbnail, UriKind.Absolute) == true) {
                        embed.WithThumbnail(thumbnail);
                    } else {
                        _Logger.LogWarning($"thumbnail gives an invalid Uri! [thumbnail={thumbnail}]");
                    }

                    msg.Components.Add(SubscribeButtonCommand.REMOVE_PLAYER_SUB(player.UserID));
                    msg.Embeds.Add(embed);

                    _Logger.LogDebug($"sending game processed sub [gameID={entry.GameID}] [userID={sub.UserID}] [discordID={sub.DiscordID}]");
                    msg.TargetUserID = sub.DiscordID;
                    _MessageQueue.Queue(msg);
                }
            }

            return true;
        }

    }
}
