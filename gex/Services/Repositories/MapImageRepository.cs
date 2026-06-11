using gex.Common.Models;
using gex.Common.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace gex.Services.Repositories {

    public class MapImageRepository {

        private readonly ILogger<MapImageRepository> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;

        private static readonly HttpClient _Http = new HttpClient();

        private const string BASE_URL = "https://api.bar-rts.com";

        static MapImageRepository() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex/0.1 (discord: varunda)");
        }

        public MapImageRepository(ILogger<MapImageRepository> logger,
            IOptions<FileStorageOptions> options) {

            _Logger = logger;
            _Options = options;
        }

        public async Task<Result<string, string>> GetMapPath(string mapName, string size) {
            mapName = mapName.Replace(" ", "%20");

            string mapDir = Path.Join(_Options.Value.WebImageLocation, "maps");
            if (Directory.Exists(mapDir) == false) {
                Directory.CreateDirectory(mapDir);
            }

            string sizeDir = Path.Join(mapDir, size);
            Directory.CreateDirectory(sizeDir);

            string mapPath = Path.Join(sizeDir, mapName + ".jpg");

            if (System.IO.File.Exists(mapPath) == false) {
                string url = BASE_URL + "/maps/" + mapName + "/" + size + ".jpg";
                _Logger.LogDebug($"missing map background for given size, downloading [mapName={mapName}] [size={size}] [url={url}]");

                HttpResponseMessage response = await _Http.GetAsync(url);
                if (response.StatusCode != HttpStatusCode.OK) {
                    return Result<string, string>.Err($"invalid response code for map [code={response.StatusCode}]");
                }

                await File.WriteAllBytesAsync(mapPath, await response.Content.ReadAsByteArrayAsync());
            }

            return Result<string, string>.Ok(mapPath);
        }

    }
}
