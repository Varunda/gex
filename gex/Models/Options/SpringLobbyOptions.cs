namespace gex.Models.Options {

    public class SpringLobbyOptions {

        /// <summary>
        ///     will Gex try to connect to a Spring lobby
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        ///     host of the Spring lobby
        /// </summary>
        public string Host { get; set; } = "";

        /// <summary>
        ///     port the Spring lobby is on
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     username to use when connecting
        /// </summary>
        public string Username { get; set; } = "";

        /// <summary>
        ///     password to the user
        /// </summary>
        public string Password { get; set; } = "";

    }
}
