using Dapper.ColumnMapper;
using gex.Code;
using System;

namespace gex.Models.Db {

    [DapperColumnsMapped]
    public class MatchProcessingWebhook {

        [ColumnMapping("url")]
        public string Url { get; set; } = "";

        [ColumnMapping("type")]
        public string Type { get; set; } = "";

        [ColumnMapping("shared_secret")]
        public string SharedSecret { get; set; } = "";

        [ColumnMapping("include_events")]
        public bool IncludeEvents { get; set; }

        [ColumnMapping("timestamp")]
        public DateTime Timestamp { get; set; }

        [ColumnMapping("ip")]
        public string IP { get; set; } = "";

    }
}
