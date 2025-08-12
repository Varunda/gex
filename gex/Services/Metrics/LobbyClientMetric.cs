using gex.Code;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace gex.Services.Metrics {

    [MetricName(LobbyClientMetric.NAME)]
    public class LobbyClientMetric {

        public const string NAME = "Gex.LobbyClient";

        private readonly Meter _Meter;

        private readonly Counter<long> _CommandsReceived;
        private readonly Counter<long> _CommandsSent;
        private readonly Counter<long> _ConnectionCount;
        private readonly Counter<long> _DisconnectCount;
        private readonly Counter<long> _WriteErrors;

        public LobbyClientMetric(IMeterFactory meterFactory) {
            _Meter = meterFactory.Create(NAME);

            _CommandsReceived = _Meter.CreateCounter<long>(
                name: "gex_lobby_client_commands_received",
                description: "how many commands Gex has gotten from the lobby client"
            );
            _CommandsSent = _Meter.CreateCounter<long>(
                name: "gex_lobby_client_commands_sent",
                description: "how many commands Gex has sent to the lobby client"
            );
            _ConnectionCount = _Meter.CreateCounter<long>(
                name: "gex_lobby_client_connects_made",
                description: "how many times Gex has connected to the lobby"
            );
            _DisconnectCount = _Meter.CreateCounter<long>(
                name: "gex_lobby_client_disconnects",
                description: "how many times Gex has disconnected from the lobby"
            );
            _WriteErrors = _Meter.CreateCounter<long>(
                name: "gex_lobby_write_error",
                description: "how many times Gex has errored when writing a message"
            );
        }

        public void RecordCommandReceived(string command) {
            _CommandsReceived.Add(1, new KeyValuePair<string, object?>("command", command));
        }

        public void RecordCommandSent(string command) {
            _CommandsSent.Add(1, new KeyValuePair<string, object?>("command", command));
        }

        public void RecordConnect() {
            _ConnectionCount.Add(1);
        }

        public void RecordDisconnect() {
            _DisconnectCount.Add(1);
        }

        public void RecordWriteError(string command) {
            _WriteErrors.Add(1, new KeyValuePair<string, object?>("command", command));
        }

    }
}
