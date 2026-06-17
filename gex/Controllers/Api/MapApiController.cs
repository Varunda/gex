using gex.Code;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Internal;
using gex.Models.Map;
using gex.Services.Db.Map;
using gex.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Controllers.Api {

    [ApiController]
    [Route("/api/map")]
    public class MapApiController : ApiControllerBase {

        private readonly ILogger<MapApiController> _Logger;
        private readonly BarMapRepository _MapRepository;
        private readonly StartSpotDataRepository _StartSpotDataRepository;
        private readonly StartSpotSideStartRoleOverrideDb _OverrideDb;
        private readonly BarMatchPlayerRepository _MatchPlayerRepository;

        public MapApiController(ILogger<MapApiController> logger,
            BarMapRepository mapRepository, StartSpotDataRepository startSpotDataRepository,
            StartSpotSideStartRoleOverrideDb overrideDb, BarMatchPlayerRepository matchPlayerRepository) {

            _Logger = logger;
            _MapRepository = mapRepository;
            _StartSpotDataRepository = startSpotDataRepository;
            _OverrideDb = overrideDb;
            _MatchPlayerRepository = matchPlayerRepository;
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
            CancellationToken cancel) {

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
            await _MatchPlayerRepository.UpdateStartSpotRole(@override, cancel);
            _Logger.LogInformation($"updated start spot role names [mapFilename={mapFilename}] [version={version}] "
                + $"[position={position}] [role={role}] [timer={timer.ElapsedMilliseconds}ms]");

            return ApiOk(@override);
        }

    }
}
