using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Models.Bar;
using gex.Services.Repositories;
using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
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
        private readonly MapImageRepository _MapImageRepository;
        private readonly BarMatchPlayerRepository _PlayerRepository;
        private readonly BarIconTypeRepository _IconTypeRepository;
        private readonly IGithubDownloadRepository _GithubDownloader;
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
            BarMapRepository mapRepository, MapImageRepository mapImageRepository,
            BarIconTypeRepository iconTypeRepository, IGithubDownloadRepository githubDownloader,
            BarMatchPlayerRepository playerRepository) {

            _Logger = logger;

            _Options = options;
            _MapRepository = mapRepository;
            _Cache = cache;
            _MapImageRepository = mapImageRepository;
            _IconTypeRepository = iconTypeRepository;
            _GithubDownloader = githubDownloader;
            _PlayerRepository = playerRepository;
        }

        /// <summary>
        ///     get the background of a map by <see cref="BarMap.FileName"/>
        /// </summary>
        /// <param name="mapName">file name of the map</param>
        /// <param name="size">size of the map, examples: texture-mq, texture-hq, texture-thumb</param>
        /// <returns></returns>
        [ResponseCache(Duration = 60 * 60 * 24, VaryByQueryKeys = ["mapName", "size"])] // 24 hours
        public async Task<IActionResult> MapBackground([FromQuery] string mapName, [FromQuery] string size) {

            if (string.IsNullOrEmpty(mapName)) {
                return StatusCode(400, $"{nameof(mapName)} is empty or null");
            }

            Result<string, string> mapPath = await _MapImageRepository.GetMapPath(mapName, size);

            if (mapPath.IsOk == false) {
                return StatusCode(500, $"failed to load map image: {mapPath.Error}]");
            }

            FileStream image = System.IO.File.OpenRead(mapPath.Value);
            return File(image, "image/jpeg", $"{mapName}.jpg", false);
        }

        /// <summary>
        ///     get the background image of a map by <see cref="BarMap.Name"/>
        /// </summary>
        /// <param name="map">name of the map</param>
        /// <param name="size">size of the map, examples: texture-mq, texture-hq, texture-thumb</param>
        /// <param name="cancel"></param>
        /// <returns></returns>
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
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        [ResponseCache(Duration = 60 * 60 * 24, VaryByQueryKeys = ["defName", "color"])] // 24 hours
        public async Task<IActionResult> UnitIcon(
            [FromQuery] string defName, [FromQuery] int? color,
            CancellationToken cancel = default
        ) {

            if (color != null && (await _PlayerRepository.GetUniqueColors(cancel)).Contains(color.Value) == false) {
                color = null;
            }

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
                    string overridePath = Path.Join(Environment.CurrentDirectory, "wwwroot", "img", "unit_icon_override");
                    string overrideIcon = Path.Join(overridePath, $"{defName}.png");

                    _Logger.LogDebug($"missing icon, checking if override exists or downloading from GitHub [defName={defName}] [overrideIcon={overrideIcon}]");

                    if (System.IO.File.Exists(overrideIcon) == true) {
                        _Logger.LogTrace($"unit icon for definition has override [defName={defName}] [path={overridePath}]");
                        data = await System.IO.File.ReadAllBytesAsync(overrideIcon);
                    } else {

                        // no override, get from icon type repo (GitHub)
                        Result<string?, string> iconType = await _IconTypeRepository.GetUnitDefIcon(defName, cancel);
                        if (iconType.IsOk == false) {
                            return StatusCode(500, $"failed to load iconType from repository");
                        }

                        string? iconName = iconType.Value;

                        if (iconName != null) {
                            HttpResponseMessage iconRes = await _Http.GetAsync(
                                $"https://raw.githubusercontent.com/beyond-all-reason/Beyond-All-Reason/refs/heads/master/icons/{iconName}");

                            if (iconRes.StatusCode != HttpStatusCode.OK) {
                                _Logger.LogWarning($"failed to load iconType from GitHub [iconName={iconName}] [status={iconRes.StatusCode}]");
                                return StatusCode((int)iconRes.StatusCode);
                            }

                            data = await iconRes.Content.ReadAsByteArrayAsync();
                        } else {
                            _Logger.LogWarning($"failed to find iconName from icontypes.lua [defName={defName}]");
                            return StatusCode(404);
                        }
                    }
                }

                if (color != null && color != 0) {
                    _Logger.LogTrace($"coloring icon [defName={defName}] [color={color}]");
                    data = _RecolorPng(data, color.Value);
                }

                _Logger.LogTrace($"saving icon [defName={defName}] [color={color}] [path={iconPath}]");
                await System.IO.File.WriteAllBytesAsync(iconPath, data);
            }

            byte[] img = await System.IO.File.ReadAllBytesAsync(iconPath, cancel);
            byte[] md5 = MD5.HashData(img);
            string etag = string.Join("", md5.Select(iter => iter.ToString("x2")));

            return File(
                img, "image/png", $"{defName}.png", lastModified: null,
                entityTag: new EntityTagHeaderValue($"\"{etag}\""), enableRangeProcessing: false
            );
        }

        /// <summary>
        ///     get unit icons as an atlas, optionally coloring them
        /// </summary>
        /// <param name="color">optional color to tint the icons with</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        [ResponseCache(Duration = 60 * 60 * 24, VaryByQueryKeys = ["color"])] // 24 hours
        public async Task<IActionResult> UnitIconAtlas([FromQuery] int? color, CancellationToken cancel = default) {
            if (color != null && (await _PlayerRepository.GetUniqueColors(cancel)).Contains(color.Value) == false) {
                color = null;
            }

            string iconDir = Path.Join(_Options.Value.WebImageLocation, "icons");
            Directory.CreateDirectory(iconDir);

            string storedName = "atlas";

            if (color != null && color.Value != 0) {
                storedName += $"_{color}";
            }

            string atlasPath = Path.Join(iconDir, storedName + ".png");

            bool needsCreation = System.IO.File.Exists(atlasPath) == false;

            if (needsCreation == false) {
                FileInfo atlasInfo = new(atlasPath);
                needsCreation = (DateTime.UtcNow - atlasInfo.LastWriteTimeUtc) > TimeSpan.FromHours(24);
                if (needsCreation == true) {
                    _Logger.LogDebug($"atlas is too old, needs recreation [creationTime={atlasInfo.CreationTimeUtc:u}] [path={atlasPath}]");
                }
            }

            if (needsCreation == true) {
                _Logger.LogDebug($"missing atlas or json [color={color}]");
                string uncoloredPath = Path.Join(iconDir, "atlas.png");
                byte[] data = [];
                if (System.IO.File.Exists(uncoloredPath) == true) {
                    _Logger.LogDebug($"have an uncolored atlas, can use that one instead of fetching [color={color}]");
                    data = await System.IO.File.ReadAllBytesAsync(uncoloredPath, cancel);
                } else {
                    _Logger.LogDebug($"creating icon atlas");
                    await _CreateAtlasAndJson(cancel);
                    data = await System.IO.File.ReadAllBytesAsync(uncoloredPath, cancel);
                }

                if (color != null && color != 0) {
                    _Logger.LogTrace($"coloring atlas [color={color}]");
                    Stopwatch timer = Stopwatch.StartNew();
                    data = _RecolorPng(data, color.Value);
                    _Logger.LogTrace($"colored atlas [color={color}] [timer={timer.ElapsedMilliseconds}ms]");
                }

                _Logger.LogTrace($"saving colored atlas [color={color}] [path={atlasPath}]");
                Stopwatch timer2 = Stopwatch.StartNew();
                await System.IO.File.WriteAllBytesAsync(atlasPath, data, CancellationToken.None);
                _Logger.LogTrace($"saved colored atlas [color={color}] [path={atlasPath}] [timer={timer2.ElapsedMilliseconds}ms]");
                Debug.Assert(System.IO.File.Exists(atlasPath), $"missing atlasPath {atlasPath}");
            }

            byte[] img = await System.IO.File.ReadAllBytesAsync(atlasPath, cancel);
            byte[] md5 = MD5.HashData(img);
            string etag = string.Join("", md5.Select(iter => iter.ToString("x2")));

            return File(
                img, "image/png", $"{storedName}.png", lastModified: null,
                entityTag: new EntityTagHeaderValue($"\"{etag}\""), enableRangeProcessing: false
            );
        }

        /// <summary>
        ///     get the JSON of the unit icon atlas, which contains each unit definition name, and the offset (in pixels)
        ///     where the icon for that icon is found. these are 32x32 pixel images
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     a JSON containing each unit definition name, and a pixel offset into the atlas
        ///     (which can be found at <see cref="UnitIconAtlas(int?, CancellationToken)"/>)
        ///     where the corresponding unit icon can be found
        /// </response>
        /// <response code="500">
        ///     the json was not found after being created. this indicates an internal error
        /// </response>
        [ResponseCache(Duration = 60 * 60 * 24)] // 24 hours
        public async Task<IActionResult> UnitIconAtlasJson(CancellationToken cancel = default) {
            string path = Path.Join(_Options.Value.WebImageLocation, "icons", "atlas.json");
            if (System.IO.File.Exists(path) == false) {
                await _CreateAtlasAndJson(cancel);
            }

            if (System.IO.File.Exists(path) == false) {
                return StatusCode(500, $"failed to find atlas.json after calling create");
            }

            FileStream fs = System.IO.File.OpenRead(path);
            return File(fs, "application/json");
        }

        /// <summary>
        ///     get the CSS of the unit icon atlas
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpGet("image-proxy/UnitIconAtlas.css")]
        //[ResponseCache(Duration = 60 * 60 * 24)] // 24 hours
        public async Task<IActionResult> UnitIconAtlasCss(CancellationToken cancel = default) {
            string path = Path.Join(_Options.Value.WebImageLocation, "icons", "atlas.css");
            if (System.IO.File.Exists(path) == false) {
                await _CreateAtlasAndJson(cancel);
            }

            if (System.IO.File.Exists(path) == false) {
                return StatusCode(500, $"failed to find atlas.css after calling create");
            }

            byte[] css = await System.IO.File.ReadAllBytesAsync(path, cancel);
            byte[] md5 = MD5.HashData(css);
            string etag = string.Join("", md5.Select(iter => iter.ToString("x2")));

            return File(css, "text/css", lastModified: null, entityTag: new EntityTagHeaderValue($"\"{etag}\""));
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

            // using is not needed here, |File()| will dispose of it
            FileStream image = System.IO.File.OpenRead(picPath);
            return File(image, "image/jpeg", $"{defName}.jpg", false);
        }

        /// <summary>
        ///     create the icon atlas and corresponding json at the same time
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<Result<bool, string>> _CreateAtlasAndJson(CancellationToken cancel) {
            const uint SIZE = 32;

            string iconDir = Path.Join(_Options.Value.WebImageLocation, "icons");
            Directory.CreateDirectory(iconDir);

            string uncoloredPath = Path.Join(iconDir, "atlas.png");
            string jsonPath = Path.Join(iconDir, "atlas.json");
            string cssPath = Path.Join(iconDir, "atlas.css");

            Result<Dictionary<string, string>, string> dictRet = await _IconTypeRepository.GetAll(cancel);
            if (dictRet.IsOk == false) {
                return $"failed to load icon types: {dictRet.Error}";
            }

            Dictionary<string, string> dict = dictRet.Value;

            if (_GithubDownloader.HasFile("icons", "blank.png") == false) {
                _Logger.LogInformation($"missing icons from github, downloading (missing blank.png in icons folder)");
                await _GithubDownloader.DownloadFolder("icons", false, false, cancel);
            }

            if (_GithubDownloader.HasFile("icons", "blank.png") == false) {
                throw new Exception($"failed to find icons/blank.png after downloading the folder");
            }

            List<string> unitDefs = dict.Keys.OrderBy(iter => iter).ToList();

            // provides a --size and --url variable that can be used to change the size in pixels
            //      and the url of the image atlas (used to load an atlas of different colors)
            string css = $@"
                .bui {{
                    --size: {SIZE};
                    --url: url(""/image-proxy/UnitIconAtlas"");
                    width: calc(var(--size) * 1px);
                    height: calc(var(--size) * 1px);
                    display: inline-block;
                    background-image: var(--url);
                    background-size: calc(var(--size) / {SIZE} * {(unitDefs.Count + 1) * SIZE} * 1px) calc(var(--size) * 1px);
                    background-repeat: no-repeat;
                }}
            ";

            string basePath = Path.Join(_Options.Value.GitHubDataLocation, "icons");
            string overridePath = Path.Join(Environment.CurrentDirectory, "wwwroot", "img", "unit_icon_override");

            JsonObject json = JsonSerializer.Deserialize<JsonObject>("{}")!;
            json.Add("timestamp", DateTime.UtcNow.ToString("u"));

            JsonObject data = JsonSerializer.Deserialize<JsonObject>("{}")!;
            json.Add("data", data);

            using MagickImageCollection atlas = [new MagickImage(Path.Join(basePath, "blank.png"))];

            for (int i = 0; i < unitDefs.Count; ++ i) {
                string unitDef = unitDefs[i];
                data.Add(unitDef, (i + 1) * SIZE); // add one for the blank icon added

                css += @$"
                    .bui-{unitDef} {{
                        background-position: calc(({(i + 1) * SIZE} * var(--size) / {SIZE}) * -1px) 0px;
                    }}
                ";

                string overrideIcon = Path.Join(overridePath, $"{unitDef}.png");
                if (System.IO.File.Exists(overrideIcon) == true) {
                    MagickImage img = new(overrideIcon);
                    img.Resize(SIZE, SIZE);
                    atlas.Add(new MagickImage(overrideIcon));
                } else {
                    string iconType = dict.GetValueOrDefault(unitDef)!;
                    MagickImage img = new(Path.Join(basePath, iconType));
                    img.Resize(SIZE, SIZE);
                    atlas.Add(img);
                }
            }

            using IMagickImage<ushort> atlasOutput = atlas.Montage(new MontageSettings() {
                Geometry = new MagickGeometry(SIZE, SIZE),
                BackgroundColor = MagickColors.None,
                TileGeometry = new MagickGeometry(0, 1)
            });

            await atlasOutput.WriteAsync(uncoloredPath, CancellationToken.None);
            await System.IO.File.WriteAllTextAsync(jsonPath, json.ToJsonString(), CancellationToken.None);
            await System.IO.File.WriteAllTextAsync(cssPath, css, CancellationToken.None);

            return true;
        }

        /// <summary>
        ///     recolor a bytes of a PNG file to another color
        /// </summary>
        /// <param name="data">PNG data</param>
        /// <param name="color">int32 color to recolor</param>
        /// <returns>a recolored PNG</returns>
        private byte[] _RecolorPng(byte[] data, int color) {
            byte r = (byte)((color >> 16) & 0xFF);
            byte g = (byte)((color >> 8) & 0xFF);
            byte b = (byte)((color >> 0) & 0xFF);

            Dictionary<int, SKColor> cache = [];

            SKBitmap bp = SKBitmap.Decode(data);
            SKColor[] pixels = bp.Pixels;

            int w = bp.Width;
            int h = bp.Height;

            for (int col = 0; col < w; ++col) {
                for (int row = 0; row < h; ++row) {
                    SKColor pixel = pixels[col + (row * w)];
                    // they're black and white images, doesn't matter what channel is used
                    byte nr = (byte)(r * (pixel.Red / (double)255));
                    byte ng = (byte)(g * (pixel.Red / (double)255));
                    byte nb = (byte)(b * (pixel.Red / (double)255));

                    int key = (nr << 24) | (nr << 16) | (nb << 8) | (pixel.Alpha << 0);

                    SKColor c;
                    if (cache.ContainsKey(key) == true) {
                        c = cache[key];
                    } else {
                        // fix #41: copy pixel alpha to new image
                        c = new SKColor(nr, ng, nb, pixel.Alpha);
                        cache.Add(key, c);
                    }

                    pixels[col + (row * w)] = c;
                }
            }

            bp.Pixels = pixels;

            SKData output = bp.Encode(SKEncodedImageFormat.Png, 100);
            return output.AsSpan().ToArray();
        }

    }
}
