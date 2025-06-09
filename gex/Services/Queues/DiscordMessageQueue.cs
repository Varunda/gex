using gex.Models.Discord;
using gex.Services.Metrics;
using Microsoft.Extensions.Logging;
using System;

namespace gex.Services.Queues {

    /// <summary>
    ///     Queue of messages to be sent in Discord
    /// </summary>
    public class DiscordMessageQueue : BaseQueue<AppDiscordMessage> {

        public DiscordMessageQueue(ILoggerFactory factory, QueueMetric metrics) : base(factory, metrics) { }

        public new void Queue(AppDiscordMessage msg) {
            if ((msg.ChannelID == null || msg.ChannelID == 0)
                && (msg.GuildID == null || msg.GuildID == 0)
                && (msg.TargetUserID == 0 || msg.TargetUserID == 0)) {

                throw new ArgumentException($"No valid target for message given. You must specify a ChannelID and GuildID, or a TargetUserID");
            }

            _Items.Enqueue(msg);
            _Signal.Release();
        }

    }

}
