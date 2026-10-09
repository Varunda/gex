using gex.Common.Code;
using gex.Common.Models;
using gex.Common.Models.Map;
using gex.Common.Models.Options;
using gex.Common.Services;
using ImageMagick;
using ImageMagick.Formats;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pfim;
using SevenZip;
using SevenZip.Extensions;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Common.Services.Parser {

    public class BarMapParser {

        private readonly ILogger<BarMapParser> _Logger;

        private readonly LuaRunner _LuaRunner;

        public BarMapParser(ILogger<BarMapParser> logger,
            LuaRunner luaRunner) {

            _Logger = logger;
            _LuaRunner = luaRunner;
        }

        public Task<Result<BarMapData, string>> Parse(string location, CancellationToken cancel) {
            return Parse(location, new ParseOptions(), cancel);
        }

        /// <summary>
        ///		parse a .sd7 map at the location given
        /// </summary>
        /// <param name="location"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result<BarMapData, string>> Parse(string location, ParseOptions options, CancellationToken cancel) {
            Stopwatch timer = Stopwatch.StartNew();
            Stopwatch stepTimer = Stopwatch.StartNew();

            if (File.Exists(location) == false) {
                return $"missing file location: '{location}'";
            }

            _Logger.LogDebug($"loading map [location={location}]");
            // normalize path (convert windows style \ to /)
            location = Path.GetFullPath(location) ?? throw new Exception($"failed to normalized path");

            string mapName = Path.GetFileName(location)!;
            _Logger.LogDebug($"map name parsed [location={location}] [mapName={mapName}]");

            using ArchiveReader reader = new ArchiveReader(location);
            string mapWorkingFolder = Path.Join("temp", mapName);
            Directory.CreateDirectory(mapWorkingFolder);

            try {
                reader.ExtractAll(mapWorkingFolder);
                long unzipMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                string mapInfoFile = Path.Join(mapWorkingFolder, "mapinfo.lua");
                if (File.Exists(mapInfoFile) == false) {
                    return $"missing mapinfo.lua from '{mapInfoFile}'";
                }

                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // NOTE: because Gex is actually running the Lua,
                // the mapinfo files will normalize all keys to lowercase, as that's what the Lua file does
                // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

                string lua = await File.ReadAllTextAsync(mapInfoFile, cancel);
                if (lua.Length == 0) {
                    return $"mapinfo.lua is empty";
                }

                Result<object[], string> result = await _LuaRunner.Run(lua, TimeSpan.FromSeconds(2), cancel);
                if (result.IsOk == false) {
                    return $"failed to run lua [error={result.Error}]";
                }

                object[] mapInfo = result.Value;
                long runLuaMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                if (mapInfo.Length < 1) {
                    return $"expected at least 1 object in mapInfo";
                }

                if (mapInfo[0] is not Dictionary<object, object> table) {
                    return $"expected returned Lua script to be an Dictionary<object, object>, is a {mapInfo[0].GetType().FullName} instead";
                }

                string? name = table.GetValueOrDefault("name")?.ToString();
                if (name == null) {
                    return $"missing 'name' property in mapinfo table";
                }

                string mapFile = table.GetValueOrDefault("mapfile")?.ToString()
                    ?? "maps/" + name + ".smf"; // if the mapfile is not given, looks like it defaults to the map name

                // get the smf
                string? smfLocation = GetSmfLocation(mapWorkingFolder, mapFile);
                if (smfLocation == null) {
                    return $"failed to find .smf file with name of '{mapFile}' in '{mapWorkingFolder}'";
                }
                if (File.Exists(smfLocation) == false) {
                    throw new Exception($"expected smfLocation '{smfLocation}' to exist");
                }
                long findSmfMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                // get the smt file
                string? smtLocation = GetSmtLocation(mapWorkingFolder, mapFile);
                if (options.Smts == true) {
                    if (smtLocation == null) {
                        return $"failed to find .smt file with a name of '{mapFile}' in '{mapWorkingFolder}'";
                    }
                    if (File.Exists(smtLocation) == false) {
                        throw new InvalidOperationException($"expected smtLocation '{smtLocation}' to exist");
                    }
                }

                object? atmoObj = table["atmosphere"];
                if (atmoObj == null) {
                    return $"missing 'atmosphere' property in mapinfo table";
                }
                if (atmoObj is not Dictionary<object, object> atmo) {
                    return $"expected property 'atmosphere' to be a table, is a {atmoObj.GetType().FullName} instead";
                }

                // but isn't this value minWind in the mapfile.lua? yes, but actually no
                // because the Lua file will normalize all keys to lowercase when executed
                string? minWind = atmo.GetValueOrDefault("minwind")?.ToString();
                string? maxWind = atmo.GetValueOrDefault("maxwind")?.ToString();
                string? maxMetal = table.GetValueOrDefault("maxmetal")?.ToString();
                string? extractorRadius = table.GetValueOrDefault("extractorradius")?.ToString();
                string? tidalStrength = table.GetValueOrDefault("tidalstrength")?.ToString();

                object? resourcesObj = table["resources"];
                if (resourcesObj == null) {
                    return $"missing 'resources' property in mapinfo table";
                }
                if (resourcesObj is not Dictionary<object, object> resources) {
                    return $"expected property 'resources' to be a table, is a {resourcesObj.GetType().FullName} instead";
                }

                string? version = table.GetValueOrDefault("version")?.ToString();

                BarMapData map = new();
                map.Name = name; // + (version == null ? "" : $" {version}");
                if (version != null && map.Name.EndsWith(version) == false) {
                    map.Name += $" {version}";
                }
                map.Description = table.GetValueOrDefault("description")?.ToString() ?? "";
                map.Author = table.GetValueOrDefault("author")?.ToString() ?? "";
                map.FileName = mapName;
                map.NormalMapFilename = resources.GetValueOrDefault("detailnormaltex")?.ToString() ?? "";
                map.SpecularMapFilename = resources.GetValueOrDefault("speculartex")?.ToString() ?? "";

                Result<BarMapFileHeader, string> header = await ParseSmf(smfLocation, options, cancel);
                if (header.IsOk == false) {
                    return $"failed to read .smf at '{smfLocation}': {header.Error}";
                }
                long parseSmfMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
                map.Header = header.Value;

                if (options.Smts == true) {
                    Result<BarMapSmt, string> smtResult = await ParseSmt(smtLocation!, map.Header.TileIndexes, map.Header.Width / 128, map.Header.Height / 128, 32);
                    if (smtResult.IsOk == false) {
                        return $"failed to parse smt at '{smtLocation}': {smtResult.Error}";
                    }

                    map.Smt = smtResult.Value;
                }
                long parseSmtMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                if (options.Normals == true) {
                    string normalPath = Path.Join(mapWorkingFolder, "maps", map.NormalMapFilename);

                    if (File.Exists(normalPath) == false) {
                        _Logger.LogError($"missing normal path [map={map.Name}] [normalMapFilename={map.NormalMapFilename}]");
                    }

                    using FileStream readFs = File.OpenRead(normalPath);
                    using IImage dds = Pfimage.FromStream(readFs);

                    map.NormalMap = DdsToSKBitmap.Convert(dds);
                }

                if (options.Specular == true) {
                    string specularMap = Path.Join(mapWorkingFolder, "maps", map.SpecularMapFilename);

                    if (File.Exists(specularMap) == false) {
                        _Logger.LogError($"missing normal path [map={map.Name}] [specularMapFilename={map.SpecularMapFilename}]");
                    }

                    using FileStream readFs = File.OpenRead(specularMap);
                    using IImage dds = Pfimage.FromStream(readFs);

                    map.SpeculaMap = DdsToSKBitmap.Convert(dds);
                }

                // 2025-04-25 TODO: can this value change? will it always be 64?
                map.Width = header.Value.Width / 64;
                map.Height = header.Value.Height / 64;
                map.ID = header.Value.ID;

                if (double.TryParse(minWind, out double minWindD) == false) {
                    return $"failed to parse minWind (which is '{minWind}') to a valid double";
                } else {
                    map.MinimumWind = minWindD;
                }

                if (double.TryParse(maxWind, out double maxWindD) == false) {
                    return $"failed to parse maxWind (which is '{maxWind}') to a valid double";
                } else {
                    map.MaximumWind = maxWindD;
                }

                if (double.TryParse(extractorRadius, out double extractorRadiusD) == false) {
                    return $"failed to parse extractorRadius (which is '{extractorRadius}') to a valid double";
                } else {
                    map.ExtractorRadius = extractorRadiusD;
                }

                if (double.TryParse(maxMetal, out double maxMetalD) == false) {
                    return $"failed to parse maxMetal (which is '{maxMetal}') to a valid double";
                } else {
                    map.MaxMetal = maxMetalD;
                }

                if (double.TryParse(tidalStrength, out double tidalStrD) == false) {
                    return $"failed to parse tidalStrength (which is '{tidalStrength}') to a valid double";
                } else {
                    map.TidalStrength = tidalStrD;
                }

                _Logger.LogDebug($"parsed map steps [map name={map.Name}] [unzip={unzipMs}ms]"
                    + $" [run lua={runLuaMs}ms] [find smf={findSmfMs}] [parse smf={parseSmfMs}ms] [parse smt={parseSmtMs}ms]");
                _Logger.LogInformation($"parsed map info successfully [map name={map.Name}] [timer={timer.ElapsedMilliseconds}ms] [location={location}]");

                return map;
            } finally {
                try {
                    Directory.Delete(mapWorkingFolder, true);
                } catch (Exception ex) {
                    _Logger.LogWarning($"failed to delete map working folder: {ex.Message}");
                }
            }
        }

        private async Task<Result<BarMapFileHeader, string>> ParseSmf(string location, ParseOptions options, CancellationToken cancel) {
            if (File.Exists(location) == false) {
                return $"failed to open SMF at '{location}'";
            }

            byte[] bytes = await File.ReadAllBytesAsync(location, cancel);

            ByteArrayReader reader = new(bytes);

            string magic = reader.ReadAsciiStringNullTerminated(16);
            if (magic != "spring map file") {
                return $"expected 'spring map file' from SMF, got '{magic}' instead";
            }

            int version = reader.ReadInt32LE();
            if (version != 1) {
                return $"expected version 1, got version {version} instead";
            }

            BarMapFileHeader header = new();
            header.ID = reader.ReadInt32LE();
            header.Width = reader.ReadInt32LE();
            header.Height = reader.ReadInt32LE();
            header.SquareSize = reader.ReadInt32LE();
            header.TexelsPerSquare = reader.ReadInt32LE();
            header.TileSize = reader.ReadInt32LE();
            header.MinHeight = reader.ReadFloat32LE();
            header.MaxHeight = reader.ReadFloat32LE();
            header.HeightMapOffset = reader.ReadInt32LE();
            header.TypeMapOffset = reader.ReadInt32LE();
            header.TileIndexOffset = reader.ReadInt32LE();
            header.MiniMapOffset = reader.ReadInt32LE();
            header.MetalMapOffset = reader.ReadInt32LE();
            header.FeatureMapOffset = reader.ReadInt32LE();
            header.ExtraHeaderCount = reader.ReadInt32LE();

            for (int i = 0; i < header.ExtraHeaderCount; ++i) {
                int size = reader.ReadInt32LE();
                int type = reader.ReadInt32LE();

                Span<byte> _ = reader.Read(size);
            }

            if (options.HeightMap == true) {
                reader.Seek(header.HeightMapOffset);
                int heightMapSize = (header.Width + 1) * (header.Height + 1);
                header.HeightMap = new ushort[heightMapSize];

                Span<byte> rawHeights = reader.Read(heightMapSize * 2);
                ByteArrayReader heightReader = new(rawHeights.ToArray());
                for (int i = 0; i < heightMapSize; ++i) {
                    ushort val = heightReader.ReadUInt16LE();
                    header.HeightMap[i] = val;
                }
            }

            if (options.Smts == true) {
                reader.Seek(header.TileIndexOffset);
                int tileCount = reader.ReadInt32LE();
                int totalTileCount = reader.ReadInt32LE();
                int tileCountHere = reader.ReadInt32LE();

                string smtFileName = Encoding.ASCII.GetString(reader.ReadUntilNull());
                int tileIndexMapSize = (header.Width / 4) * (header.Height / 4);

                byte[] tileIndexMap = reader.Read(tileIndexMapSize * 4).ToArray();
                if (tileIndexMap.Length % 4 != 0) {
                    throw new InvalidOperationException($"tileIndexMap must be divisible by 4, length was {tileIndexMap.Length}");
                }

                header.TileIndexes = new int[tileIndexMap.Length / 4];
                for (int i = 0; i < tileIndexMap.Length; i += 4) {
                    header.TileIndexes[i / 4] = 0
                        | (tileIndexMap[i + 3] << 24)
                        | (tileIndexMap[i + 2] << 16)
                        | (tileIndexMap[i + 1] << 8)
                        | (tileIndexMap[i + 0] << 0);
                }
            }

            return header;
        }

        private async Task<Result<BarMapSmt, string>> ParseSmt(string location, int[] tileIndexes,
            int mapWidthUnits, int mapHeightUnits, int mipmapSize = 32, CancellationToken cancel = default) {

            if (File.Exists(location) == false) {
                return $"failed to open SMT at '{location}'";
            }

            byte[] bytes = await File.ReadAllBytesAsync(location, cancel);
            ByteArrayReader reader = new(bytes);

            string magic = reader.ReadAsciiString(16);
            if (magic != "spring tilefile\0") {
                return $"wrong file magic (got '{magic}')";
            }

            int version = reader.ReadInt32LE();
            if (version != 1) {
                return $"unsupported SMT version {version}";
            }

            int tileCount = reader.ReadInt32LE();
            int tileSize = reader.ReadInt32LE();
            int compressionType = reader.ReadInt32LE();

            int startIndex = mipmapSize == 32 ? 0
                : mipmapSize == 16 ? 512
                : mipmapSize == 8 ? 640
                : 672;
            int dxt1Size = mipmapSize * mipmapSize / 2;
            int rowLength = mipmapSize * 4;

            BarMapSmt smt = new();
            smt.TileCount = tileCount;
            smt.TileSize = tileSize;
            smt.CompressionType = compressionType;
            smt.Version = version;

            List<byte[]> tiles = [];
            for (int i = 0; i < tileCount; ++i) {
                Span<byte> dxt1 = reader.Read(680).Slice(startIndex, dxt1Size);
                byte[] uncompressed = Dxt1.Decompress(mipmapSize, mipmapSize, dxt1);
                tiles.Add(uncompressed);
            }

            int tilesWide = mapWidthUnits * 32;
            int tilesHigh = mapHeightUnits * 32;
            int outputWidth = mipmapSize * tilesWide;
            int outputHeight = mipmapSize * tilesHigh;
            int outputStride = mipmapSize * tilesWide * 4;

            // tiles aren't ordered, they can be in any order, so we use the data from the header to put the tiles in the correct spot
            byte[] output = new byte[outputWidth * outputHeight * 4];
            for (int i = 0; i < tileIndexes.Length; ++i) {
                int refIndex = tileIndexes[i];
                byte[] tileData = tiles[refIndex];
                int tileX = i % tilesWide;
                int tileY = i / tilesWide;

                int destXByte = tileX * mipmapSize * 4;
                int destYRow = tileY * mipmapSize;

                for (int row = 0; row < mipmapSize; ++row) {
                    int srcOffset = row * rowLength;
                    int destOffset = (destYRow + row) * outputStride + destXByte;

                    Span<byte> rowData = tileData.AsSpan().Slice(srcOffset, rowLength);

                    for (int rr = 0; rr < rowData.Length; ++rr) {
                        output[destOffset + rr] = rowData[rr];
                    }
                }
            }

            SKBitmap bitmap = new(outputWidth, outputHeight);
            bitmap.Pixels = RgbaArrayToSKBitmap.Convert(output, bitmap.Pixels);

            smt.Bitmap = bitmap;
            return smt;
        }

        /// <summary>
        ///		try different places to find the SMF
        /// </summary>
        /// <param name="workingDir">directory where the map was extracted to</param>
        /// <param name="mapName">name of the map according to the mapinfo.lua</param>
        /// <returns></returns>
        private string? GetSmfLocation(string workingDir, string mapName) {
            return GetFileWithExtensionLocation(workingDir, mapName, "smf");
        }

        private string? GetSmtLocation(string workingDir, string mapName) {
            return GetFileWithExtensionLocation(workingDir, mapName, "smt");
        }

        private static string? GetFileWithExtensionLocation(string workingDir, string mapName, string fileExt) {
            string mapsFolder = Path.Join(workingDir, "maps");

            string[] files = Directory.GetFiles(mapsFolder);

            foreach (string file in files) {
                if (file.EndsWith($".{fileExt}")) {
                    return file;
                }
            }

            string loc = Path.Join(workingDir, mapName);
            if (File.Exists(loc) == true) {
                return loc;
            }

            loc = Path.Join(workingDir, mapName.ToLower());
            if (File.Exists(loc) == true) {
                return loc;
            }

            loc = Path.Join(workingDir, mapName.Replace(" ", "_"));
            if (File.Exists(loc) == true) {
                return loc;
            }

            loc = Path.Join(workingDir, mapName.ToLower().Replace(" ", "_"));
            if (File.Exists(loc) == true) {
                return loc;
            }

            return null;
        }

        public class ParseOptions {

            public bool Header { get; set; } = true;

            public bool HeightMap { get; set; } = false;

            public bool Smts { get; set; } = false;

            public bool Normals { get; set; } = false;

            public bool Specular { get; set; } = false;

        }

    }
}
