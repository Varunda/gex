using gex.Common.Code.ExtensionMethods;
using System;
using System.Text.Json;

namespace gex.Models.Apm {

    public class ApmTournamentEntry {

        public string ID { get; set; } = "";

        public string Name { get; set; } = "";

        public string ShortName { get; set; } = "";

        public string Slug { get; set; } = "";

        public string Status { get; set; } = "";

        public string Mode { get; set; } = "";

        public string FormatType { get; set; } = "";

        public string ApiUrl { get; set; } = "";

        public long? GexMatchPoolID { get; set; }

        public static ApmTournamentEntry Parse(JsonElement json) {
            ApmTournamentEntry entry = new();
            entry.ID = json.GetRequiredString("id");
            entry.Name = json.GetRequiredString("name");
            entry.Status = json.GetRequiredString("status");
            entry.Slug = json.GetRequiredString("slug");
            entry.ApiUrl = json.GetRequiredString("api_url");

            JsonElement poolID = json.GetRequiredChild("gex_match_pool_id");
            if (poolID.ValueKind != JsonValueKind.Null) {
                string value = poolID.GetString()
                    ?? throw new Exception($"expected a string value in a non-null gex_match_pool_id");

                entry.GexMatchPoolID = long.Parse(value);
            }

            return entry;

        }

    }
}
