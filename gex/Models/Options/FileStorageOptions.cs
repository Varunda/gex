namespace gex.Models.Options {

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

    }
}
