using gex.Common.Models.Options;
using gex.Models.Bar;
using gex.Services.Repositories;
using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers {

    /// <summary>
    ///     proxy to BAR images that are stored locally
    /// </summary>
    public class GameImageController : Controller {

        private readonly ILogger<GameImageController> _Logger;

        private readonly IOptions<FileStorageOptions> _Options;
        private readonly BarMapRepository _MapRepository;
        private readonly IMemoryCache _Cache;

        private const string CACHE_KEY_ICON_TYPES = "Gex.ImageProxy.IconTypes";
        private const string CACHE_KEY_PIC_MISSING = "Gex.ImageProxy.Pic.{0}"; // {0} => defName

        private static readonly HttpClient _Http = new HttpClient();

        private const string BASE_URL = "https://api.bar-rts.com";

        static GameImageController() {
            _Http.DefaultRequestHeaders.UserAgent.TryParseAdd("gex/0.1 (discord: varunda)");
        }

        public GameImageController(ILogger<GameImageController> logger,
            IOptions<FileStorageOptions> options, IMemoryCache cache,
            BarMapRepository mapRepository) {

            _Logger = logger;

            _Options = options;
            _MapRepository = mapRepository;
            _Cache = cache;
        }

        /// <summary>
        ///     get the background of a map
        /// </summary>
        /// <param name="mapName">name of the map</param>
        /// <param name="size">size of the map, examples: texture-mq, texture-hq, texture-thumb</param>
        /// <returns></returns>
        [ResponseCache(Duration = 60 * 60 * 24, VaryByQueryKeys = ["mapName", "size"])] // 24 hours
        public async Task<IActionResult> MapBackground([FromQuery] string mapName, [FromQuery] string size) {

            if (string.IsNullOrEmpty(mapName)) {
                return StatusCode(400, $"{nameof(mapName)} is empty or null");
            }

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
            return File(image, "image/jpeg", $"{mapName}.jpg", false);
        }

        [ResponseCache(Duration = 60 * 60 * 24, VaryByQueryKeys = ["map", "size"])] // 24 hours
        public async Task<IActionResult> MapNameBackground(
            [FromQuery] string map,
            [FromQuery] string size,
            CancellationToken cancel = default
        ) {

            BarMap? barMap = await _MapRepository.GetByName(map, cancel);
            if (barMap == null) {
                return StatusCode(404, $"failed to find map with name of '{map}'");
            }

            return await MapBackground(barMap.FileName, size);
        }

        /// <summary>
        ///     get unit icons, optionally coloring them to match <paramref name="color"/>
        /// </summary>
        /// <param name="defName">definition name of the unit</param>
        /// <param name="color">optional color to tint the icons with</param>
        /// <returns></returns>
        [ResponseCache(Duration = 60 * 60 * 24, VaryByQueryKeys = ["defName", "color"])] // 24 hours
        public async Task<IActionResult> UnitIcon([FromQuery] string defName, [FromQuery] int? color) {

            string iconDir = Path.Join(_Options.Value.WebImageLocation, "icons");
            Directory.CreateDirectory(iconDir);

            string storedName = defName;

            if (color != null) {
                storedName += $"_{color}";
            }

            string iconPath = Path.Join(iconDir, storedName + ".png");

            if (System.IO.File.Exists(iconPath) == false) {

                string uncoloredPath = Path.Join(iconDir, defName + ".png");
                byte[] data = [];
                if (System.IO.File.Exists(uncoloredPath)) {
                    _Logger.LogDebug($"have an uncolored icon, can use that one instead of fetching [defName={defName}] [color={color}]");
                    data = await System.IO.File.ReadAllBytesAsync(uncoloredPath);
                } else {

                    _Logger.LogDebug($"missing icon, downloading from GitHub [defName={defName}]");

                    if (_Cache.TryGetValue(CACHE_KEY_ICON_TYPES, out string? body) == false || body == null) {
                        _Logger.LogDebug($"icon types not cached, getting from GitHub");
                        string url = "https://raw.githubusercontent.com/beyond-all-reason/Beyond-All-Reason/refs/heads/master/gamedata/icontypes.lua";

                        HttpResponseMessage response = await _Http.GetAsync(url);
                        if (response.StatusCode != HttpStatusCode.OK) {
                            return StatusCode(500, $"expected 200 OK from {url}, got {response.StatusCode} instead");
                        }

                        body = await response.Content.ReadAsStringAsync();
                        _Cache.Set(CACHE_KEY_ICON_TYPES, body, new MemoryCacheEntryOptions() {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                        });
                    }

                    string[] units = body.Split("\n");
                    string? iconName = null;

                    for (int i = 0; i < units.Length; ++i) {
                        string unit = units[i];

                        // cormex and cormexp can get mixed up
                        if (unit.Trim().StartsWith(defName + " =") == false) {
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

                        data = await iconRes.Content.ReadAsByteArrayAsync();
                    } else {
                        _Logger.LogWarning($"failed to find iconName from icontypes.lua [defName={defName}]");
                        return StatusCode(404);
                    }
                }

                if (color != null) {
                    _Logger.LogTrace($"coloring icon [defName={defName}] [color={color}]");
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

                            // fix #41: copy pixel alpha to new image
                            bp.SetPixel(col, row, new SKColor(nr, ng, nb, pixel.Alpha));
                        }
                    }

                    SKData output = bp.Encode(SKEncodedImageFormat.Png, 100);
                    data = output.AsSpan().ToArray();
                }

                await System.IO.File.WriteAllBytesAsync(iconPath, data);
            }

            FileStream image = System.IO.File.OpenRead(iconPath);
            return File(image, "image/png", $"{defName}.png", false);
        }

        /// <summary>
        ///		get the picture of a unit, first locally saved, otherwise load it from the github
        /// </summary>
        /// <param name="defName">definition name of the unit</param>
        /// <returns></returns>
        [ResponseCache(Duration = 60 * 60 * 24, VaryByQueryKeys = ["defName"])] // 24 hours
        public async Task<IActionResult> UnitPic([FromQuery] string defName) {

            string cacheKey = string.Format(CACHE_KEY_PIC_MISSING, defName);
            if (_Cache.TryGetValue(cacheKey, out bool value) == true) {
                return StatusCode(404);
            }

            string picDir = Path.Join(_Options.Value.WebImageLocation, "pics");
            Directory.CreateDirectory(picDir);

            string storedName = defName;

            string picPath = Path.Join(picDir, storedName + ".jpg");

            if (System.IO.File.Exists(picPath) == false) {
                string url = $"https://raw.githubusercontent.com/beyond-all-reason/Beyond-All-Reason/refs/heads/master/unitpics/{defName}.dds";
                _Logger.LogDebug($"missing pic, downloading from GitHub [defName={defName}]");

                HttpResponseMessage response = await _Http.GetAsync(url);
                if (response.StatusCode == HttpStatusCode.NotFound) {
                    _Logger.LogInformation($"unit picture does not exist, caching 404 [defName={defName}]");
                    _Cache.Set(cacheKey, true, new MemoryCacheEntryOptions() {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8)
                    });
                    return StatusCode(404);
                }

                if (response.StatusCode != HttpStatusCode.OK) {
                    return StatusCode(500, $"expected 200 OK from {url}, got {response.StatusCode} instead");
                }

                using MagickImage mImage = new(response.Content.ReadAsStream());
                mImage.Strip();
                mImage.Format = MagickFormat.Jpg;

                using FileStream outputJpg = System.IO.File.OpenWrite(picPath);
                await mImage.WriteAsync(outputJpg);
            }

            FileStream image = System.IO.File.OpenRead(picPath);
            return File(image, "image/jpeg", $"{defName}.jpg", false);
        }

    }
}
