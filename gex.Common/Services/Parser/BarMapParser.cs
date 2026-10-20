using gex.Common.Code;
using gex.Common.Models;
using gex.Common.Models.Bar;
using gex.Common.Models.Map;
using Microsoft.Extensions.Logging;
using Pfim;
using SevenZip;
using SevenZip.Extensions;
using SkiaSharp;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace gex.Common.Services.Parser {

    public class BarMapParser {

        private readonly ILogger<BarMapParser> _Logger;

        private readonly LuaRunner _LuaRunner;
        private readonly BarFeatureParser _FeatureParser;
        private readonly BarFeatureSetParser _FeatureSetParser;

        public BarMapParser(ILogger<BarMapParser> logger,
            LuaRunner luaRunner, BarFeatureParser featureParser,
            BarFeatureSetParser featureSetParser) {

            _Logger = logger;

            _LuaRunner = luaRunner;
            _FeatureParser = featureParser;
            _FeatureSetParser = featureSetParser;
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

            string mapName = location.EndsWith(".sd7") ? Path.GetFileNameWithoutExtension(location)! : Path.GetFileName(location)!;
            _Logger.LogDebug($"map name parsed [location={location}] [mapName={mapName}]");

            using ArchiveReader reader = new(location);
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory($"gex-map-parse_{mapName}");
            string mapWorkingFolder = Path.Join(tempDir.FullName, mapName);
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

                BarMapData map = new();

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
                // min wind
                string? minWind = atmo.GetValueOrDefault("minwind")?.ToString();
                if (double.TryParse(minWind, out double minWindD) == false) {
                    return $"failed to parse minWind (which is '{minWind}') to a valid double";
                } else {
                    map.MinimumWind = minWindD;
                }

                // max wind
                string? maxWind = atmo.GetValueOrDefault("maxwind")?.ToString();
                if (double.TryParse(maxWind, out double maxWindD) == false) {
                    return $"failed to parse maxWind (which is '{maxWind}') to a valid double";
                } else {
                    map.MaximumWind = maxWindD;
                }

                // max metal
                string? maxMetal = table.GetValueOrDefault("maxmetal")?.ToString();
                if (double.TryParse(maxMetal, out double maxMetalD) == false) {
                    return $"failed to parse maxMetal (which is '{maxMetal}') to a valid double";
                } else {
                    map.MaxMetal = maxMetalD;
                }

                // extractor radius
                string? extractorRadius = table.GetValueOrDefault("extractorradius")?.ToString();
                if (double.TryParse(extractorRadius, out double extractorRadiusD) == false) {
                    return $"failed to parse extractorRadius (which is '{extractorRadius}') to a valid double";
                } else {
                    map.ExtractorRadius = extractorRadiusD;
                }

                // tidal strength
                string? tidalStrength = table.GetValueOrDefault("tidalstrength")?.ToString();
                if (double.TryParse(tidalStrength, out double tidalStrD) == false) {
                    return $"failed to parse tidalStrength (which is '{tidalStrength}') to a valid double";
                } else {
                    map.TidalStrength = tidalStrD;
                }

                object? resourcesObj = table["resources"];
                if (resourcesObj == null) {
                    return $"missing 'resources' property in mapinfo table";
                }
                if (resourcesObj is not Dictionary<object, object> resources) {
                    return $"expected property 'resources' to be a table, is a {resourcesObj.GetType().FullName} instead";
                }

                string? version = table.GetValueOrDefault("version")?.ToString();

                map.Name = name; // + (version == null ? "" : $" {version}");
                if (version != null && map.Name.EndsWith(version) == false) {
                    map.Name += $" {version}";
                }
                map.Description = table.GetValueOrDefault("description")?.ToString() ?? "";
                map.Author = table.GetValueOrDefault("author")?.ToString() ?? "";
                map.FileName = mapName;
                map.NormalMapFilename = resources.GetValueOrDefault("detailnormaltex")?.ToString() ?? "";
                map.SpecularMapFilename = resources.GetValueOrDefault("speculartex")?.ToString() ?? "";

                // parse header
                Result<BarMapFileHeader, string> header = await ParseSmf(smfLocation, options, cancel);
                if (header.IsOk == false) {
                    return $"failed to read .smf at '{smfLocation}': {header.Error}";
                }
                long parseSmfMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();
                map.Header = header.Value;

                // parse tile map
                if (options.Smts == true) {
                    Result<BarMapSmt, string> smtResult = await ParseSmt(smtLocation!, map.Header.TileIndexes, map.Header.Width / 128, map.Header.Height / 128, 32);
                    if (smtResult.IsOk == false) {
                        return $"failed to parse smt at '{smtLocation}': {smtResult.Error}";
                    }

                    map.Smt = smtResult.Value;
                }
                long parseSmtMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                // extract metal spots from metal map
                if (options.Metal == true) {
                    Result<List<BarMapMetalSpot>, string> metalSpots 
                        = await ExtractMetalSpots(header.Value.MetalMap, map.MaxMetal, map.Header.Width / 2, map.Header.Height / 2);

                    if (metalSpots.IsOk == false) {
                        return $"failed to extract map spots: {metalSpots.Error}";
                    }

                    map.MetalSpots = metalSpots.Value;
                }
                long metalMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                // parse normal map
                if (options.Normals == true) {
                    string normalPath = Path.Join(mapWorkingFolder, "maps", map.NormalMapFilename);

                    if (File.Exists(normalPath) == false) {
                        _Logger.LogError($"missing normal path [map={map.Name}] [normalMapFilename={map.NormalMapFilename}]");
                    }

                    using FileStream readFs = File.OpenRead(normalPath);
                    using IImage dds = Pfimage.FromStream(readFs);

                    map.NormalMap = DdsToSKBitmap.Convert(dds);
                }
                long parseNormal = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                // parse specular map
                if (options.Specular == true) {
                    string specularMap = Path.Join(mapWorkingFolder, "maps", map.SpecularMapFilename);

                    if (File.Exists(specularMap) == false) {
                        _Logger.LogError($"missing normal path [map={map.Name}] [specularMapFilename={map.SpecularMapFilename}]");
                    }

                    using FileStream readFs = File.OpenRead(specularMap);
                    using IImage dds = Pfimage.FromStream(readFs);

                    map.SpeculaMap = DdsToSKBitmap.Convert(dds);
                }
                long parseSpec = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                // parse features
                if (options.Features == true) {
                    Result<List<BarFeature>, string> features = await GetFeatures(mapWorkingFolder, cancel);
                    if (features.IsOk == false) {
                        return features.Error;
                    }
                    map.Features = features.Value;

                    Result<List<BarFeature>, string> defaultFeatures = await _FeatureParser.GetDefaultFeatures(cancel);
                    if (defaultFeatures.IsOk == true) {
                        map.Features.AddRange(defaultFeatures.Value);
                    } else {
                        _Logger.LogWarning($"failed to load default features [error={defaultFeatures.Error}]");
                    }

                    Result<BarFeatureSet, string> featureSet = await GetFeatureSet(mapWorkingFolder, cancel);
                    if (featureSet.IsOk == false) {
                        return featureSet.Error;
                    }
                    map.FeatureSet = featureSet.Value;

                    map.ReclaimableMetal = 0;
                    map.ReclaimableEnergy = 0;
                    map.GeothermalCount = 0;

                    HashSet<string> missingFeatureNames = new();

                    Dictionary<string, BarFeature> featureDict = [];
                    foreach (BarFeature feat in map.Features) {
                        // both of these get used
                        featureDict[feat.Name] = feat;
                        featureDict[Path.GetFileNameWithoutExtension(feat.Object)] = feat;
                    }

                    if (featureDict.ContainsKey("geovent") == false) {
                        featureDict["geovent"] = BarFeature.DEFAULT_GEOTHERML;
                        map.Features.Add(BarFeature.DEFAULT_GEOTHERML);
                    }
                    if (featureDict.ContainsKey("treetype") == false) {
                        featureDict["treetype"] = BarFeature.DEFAULT_TREE;
                        map.Features.Add(BarFeature.DEFAULT_TREE);
                    }

                    map.FeatureSet.ObjectList.AddRange(header.Value.FeatureInstances);

                    foreach (BarFeatureInstance inst in map.FeatureSet.ObjectList) {
                        BarFeature? feat = featureDict.GetValueOrDefault(inst.FeatureName);

                        if (feat == null) {
                            if (missingFeatureNames.Contains(inst.FeatureName) == false) {
                                _Logger.LogWarning($"missing feature definition [featureName={inst.FeatureName}] [map={map.Name}]");
                                missingFeatureNames.Add(inst.FeatureName);
                            }

                            continue;
                        }

                        map.ReclaimableEnergy += feat.Energy;
                        map.ReclaimableMetal += feat.Metal;

                        if (feat.Geothermal == true) {
                            map.GeothermalCount += 1;
                        }
                    }
                }
                long parseFeats = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

                // 2025-04-25 TODO: can this value change? will it always be 64?
                map.Width = header.Value.Width / 64;
                map.Height = header.Value.Height / 64;
                map.ID = header.Value.ID;

                _Logger.LogDebug(
                    $"parsed map steps [map name={map.Name}] [unzip={unzipMs}ms]"
                    + $" [run lua={runLuaMs}ms] [find smf={findSmfMs}] [parse smf={parseSmfMs}ms] [parse smt={parseSmtMs}ms]"
                    + $" [parse normal={parseNormal}ms] [parse spec={parseSpec}ms] [parse feats={parseFeats}ms]"
                );

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

        /// <summary>
        ///     parse the SMF (spring mape file) of a map
        /// </summary>
        /// <param name="location">where the SMF is found</param>
        /// <param name="options">options for parsing</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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

            if (options.Metal == true) {
                reader.Seek(header.MetalMapOffset);
                int metalMapSize = (header.Width / 2) * (header.Height / 2);

                Span<byte> buff = reader.Read(metalMapSize);
                header.MetalMap = buff.ToArray();
            }

            if (options.Features == true) {
                reader.Seek(header.FeatureMapOffset);
                Span<byte> buff = reader.ReadAll();

                if (buff.Length >= 4) {
                    Result<List<BarFeatureInstance>, string> instances = ParseFeatureData(buff.ToArray());
                    if (instances.IsOk == false) {
                        return instances.Error;
                    }
                    header.FeatureInstances = instances.Value;
                }
            }

            return header;
        }

        /// <summary>
        ///     parse the SMT of a map
        /// </summary>
        /// <param name="location"></param>
        /// <param name="tileIndexes"></param>
        /// <param name="mapWidthUnits"></param>
        /// <param name="mapHeightUnits"></param>
        /// <param name="mipmapSize"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
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
        ///     get all the features within a map
        /// </summary>
        /// <param name="workingDir"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        private async Task<Result<List<BarFeature>, string>> GetFeatures(string workingDir, CancellationToken cancel) {
            string featureFolder = Path.Join(workingDir, "features");

            if (Directory.Exists(featureFolder) == false) {
                _Logger.LogTrace($"no feature folder exists for map [workingDir={workingDir}] [featureFolder={featureFolder}]");
                return Result<List<BarFeature>, string>.Ok([]);
            }

            List<BarFeature> features = [];

            string[] files = Directory.GetFiles(featureFolder);
            foreach (string file in files) {
                if (file.EndsWith(".lua") == false) {
                    _Logger.LogTrace($"found non-lua file in feature folder, skipping [featureFolder={featureFolder}]");
                    continue;
                }

                string contents = await File.ReadAllTextAsync(file, cancel);

                Result<List<BarFeature>, string> fileFeatures = await _FeatureParser.Parse(contents, cancel);
                if (fileFeatures.IsOk == false) {
                    _Logger.LogError($"failed to parse fileFeatures [file={file}] [featureFolder={featureFolder}] [error={fileFeatures.Error}]");
                    // some maps have invalid lua (dsd 8 way :stare:), try next file
                    continue;
                }

                features.AddRange(fileFeatures.Value);
            }

            return features;
        }

        /// <summary>
        ///     get the <see cref="BarFeatureSet"/> of a map
        /// </summary>
        /// <param name="workingDir">root folder</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        private async Task<Result<BarFeatureSet, string>> GetFeatureSet(string workingDir, CancellationToken cancel) {
            string setFile = Path.Join(workingDir, "mapconfig", "featureplacer", "set.lua");
            if (File.Exists(setFile) == false) {
                _Logger.LogTrace($"no set.lua for map [path={setFile}]");
                return new BarFeatureSet();
            }

            string content = await File.ReadAllTextAsync(setFile, cancel);

            return await _FeatureSetParser.Parse(content, cancel);
        }

        private Result<List<BarFeatureInstance>, string> ParseFeatureData(byte[] data) {
            ByteArrayReader reader = new(data);

            int featureCount = reader.ReadInt32LE();
            int featureTypeCount = reader.ReadInt32LE();

            List<string> featureNames = [];

            for (int i = 0; i < featureCount; ++i) {
                byte[] cstr = reader.ReadUntilNull();
                string str = Encoding.ASCII.GetString(cstr); // trim \0 at end
                featureNames.Add(str);
            }

            List<BarFeatureInstance> features = [];

            for (int i = 0; i < featureTypeCount; ++i) {
                int nameIndex = reader.ReadInt32LE();

                if (nameIndex < 0 || nameIndex > featureNames.Count) {
                    return $"name index is out of bounds [nameIndex={nameIndex}] [featureNames.Count={featureNames.Count}]";
                }

                float x = reader.ReadFloat32LE();
                float y = reader.ReadFloat32LE();
                float z = reader.ReadFloat32LE();
                float rot = reader.ReadFloat32LE();
                float scale = reader.ReadFloat32LE();

                BarFeatureInstance feat = new();
                feat.FeatureName = featureNames[nameIndex];
                feat.X = x;
                feat.Z = z;
                feat.Rotation = rot;

                features.Add(feat);
            }

            return features;
        }

        /// <summary>
        ///     extract a list of <see cref="BarMapMetalSpot"/>s from a metalMap
        /// </summary>
        /// <param name="metalMap">metalMap</param>
        /// <param name="maxMetal">what the maximum value (255) in the metalMap means</param>
        /// <param name="width">width of the metal map</param>
        /// <param name="height">height of the metal map</param>
        /// <returns>
        ///     a result containing a list of <see cref="BarMapMetalSpot"/>s,
        ///     or a string containing an error why the metal spots could not be extracted
        /// </returns>
        private async Task<Result<List<BarMapMetalSpot>, string>> ExtractMetalSpots(byte[] metalMap, double maxMetal, int width, int height) {
            if (width * height != metalMap.Length) {
                return $"expected metalMap to have {width * height} elements ({width} x {height}), had {metalMap.Length} instead";
            }

            byte[][] metalGrid = new byte[height][];
            for (int i = 0; i < height; ++i) {
                metalGrid[i] = new byte[width];
            }

            for (int i = 0; i < metalMap.Length; ++i) {
                int row = i / width;
                int col = i % width;

                metalGrid[row][col] = metalMap[i];
            }

            /*
            string m = "";
            for (int h = 0; h < metalGrid.Length; ++h) {
                for (int w = 0; w < metalGrid[0].Length; ++w) {
                    m += $"{metalGrid[h][w]:X2} ";
                }
                m += "\n";
            }
            _Logger.LogInformation(m);
            */

            List<BarMapMetalSpot> spots = [];

            Stack<int> search = [];
            HashSet<int> used = [];

            // take a row and column pair and turn it into an index used in |search| and |used|
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            int _i(int row, int col) => row * width + col;

            // take a row and column pair, and add all adjacent squares for searching, 
            //      unless that square was already searched or included in a metal spot already
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void _addAdj(int r, int c) {
                if (r > 0) { _push(_i(r - 1, c)); }
                if (r < height - 1) { _push(_i(r + 1, c)); }

                if (c > 0) { _push(_i(r, c - 1)); }
                if (c < width - 1) { _push(_i(r, c + 1)); }
            }

            // push an index (which is a row&col pair) to be search, and mark that index as search (in |used|)
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void _push(int index) {
                if (used.Contains(index) == false) {
                    search.Push(index);
                    used.Add(index);
                }
            }

            // go thru each grid square, finding any coords that have a value greater than 0, and adding that square
            //      and all adjacent squares (and all those adjacent to the adjacent squares, repeat) with a value
            //      greater than 0 to a new metal spot's value.
            //  a list of squares already visited is kept, and no square can be used more than 1

            // 1. go thru each row and column pair (coord -> index)
            // 2. if coord has a value greater than 0 add it and all adjacent (no diagonals) squares to search queue
            // 3. if search queue is empty, go back to step (1)
            // 4. get the next search search, if value is >0, then add it's value to the metal spot's worth, and add all adjacent squares
            // 5. if the search queue isn't empty, go back to step (4)
            for (int r = 0; r < metalGrid.Length; ++r) {
                for (int c = 0; c < metalGrid[0].Length; ++c) {
                    byte value = metalGrid[r][c];

                    int index = _i(r, c);

                    if (used.Contains(index) == true) {
                        continue;
                    }

                    if (value == 0) {
                        continue;
                    }

                    // a spot was found!
                    _addAdj(r, c);

                    BarMapMetalSpot spot = new();

                    // bounding box of all the squares used to make this spot,
                    //      used to create the center of the spot
                    int minR = r;
                    int maxR = r;
                    int minC = c;
                    int maxC = c;

                    while (search.Count > 0) {
                        int a = search.Pop();

                        int ar = a / width;
                        int ac = a % width;

                        byte av = metalGrid[ar][ac];

                        if (av > 0) {
                            // adjust bounds
                            minR = Math.Min(minR, ar);
                            maxR = Math.Max(maxR, ar);
                            minC = Math.Min(minC, ac);
                            maxC = Math.Max(maxC, ac);

                            _addAdj(ar, ac);
                            spot.Worth += av;
                            spot.SquareCount += 1;

                            //_Logger.LogTrace($"adding worth to spot [ar={ar}] [ac={ac}] [av={av}]");
                        }
                    }

                    spot.X = (maxC + minC) / 2d * 16d; // 16 = metal map square size
                    spot.Z = (maxR + minR) / 2d * 16d;
                    spots.Add(spot);
                    //_Logger.LogDebug($"added new spot [x={spot.X}] [mmc={minC}/{maxC}] [z={spot.Z}] [mmr={minR}/{maxR}] [worth={spot.Worth}] [sq count={spot.SquareCount}]");
                }
            }

            foreach (BarMapMetalSpot spot in spots) {
                spot.Worth = spot.Worth * maxMetal / 1000d;
            }

            return spots;
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

            public bool Metal { get; set; } = false;

            public bool Smts { get; set; } = false;

            public bool Normals { get; set; } = false;

            public bool Specular { get; set; } = false;

            public bool Features { get; set; } = false;

        }

    }
}
