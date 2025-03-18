using gex.Models.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System.Data;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace gex.Controllers {

    /// <summary>
    ///     proxy to BAR images that are stored locally
    /// </summary>
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

        /// <summary>
        ///     get the background of a map
        /// </summary>
        /// <param name="mapName">name of the map</param>
        /// <param name="size">size of the map, examples: texture-mq, texture-hq, texture-thumb</param>
        /// <returns></returns>
        [ResponseCache(Duration = 60 * 60 * 24, VaryByQueryKeys = ["mapName", "size" ] )] // 24 hours
        public async Task<IActionResult> MapBackground([FromQuery] string mapName, [FromQuery] string size) {

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
                    return StatusCode((int)response.StatusCode);
                }

                await System.IO.File.WriteAllBytesAsync(mapPath, await response.Content.ReadAsByteArrayAsync());
            }

            FileStream image = System.IO.File.OpenRead(mapPath);
            return File(image, "image/jpg", false);
        }

        [ResponseCache(Duration = 60 * 60 * 24, VaryByQueryKeys = ["defName", "color"] )] // 24 hours
        public async Task<IActionResult> UnitIcon([FromQuery] string defName, [FromQuery] int? color) {

            string iconDir = Path.Join(_Options.Value.WebImageLocation, "icons");
            Directory.CreateDirectory(iconDir);

            string storedName = defName;

            if (color != null) {
                storedName += $"_{color}";
            }

            string iconPath = Path.Join(iconDir, storedName + ".png");

            if (System.IO.File.Exists(iconPath) == false) {

                string url = "https://raw.githubusercontent.com/beyond-all-reason/Beyond-All-Reason/refs/heads/master/gamedata/icontypes.lua";
                _Logger.LogDebug($"missing icon, downloading [defName={defName}]");

                HttpResponseMessage response = await _Http.GetAsync(url);
                if (response.StatusCode != HttpStatusCode.OK) {
                    return StatusCode(500, $"expected 200 OK from {url}, got {response.StatusCode} instead");
                }

                string? iconName = null;

                string[] units = (await response.Content.ReadAsStringAsync()).Split("\n");
                for (int i = 0; i < units.Length; ++i) {
                    string unit = units[i];

                    if (unit.Trim().StartsWith(defName) == false) {
                        continue;
                    }

                    string nextLine = units[i + 1].Trim();
                    _Logger.LogTrace($"found unit def [defName={defName}] [nextLine={nextLine}]");
                    Regex reg = new Regex(@"bitmap = ""icons/(.*).png""");
                    Match match = reg.Match(nextLine);
                    if (match.Success == false) {
                        return StatusCode(500, $"failed to match regex {nextLine}");
                    }

                    iconName = match.Groups[1].Value;
                    _Logger.LogTrace($"icon name found from regex [iconName={iconName}]");
                }

                if (iconName != null) {
                    HttpResponseMessage iconRes = await _Http.GetAsync(
                        $"https://raw.githubusercontent.com/beyond-all-reason/Beyond-All-Reason/refs/heads/master/icons/{iconName}.png");

                    if (iconRes.StatusCode != HttpStatusCode.OK) {
                        return StatusCode((int)iconRes.StatusCode);
                    }

                    byte[] data = await iconRes.Content.ReadAsByteArrayAsync();

                    if (color != null) {
                        byte r = (byte)((color >> 16) & 0xFF);
                        byte g = (byte)((color >> 8) & 0xFF);
                        byte b = (byte)((color >> 0) & 0xFF);

                        SKBitmap bp = SKBitmap.Decode(data);
                        SKColor[] pixels = bp.Pixels;
                        for (int col = 0; col < bp.Width; ++col) {
                            for (int row = 0; row < bp.Height; ++row) {
                                SKColor pixel = pixels[col + (row * bp.Width)];
                                byte nr = (byte)(r * (pixel.Red / (double)255));
                                byte ng = (byte)(g * (pixel.Red / (double)255));
                                byte nb = (byte)(b * (pixel.Red / (double)255));

                                bp.SetPixel(col, row, new SKColor(nr, ng, nb));
                            }
                        }

                        SKData output = bp.Encode(SKEncodedImageFormat.Png, 100);
                        data = output.AsSpan().ToArray();
                    }

                    await System.IO.File.WriteAllBytesAsync(iconPath, data);
                } else {
                    return StatusCode(404);
                }
            }

            FileStream image = System.IO.File.OpenRead(iconPath);
            return File(image, "image/png", false);
        }


    }
}
