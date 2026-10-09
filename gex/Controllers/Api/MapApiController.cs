using gex.Code;
using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Options;
using gex.Common.Services.Parser;
using gex.Common.Services.Repository;
using gex.Common.Services.Repository.Match;
using gex.Models;
using gex.Models.Internal;
using gex.Services.Db.Map;
using gex.Services.Migrations;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/map")]
    public class MapApiController : ApiControllerBase {

        private readonly ILogger<MapApiController> _Logger;
        private readonly BarMapRepository _MapRepository;
        private readonly StartSpotDataRepository _StartSpotDataRepository;
        private readonly BarMatchTeamRepository _TeamRepository;
        private readonly BarMatchPlayerStartSpotMigration _PlayerStartSpotMigration;
        private readonly IOptions<FileStorageOptions> _StorageOptions;
        private readonly BarMapParser _MapParser;

        public MapApiController(ILogger<MapApiController> logger,
            BarMapRepository mapRepository, StartSpotDataRepository startSpotDataRepository,
            BarMatchPlayerStartSpotMigration playerStartSpotMigration, BarMatchTeamRepository teamRepository,
            IOptions<FileStorageOptions> storageOptions, BarMapParser mapParser) {

            _Logger = logger;
            _MapRepository = mapRepository;
            _StartSpotDataRepository = startSpotDataRepository;
            _PlayerStartSpotMigration = playerStartSpotMigration;
            _TeamRepository = teamRepository;
            _StorageOptions = storageOptions;
            _MapParser = mapParser;
        }

        /// <summary>
        ///		get a <see cref="BarMap"/> by its <see cref="BarMap.FileName"/>
        /// </summary>
        /// <param name="filename">filename of the map to get</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///		the response will contain the <see cref="BarMap"/> with the <see cref="BarMap.FileName"/>
        ///		of <paramref name="filename"/>
        /// </response>
        /// <response code="204">
        ///		no <see cref="BarMap"/> with <see cref="BarMap.FileName"/> of <paramref name="filename"/> exists
        /// </response>
        [HttpGet("{filename}")]
        public async Task<ApiResponse<BarMap>> Get(string filename,
            CancellationToken cancel = default) {

            BarMap? map = await _MapRepository.GetByFileName(filename, cancel);
            if (map == null) {
                return ApiNoContent<BarMap>();
            }

            map.StartPositionData = await _StartSpotDataRepository.GetLatestByMapFilename(filename, cancel);

            return ApiOk(map);
        }

        /// <summary>
        ///     api method to get all maps that Gex knows about
        /// </summary>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain a list of <see cref="BarMap"/>s
        /// </response>
        [HttpGet("all")]
        public async Task<ApiResponse<List<BarMap>>> GetAll(CancellationToken cancel) {
            List<BarMap> maps = await _MapRepository.GetAll(cancel);
            return ApiOk(maps);
        }

        /// <summary>
        ///     get the <see cref="BarMapData"/> for a specific map, by loading it from the file system
        /// </summary>
        /// <param name="mapFilename">filename of the map (without the .sd7)</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the response will contain the <see cref="BarMapData"/> for the <see cref="BarMapData.FileName"/>
        ///     of <paramref name="mapFilename"/>
        /// </response>
        /// <response code="404">
        ///     no file matching <paramref name="mapFilename"/> was found
        /// </response>
        /// <response code="500">
        ///     the map parser failed in some way
        /// </response>
        [HttpGet("{mapFilename}/data")]
        public async Task<ApiResponse<BarMapData>> GetMapData(string mapFilename,
            CancellationToken cancel = default) {

            string mapPath = Path.Join(_StorageOptions.Value.MapLocation, "maps", (mapFilename + ".sd7").EscapeRecoilFilesytemCharacters());
            if (System.IO.File.Exists(mapPath) == false) {
                return ApiNotFound<BarMapData>($"{nameof(BarMap)} {mapFilename}");
            }

            Result<BarMapData, string> result = await _MapParser.Parse(mapPath, new BarMapParser.ParseOptions() {
                HeightMap = true,
                Header = true,
                Smts = false,
            }, cancel);
            if (result.IsOk == false) {
                _Logger.LogWarning($"failed to parse map [mapFilename={mapFilename}] [error={result.Error}]");
                return ApiInternalError<BarMapData>($"failed to parse map: {result.Error}");
            }

            return ApiOk(result.Value);
        }

        /// <summary>
        ///     get the height map of a map
        /// </summary>
        /// <param name="mapFilename"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpGet("{mapFilename}/height-map")]
        public async Task<IActionResult> GetHeightMap(string mapFilename, CancellationToken cancel = default) {
            Directory.CreateDirectory(Path.Join(_StorageOptions.Value.MapLocation, "height-map"));

            string texturePath = Path.Join(_StorageOptions.Value.MapLocation, "height-map", $"{mapFilename}.png");
            if (System.IO.File.Exists(texturePath)) {
                return File(System.IO.File.OpenRead(texturePath), "image/png");
            }

            string mapPath = Path.Join(_StorageOptions.Value.MapLocation, "maps", (mapFilename + ".sd7").EscapeRecoilFilesytemCharacters());
            if (System.IO.File.Exists(mapPath) == false) {
                return NotFound($"{nameof(BarMap)} {mapFilename}");
            }

            _Logger.LogInformation($"missing texture map for map [mapFilename={mapFilename}]");
            Stopwatch timer = Stopwatch.StartNew();

            Result<BarMapData, string> result = await _MapParser.Parse(mapPath, new BarMapParser.ParseOptions() {
                HeightMap = true,
                Header = true,
                Smts = false,
            }, cancel);

            if (result.IsOk == false) {
                return ApiInternalError($"");
            }

            BarMapData data = result.Value;
            SKBitmap bitmap = new(data.Header.Width + 1, data.Header.Height + 1);
            for (int i = 0; i < data.Header.HeightMap.Length; ++i) {
                int col = i % (data.Header.Width + 1);
                int row = i / (data.Header.Width + 1);

                ushort h = data.Header.HeightMap[i];

                float percent = h / 65536f;
                bitmap.SetPixel(col, row, new SKColor(
                    red: (byte)(percent * 255),
                    green: (byte)(percent * 255),
                    blue: (byte)(percent * 255)
                ));
            }

            long parseMs = timer.ElapsedMilliseconds; timer.Restart();

            SKData png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            await System.IO.File.WriteAllBytesAsync(texturePath, png.ToArray(), CancellationToken.None);
            long saveMs = timer.ElapsedMilliseconds;
            _Logger.LogInformation($"successfully saved map height map [mapFilename={mapFilename}] [parse={parseMs}ms] [save={saveMs}ms]");

            return File(System.IO.File.OpenRead(texturePath), "image/png");
        }

        /// <summary>
        ///     get the texture image of a map
        /// </summary>
        /// <param name="mapFilename"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        [HttpGet("{mapFilename}/texture")]
        public async Task<IActionResult> GetMapTexture(string mapFilename, CancellationToken cancel = default) {
            Directory.CreateDirectory(Path.Join(_StorageOptions.Value.MapLocation, "texture"));

            string texturePath = Path.Join(_StorageOptions.Value.MapLocation, "texture", $"{mapFilename}.png");
            if (System.IO.File.Exists(texturePath)) {
                return File(System.IO.File.OpenRead(texturePath), "image/png");
            }

            string mapPath = Path.Join(_StorageOptions.Value.MapLocation, "maps", (mapFilename + ".sd7").EscapeRecoilFilesytemCharacters());
            if (System.IO.File.Exists(mapPath) == false) {
                return NotFound($"{nameof(BarMap)} {mapFilename}");
            }

            _Logger.LogInformation($"missing texture map for map [mapFilename={mapFilename}]");
            Stopwatch timer = Stopwatch.StartNew();

            Result<BarMapData, string> result = await _MapParser.Parse(mapPath, new BarMapParser.ParseOptions() {
                HeightMap = false,
                Header = true,
                Smts = true,
            }, cancel);

            if (result.IsOk == false) {
                return ApiInternalError($"");
            }

            if (result.Value.Smt is null) {
                throw new InvalidOperationException($"Smt cannot be null");
            }

            long parseMs = timer.ElapsedMilliseconds; timer.Restart();

            SKData png = result.Value.Smt.Bitmap.Encode(SKEncodedImageFormat.Png, 100);
            await System.IO.File.WriteAllBytesAsync(texturePath, png.ToArray(), CancellationToken.None);
            long saveMs = timer.ElapsedMilliseconds;
            _Logger.LogInformation($"successfully saved map texture image [mapFilename={mapFilename}] [parse={parseMs}ms] [save={saveMs}ms]");

            return File(texturePath, "image/png");
        }

        /// <summary>
        ///     upsert a <see cref="StartSpotSideStartRoleOverride"/>
        /// </summary>
        /// <param name="mapFilename"><see cref="BarMap.FileName"/> to create the override for</param>
        /// <param name="version">version of the <see cref="StartSpotData"/> to create the override for</param>
        /// <param name="position">position to update the role for</param>
        /// <param name="role">role override</param>
        /// <param name="maxRadius">max radius away from the position that can be used</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the <see cref="StartSpotSideStartRoleOverride"/> created
        /// </response>
        /// <response code="404">
        ///     no start spot data exists for the map and version combo given
        /// </response>
        [HttpPost("start-spot-position-role-override")]
        [PermissionNeeded(AppPermission.GEX_MAP_START_SPOT_EDITOR)]
        public async Task<ApiResponse<StartSpotSideStartRoleOverride>> UpdateStartSpotPositionRoleOverrides(
            [FromQuery] string mapFilename,
            [FromQuery] int version,
            [FromQuery] string position,
            [FromQuery] string role,
            [FromQuery] float? maxRadius,
            CancellationToken cancel = default
        ) {

            StartSpotData? data = await _StartSpotDataRepository.GetByVersionAndMapFilename(mapFilename, version, cancel);
            if (data == null) {
                return ApiNotFound<StartSpotSideStartRoleOverride>($"{nameof(StartSpotData)} {mapFilename} {version}");
            }

            StartSpotSideStartRoleOverride @override = new() {
                MapFilename = mapFilename,
                Version = version,
                Position = position,
                Role = role,
                MaxRadius = maxRadius
            };

            await _StartSpotDataRepository.UpsertStartSpotPositionRoleOverride(@override, cancel);
            _Logger.LogInformation($"created override for position [map={mapFilename}] [version={version}] [position={position}] [role={role}]");

            Stopwatch timer = Stopwatch.StartNew();
            await _TeamRepository.UpdateStartSpotRole(@override, cancel);
            _Logger.LogInformation($"updated start spot role names [mapFilename={mapFilename}] [version={version}] "
                + $"[position={position}] [role={role}] [timer={timer.ElapsedMilliseconds}ms]");

            return ApiOk(@override);
        }

        /// <summary>
        ///     recalculate player start spots for a map
        /// </summary>
        /// <param name="mapFilename">name of the map</param>
        /// <param name="cancel">cancellation token</param>
        /// <response code="200">
        ///     the stats for the map were recalculated
        /// </response>
        [HttpPost("{mapFilename}/recalculate-player-start-spots")]
        [Authorize]
        [PermissionNeeded(AppPermission.GEX_DEV)]
        public async Task<ApiResponse> RecalculatePlayerStartSpots(string mapFilename, CancellationToken cancel) {
            BarMap? map = await _MapRepository.GetByFileName(mapFilename, cancel);
            if (map == null) {
                return ApiNotFound($"{nameof(BarMap)} {mapFilename}");
            }

            _Logger.LogDebug($"fixing start spot [map={mapFilename}]");
            await _PlayerStartSpotMigration.FixMap(map, cancel);
            _Logger.LogInformation($"fixed start spots for map [map={mapFilename}]");

            return ApiOk();
        }

    }
}
