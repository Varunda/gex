using gex.Code;
using gex.Models;
using gex.Models.Bar;
using gex.Models.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Parser {

    public class BarMapParser {

        private readonly ILogger<BarMapParser> _Logger;

        private readonly PathEnvironmentService _PathUtil;
        private readonly IOptions<FileStorageOptions> _Options;

        /// <summary>
        ///		what functions and variables the Lua state running the mapinfo is allowed to use
        /// </summary>
        private static List<string?> ALLOWED_VARS = [
            "ipairs", "math", "string", "table", "tonumber",
            "tostring", "type", "unpack", "pack", "next", "pairs"
        ];

        public BarMapParser(ILogger<BarMapParser> logger,
            IOptions<FileStorageOptions> options, PathEnvironmentService pathUtil) {

            _Logger = logger;
            _Options = options;
            _PathUtil = pathUtil;
        }

        /// <summary>
        ///		parse a .sd7 map at the location given
        /// </summary>
        /// <param name="location"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result<BarMap, string>> Parse(string location, CancellationToken cancel) {

            Stopwatch timer = Stopwatch.StartNew();
            Stopwatch stepTimer = Stopwatch.StartNew();

            if (File.Exists(location) == false) {
                return $"missing file location: '{location}'";
            }

            // normalize path (convert windows style \ to /)
            location = Path.GetFullPath(location) ?? throw new Exception($"failed to normalized path");

            string mapName = Path.GetFileName(location)!;
            _Logger.LogDebug($"map name parsed [location={location}] [mapName={mapName}]");

            string mapWorkingFolder = Path.Join(_Options.Value.TempWorkLocation, mapName);
            Directory.CreateDirectory(mapWorkingFolder);

            string sevenzipApp = _PathUtil.FindExecutable("7z") ?? throw new Exception($"failed to find 7z in PATH");

            ProcessStartInfo startInfo = new();
            startInfo.FileName = sevenzipApp;
            startInfo.WorkingDirectory = mapWorkingFolder;
            startInfo.Arguments = $"x -y {location}"; // -y assumes yes
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            ProcessWrapper proc = ProcessWrapper.Create(startInfo, TimeSpan.FromMinutes(3));

            if (proc.ExitCode != 0) {
                _Logger.LogWarning($"expected exit code 0 from 7z process [exitCode={proc.ExitCode}]\nstdout={proc.StdOut}\nstderr={proc.StdErr}");
                return $"expected exit code 0 from 7z process, got {proc.ExitCode} instead!";
            }
            long unzipMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            string mapInfoFile = Path.Join(mapWorkingFolder, "mapinfo.lua");
            if (File.Exists(mapInfoFile) == false) {
                return $"missing mapinfo.lua from '{mapInfoFile}'";
            }

            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            // NOTE: because Gex is actually running the Lua,
            // the mapinfo files will normalize all keys to lowercase,	as that's what the Lua file does
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            using NLua.Lua l = new NLua.Lua();
            object[] output = l.DoString("return _G");
            if (output.Length != 1 || output[0] is not NLua.LuaTable globals) {
                return $"failed to get globals of mapinfo LuaState";
            }

            // remove all objects from the lua state that are not allowed
            foreach (object? key in globals.Keys) {
                if (key == null) {
                    continue;
                }

                string? iter = key.ToString();
                if (ALLOWED_VARS.Contains(iter) == false) {
                    //_Logger.LogTrace($"setting var to null [iter={iter}]");
                    l[iter] = null;
                }
            }

            long setupLuaMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            string lua = await File.ReadAllTextAsync(mapInfoFile, cancel);
            if (lua.Length == 0) {
                return $"mapinfo.lua is empty";
            }

            // compiled lua header byte
            if (lua[0] == 27) {
                return $"refusing to run compiled lua at '{mapInfoFile}'";
            }

            object[]? mapInfo = null;
            try {
                Task executeLua = new(() => {
                    // stubs for functions used in mapinfo files but not part of std
                    mapInfo = l.DoString(@"
						function getfenv()
							local t = {}
							t[""mapinfo""] = {}
							return t
						end

						VFS = {}
						function VFS.DirList(a, b) 
							return {}
						end
					" + lua);
                });
                executeLua.Start();

                // give at most 1 second for a map to parse, if it takes longer then something went very wrong
                await executeLua.WaitAsync(TimeSpan.FromSeconds(1), cancel);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to parse map info [mapInfoFile={mapInfoFile}]");
                return $"failed to run mapinfo.lua: {ex.Message}";
            }

            long runLuaMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            if (mapInfo == null) {
                return $"failed to execute mapinfo.lua in 1 second";
            }

            if (mapInfo.Length < 1) {
                return $"expected at least 1 object in mapInfo";
            }

            if (mapInfo[0] is not NLua.LuaTable table) {
                return $"expected returned Lua script to be an object, is a {mapInfo[0].GetType().FullName} instead";
            }

            string? name = table["name"]?.ToString();
            if (name == null) {
                return $"missing 'name' property in mapinfo table";
            }

            string mapFile = table["mapfile"]?.ToString() ?? ("maps/" + name + ".smf"); // if the mapfile is not given, looks like it defaults to the map name

            string? smfLocation = GetSmfLocation(mapWorkingFolder, mapFile);
            if (smfLocation == null) {
                return $"failed to find .smf file with name of '{mapFile}' in '{mapWorkingFolder}'";
            }
            if (File.Exists(smfLocation) == false) {
                throw new Exception($"expected smfLocation '{smfLocation}' to exist");
            }

            long findSmfMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            Result<BarMapFileHeader, string> header = await ParseSmf(smfLocation, cancel);
            if (header.IsOk == false) {
                return $"failed to read .smf at '{smfLocation}': {header.Error}";
            }
            long parseSmfMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            object? atmoObj = table["atmosphere"];
            if (atmoObj == null) {
                return $"missing 'atmosphere' property in mapinfo table";
            }
            if (atmoObj is not NLua.LuaTable atmo) {
                return $"expected property 'atmosphere' to be a table, is a {atmoObj.GetType().FullName} instead";
            }

            // but isn't this value minWind in the mapfile.lua? yes, but actually no
            // because the Lua file will normalize all keys to lowercase when executed
            string? minWind = atmo["minwind"]?.ToString();
            string? maxWind = atmo["maxwind"]?.ToString();
            string? maxMetal = table["maxmetal"]?.ToString();
            string? extractorRadius = table["extractorradius"]?.ToString();
            string? tidalStrength = table["tidalstrength"]?.ToString();

            BarMap map = new();
            map.Name = name;
            map.Description = table["description"]?.ToString() ?? "";
            map.Author = table["author"]?.ToString() ?? "";
            map.FileName = mapName;

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
                map.MinimumWind = maxWindD;
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

            if (double.TryParse(minWind, out double tidalStrD) == false) {
                return $"failed to parse tidalStrength (which is '{tidalStrength}') to a valid double";
            } else {
                map.TidalStrength = tidalStrD;
            }

            try {
                Directory.Delete(mapWorkingFolder, true);
            } catch (Exception ex) {
                _Logger.LogWarning($"failed to delete map working folder: {ex.Message}");
            }

            _Logger.LogDebug($"parsed map steps [map name={map.Name}] [unzip={unzipMs}ms] [setup lua={setupLuaMs}ms]"
                + $" [run lua={runLuaMs}ms] [find smf={findSmfMs}] [parse smf={parseSmfMs}ms]");
            _Logger.LogInformation($"parsed map info successfully [map name={map.Name}] [timer={timer.ElapsedMilliseconds}ms] [location={location}]");

            return map;
        }

        private async Task<Result<BarMapFileHeader, string>> ParseSmf(string location, CancellationToken cancel) {

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

            return header;
        }

        /// <summary>
        ///		try different places to find the SMF
        /// </summary>
        /// <param name="workingDir">directory where the map was extracted to</param>
        /// <param name="mapName">name of the map according to the mapinfo.lua</param>
        /// <returns></returns>
        private string? GetSmfLocation(string workingDir, string mapName) {
            string mapsFolder = Path.Join(workingDir, "maps");

            string[] files = Directory.GetFiles(mapsFolder);

            foreach (string file in files) {
                if (file.EndsWith(".smf")) {
                    return file;
                }
            }

            string smfLocation = Path.Join(workingDir, mapName);
            if (File.Exists(smfLocation) == true) {
                return smfLocation;
            }

            smfLocation = Path.Join(workingDir, mapName.ToLower());
            if (File.Exists(smfLocation) == true) {
                return smfLocation;
            }

            smfLocation = Path.Join(workingDir, mapName.Replace(" ", "_"));
            if (File.Exists(smfLocation) == true) {
                return smfLocation;
            }

            smfLocation = Path.Join(workingDir, mapName.ToLower().Replace(" ", "_"));
            if (File.Exists(smfLocation) == true) {
                return smfLocation;
            }

            return null;
        }

    }
}
