using System;
using System.Net;
using System.Text.Json;

namespace gex.Models.Apm {

    public class ApmWebsiteResponse {

        public JsonElement Json { get; set; }

        public HttpStatusCode StatusCode { get; set; }

        public DateTime RateLimitRetryAfter { get; set; }

        public int RateLimitLeft { get; set; }

        public int RateLimitCount { get; set; }

    }
}
