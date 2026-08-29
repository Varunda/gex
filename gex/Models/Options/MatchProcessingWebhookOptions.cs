namespace gex.Models.Options {

    public class MatchProcessingWebhookOptions {

        /// <summary>
        ///     URL of the proxy. if using the gex.WebhookProxy project, include the trailing /proxy as part of the URL
        /// </summary>
        public string? Proxy { get; set; } = null;

        /// <summary>
        ///     secret sent to the proxy to ensure that Gex is the one sending webhooks to be proxied.
        ///     checked by the proxy 
        /// </summary>
        public string? ProxySecret { get; set; } = null;

    }
}
