namespace gex.Common.Models.Options {

    public class FileStorageOptions {

        /// <summary>
        ///     where replay files are stored
        /// </summary>
        public string ReplayLocation { get; set; } = "";

        /// <summary>
        ///     folder where temp files being worked on are stored
        /// </summary>
        public string TempWorkLocation { get; set; } = "";

        /// <summary>
        ///     folder where game versions are stored
        /// </summary>
        public string EngineLocation { get; set; } = "";

        /// <summary>
        ///     folder where game logs from running the games locally are stored
        /// </summary>
        public string GameLogLocation { get; set; } = "";

        /// <summary>
        ///     where downloaded files from the BAR api go (such as map images)
        /// </summary>
        public string WebImageLocation { get; set; } = "";

        /// <summary>
        ///     where downloaded GitHub data will go
        /// </summary>
        public string GitHubDataLocation { get; set; } = "";

        /// <summary>
        ///     where compressed unit position data will go
        /// </summary>
        public string UnitPositionLocation { get; set; } = "";

    }
}
