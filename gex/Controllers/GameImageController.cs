using gex.Models.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace gex.Controllers {

    public class GameImageController : Controller {

        private readonly ILogger<GameImageController> _Logger;
        private readonly IOptions<FileStorageOptions> _Options;
        private static readonly HttpClient _Http = new HttpClient();

        private const string BASE_URL = "https://api.bar-rts.com";

        static GameImageController() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex/0.1 (discord: varunda)");
        }

        public GameImageController(ILogger<GameImageController> logger,
            IOptions<FileStorageOptions> options) {

            _Logger = logger;
            _Options = options;
        }

        public async Task<IActionResult> MapBackground([FromQuery] string mapName, [FromQuery] string size) {

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
                if (response.StatusCode != System.Net.HttpStatusCode.OK) {
                    return StatusCode((int)response.StatusCode);
                }

                await System.IO.File.WriteAllBytesAsync(mapPath, await response.Content.ReadAsByteArrayAsync());
            }

            FileStream image = System.IO.File.OpenRead(mapPath);
            return File(image, "image/jpg", false);
        }


    }
}
