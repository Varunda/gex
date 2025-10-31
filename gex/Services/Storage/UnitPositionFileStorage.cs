using gex.Common.Models;
using gex.Common.Models.Options;
using gex.Models.Event;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ZstdSharp;

namespace gex.Services.Storage {

    /// <summary>
    ///     saves the <see cref="GameEventUnitPosition"/> for a game and compresses it to a disk
    /// </summary>
    public class UnitPositionFileStorage {

        private readonly ILogger<UnitPositionFileStorage> _Logger;
        private readonly IOptions<FileStorageOptions> _FileOptions;

        public UnitPositionFileStorage(ILogger<UnitPositionFileStorage> logger,
            IOptions<FileStorageOptions> fileOptions) {

            _Logger = logger;
            _FileOptions = fileOptions;
        }

        public bool IsSaved(string gameID) {
            string path = Path.Join(_FileOptions.Value.UnitPositionLocation, gameID[..2], $"{gameID}.zstd");
            return File.Exists(path);
        }

        /// <summary>
        ///     save a list of <see cref="GameEventUnitPosition"/>s for a game and compress using zstd to disk
        /// </summary>
        /// <param name="gameID">ID of the game</param>
        /// <param name="pos">list of positions to save</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task SaveToDisk(string gameID, List<GameEventUnitPosition> pos, CancellationToken cancel) {
            if (string.IsNullOrEmpty(gameID)) {
                throw new Exception($"missing gameID to save unit position data");
            }

            string gamePrefixPath = Path.Join(_FileOptions.Value.UnitPositionLocation, gameID[..2]);
            if (Directory.Exists(gamePrefixPath) == false) {
                Directory.CreateDirectory(gamePrefixPath);
            }

            string outputPath = Path.Join(gamePrefixPath, $"{gameID}.zstd");
            if (File.Exists(outputPath)) {
                throw new Exception($"output file already exists [outputPath={outputPath}]");
            }

            // count: int32, units: UnitData[]
            // UnitData = unit_id: int32, count: int32, positions: Position[]
            // Position = frame: int32, x: float, y: float, z: float

            using MemoryStream ms = new();
            using BinaryWriter writer = new(ms);
            // a string will have a prefix byte for length, a char array does not
            writer.Write($"unit-position;version=1;game_id={gameID}\n".ToCharArray());

            Dictionary<long, List<GameEventUnitPosition>> dict = [];
            foreach (GameEventUnitPosition iter in pos) {
                long key = ((long)(iter.TeamID) << 32) | ((long)iter.UnitID);

                List<GameEventUnitPosition> unitPos = dict.GetValueOrDefault(key) ?? new List<GameEventUnitPosition>();
                unitPos.Add(iter);

                dict[key] = unitPos;

                cancel.ThrowIfCancellationRequested();
            }

            writer.Write(dict.Count);
            foreach (KeyValuePair<long, List<GameEventUnitPosition>> iter in dict) {
                int teamID = (int)(iter.Key >> 32);
                int unitID = (int)(iter.Key & 0xFFFF_FFFF);

                writer.Write(unitID);
                writer.Write(teamID);
                writer.Write(iter.Value.Count);

                foreach (GameEventUnitPosition p in iter.Value) {
                    writer.Write((int)p.Frame);
                    writer.Write(p.X);
                    writer.Write(p.Y);
                    writer.Write(p.Z);
                }

                cancel.ThrowIfCancellationRequested();
            }

            Stopwatch timer = Stopwatch.StartNew();
            using FileStream output = File.OpenWrite(outputPath);

            using Compressor comp = new(22);
            ms.Position = 0;
            Span<byte> comped = comp.Wrap(ms.ToArray());
            output.Write(comped);

            using CompressionStream cs = new(output, 22);
            // do not allow a cancellation token to stop the compression while it takes place
            // UnitData[]
            // UnitData = unit_id: int32, team_id: int32, count: int32, Position[]
            // Position = frame: int32, x: float, y: float, z: float
            long compressMs = timer.ElapsedMilliseconds;

            _Logger.LogInformation($"created compressed unit position file [outputPath={outputPath}] [timer={compressMs}ms]");
            return Task.CompletedTask;
        }

        public async Task<Result<List<GameEventUnitPosition>, string>> GetByGameID(string gameID, CancellationToken cancel) {
            if (string.IsNullOrEmpty(gameID)) {
                throw new Exception($"missing gameID to save unit position data");
            }

            string outputPath = Path.Join(_FileOptions.Value.UnitPositionLocation, gameID.Substring(0, 2), $"{gameID}.zstd");
            if (File.Exists(outputPath) == false) {
                _Logger.LogWarning($"cannot read unit position data, file does not exist [outputPath={outputPath}]");
                return $"missing file [outputPath={outputPath}]";
            }

            using MemoryStream ms = new();
            using FileStream input = File.OpenRead(outputPath);
            using DecompressionStream ds = new(input);
            await ds.CopyToAsync(ms, cancel);

            ms.Position = 0;
            using BinaryReader reader = new(ms);

            // "unit-position;version=1;game_id={gameID}\n";
            string header = "";
            while (true) {
                char c = (char)reader.ReadByte();
                if (c == '\n') {
                    break;
                }

                header += c;

                if (header.Length > (34 + 40)) {
                    _Logger.LogWarning($"header is getting too long, expecting a max of 74 bytes");
                    break;
                }
            }

            string[] parts = header.Split(";");
            if (parts.Length != 3) {
                return $"bad header, expected 3 parts [parts.Length={parts.Length}] [header={header}]";
            }

            if (parts[0] != "unit-position") {
                return $"bad header, expected 'unit-position' in first part [first part={parts[0]}] [header={header}]";
            }

            if (parts[1].StartsWith("version=") == false) {
                return $"bad header, expected second part to start with 'version=' [second part={parts[1]}] [header={header}]";
            }

            string[] versionParts = parts[1].Split("=");
            if (versionParts.Length != 2) {
                return $"bad header, version part was bad [version={parts[1]}] [header={header}]";
            }

            if (versionParts[0] != "version") {
                throw new Exception($"bad state, already checked if parts[1] started with 'version'");
            }

            if (int.TryParse(versionParts[1], out int version) == false) {
                return $"bad header, failed to parse version part 2 into a valid int32 [version part 2={versionParts[1]}] [header={header}] [gameID={gameID}]";
            }

            string[] gameIDParts = parts[2].Split("=");
            if (gameIDParts.Length != 2) {
                return $"bad header, game ID part was wrong length [gameID={parts[2]}] [parts.Count={gameIDParts.Length}] [header={header}]";
            }
            string fileGameID = gameIDParts[1];
            if (gameID != fileGameID) {
                return $"wrong header, mismatched game ID [gameID={gameID}] [fileGameID={fileGameID}]";
            }

            _Logger.LogDebug($"header read done [gameID={fileGameID}] [version={version}]");

            if (version == 1) {
                try {
                    return await ParseVersion1(reader, fileGameID, cancel);
                } catch (Exception ex) {
                    _Logger.LogError(ex, $"failed to decode unit position data [gameID={gameID}]");
                    return $"failed to parse unit position data [error={ex.Message}]";
                }
            } else {
                return $"unhandled unit position file version [version={version}]";
            }
        }

        /// <summary>
        ///     remove the zstd file for a game ID
        /// </summary>
        /// <param name="gameID">ID of the game to remove the zstd file of unit positions for</param>
        /// <returns></returns>
        public bool RemoveByGameID(string gameID) {
            string path = Path.Join(_FileOptions.Value.UnitPositionLocation, gameID[..2], $"{gameID}.zstd");
            if (File.Exists(path)) {
                File.Delete(path);
            }

            return true;
        }

        private Task<List<GameEventUnitPosition>> ParseVersion1(BinaryReader reader, string gameID, CancellationToken cancel) {
            // count: int32, units: UnitData[]
            // UnitData = unit_id: int32, team_id: int32, count: int32, positions: Position[]
            // Position = frame: int32, x: float, y: float, z: float

            List<GameEventUnitPosition> positions = [];

            int unitCount = reader.ReadInt32();
            //_Logger.LogDebug($"got unit count [unitCount={unitCount}]");

            for (int i = 0; i < unitCount; ++i) {
                int unitID = reader.ReadInt32();
                int teamID = reader.ReadInt32();
                int positionCount = reader.ReadInt32();

                //_Logger.LogTrace($"got unit [unitID={unitID}] [teamID={teamID}] [positionCount={positionCount}]");

                for (int j = 0; j < positionCount; ++j) {
                    int frame = reader.ReadInt32();
                    double x = reader.ReadDouble();
                    double y = reader.ReadDouble();
                    double z = reader.ReadDouble();

                    GameEventUnitPosition pos = new();
                    pos.GameID = gameID;
                    pos.Action = "unit_position";
                    pos.UnitID = unitID;
                    pos.TeamID = teamID;
                    pos.Frame = frame;
                    pos.X = x;
                    pos.Y = y;
                    pos.Z = z;
                    positions.Add(pos);
                }
            }

            return Task.FromResult(positions.OrderBy(iter => iter.Frame).ToList());
        }

    }
}
