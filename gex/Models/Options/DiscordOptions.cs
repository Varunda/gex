namespace gex.Models.Options {

    public class DiscordOptions {

        /// <summary>
        ///     If Discord features are enabled or not
        /// </summary>
        public bool Enabled { get; set; } = false;


        public string Key { get; set; } = "aaa";

        /// <summary>
        ///     Client key
        /// </summary>
        public string ClientId { get; set; } = "";

        /// <summary>
        ///     client secret
        /// </summary>
        public string ClientSecret { get; set; } = "";

    }

}
