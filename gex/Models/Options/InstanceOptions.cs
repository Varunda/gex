namespace gex.Models.Options {

    public class InstanceOptions {

        /// <summary>
        ///     Root domain of the host. Do not include https://. For example: localhost:6001, gex.honu.pw
        /// </summary>
        public string Host { get; set; } = "";

        /// <summary>
        ///     DANGER SETTING:
        ///     this will treat all requests as coming from an account with all permissions
        /// </summary>
        public bool LocalhostDeveloperAccount { get; set; } = false;

    }
}
