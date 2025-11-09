using System.Collections.Generic;

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

        public string Username { get; set; } = "";

        public string Password { get; set; } = "";

        public bool FamiliarsHaveCasterPerms { get; set; } = false;

    }

}
