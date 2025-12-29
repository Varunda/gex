using gex.Common.Models.Options;
using Microsoft.Extensions.Options;
using System;
using System.IO;

namespace gex.Common.Services {

    /// <summary>
    ///     util service to get the path of an engine version (which is specific if running on linux or windows)
    /// </summary>
    public class EnginePathUtil {

        private readonly IOptions<FileStorageOptions> _Options;

        public EnginePathUtil(IOptions<FileStorageOptions> options) {
            _Options = options;

            if (string.IsNullOrEmpty(_Options.Value.EngineLocation)) {
                throw new Exception($"missing FileStorage:EngineLocation, set this in env.json");
            }
        }

        public string Get(string version) {
            string path = Path.GetFullPath(Path.Join(_Options.Value.EngineLocation, version));

            if (OperatingSystem.IsWindows() == true) {
                path += "-win";
            } else if (OperatingSystem.IsLinux() == true) {
                path += "-linux";
            } else {
                throw new Exception($"unexpected operating system, not on linux or windows?");
            }

            return path;
        }

    }
}
