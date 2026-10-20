using gex.Common.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Common.Models.Bar;
using gex.Common.Models.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Services.Parser {

    public class BarFeatureParser : BaseLuaTableParser {

        private readonly ILogger<BarFeatureParser> _Logger;
        private readonly LuaRunner _LuaRunner;
        private readonly IOptions<FileStorageOptions> _Options;

        private readonly IMemoryCache _Cache;
        private const string CACHE_KEY_DEFAULT = "Gex.FeatureParser.Default";

        public BarFeatureParser(ILogger<BarFeatureParser> logger,
            LuaRunner luaRunner, IOptions<FileStorageOptions> options,
            IMemoryCache cache) {

            _Logger = logger;
            _LuaRunner = luaRunner;
            _Options = options;
            _Cache = cache;
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<Result<List<BarFeature>, string>> GetDefaultFeatures(CancellationToken cancel) {
            if (_Cache.TryGetValue(CACHE_KEY_DEFAULT, out List<BarFeature>? features) == true && features != null) {
                return features;
            }

            features = [];

            string featureDir = Path.Join(_Options.Value.GitHubDataLocation, "features");

            if (Directory.Exists(featureDir) == false) {
                return $"missing feature directory at '{featureDir}'";
            }

            foreach (string featureFile in Directory.GetFiles(featureDir)) {
                if (featureFile.EndsWith(".lua") == false) {
                    continue;
                }

                string lua = await File.ReadAllTextAsync(featureFile, cancel);

                Result<List<BarFeature>, string> fileFeatures = await Parse(lua, cancel);

                if (fileFeatures.IsOk == false) {
                    _Logger.LogError($"failed to parse default feature file [file={featureFile}] [error={fileFeatures.Error}]");
                    continue;
                }

                features.AddRange(fileFeatures.Value);
            }

            return features;
        }

        /// <summary>
        ///     parse a lua script into a list of <see cref="BarFeature"/>s
        /// </summary>
        /// <param name="lua">lua script to be executed</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<List<BarFeature>, string>> Parse(string lua, CancellationToken cancel) {
            Result<object[], string> output = await _LuaRunner.Run(lua, TimeSpan.FromSeconds(5), cancel);
            if (output.IsOk == false) {
                return $"failed to execute lua: {output.Error}";
            }

            object[] unitInfo = output.Value;
            if (unitInfo.Length < 1) {
                return $"expected at least 1 object in unit data";
            }

            if (unitInfo[0] is not Dictionary<object, object> table) {
                return $"expected returned Lua script to be a Dictionary<object, object>, is a {unitInfo[0].GetType().FullName} instead";
            }

            if (table.Keys.Count < 1) {
                return $"expected at least 1 table in feature";
            }

            List<BarFeature> features = [];

            foreach (KeyValuePair<object, object> iter in table) {
                if (iter.Value is not Dictionary<object, object> values) {
                    // supreme_isthmus_v2.1, the 'anemone.lua' file returns more than
                    //      the feature def, ignore those invalid entries
                    continue;
                }
                if (iter.Key is not string name) {
                    return $"key of table was not a string (was a {iter.Key.GetType().FullName} instead)";
                }

                Result<BarFeature, string> feature = Parse(values);
                if (feature.IsOk == false) {
                    return $"failed to parse feature {name}: {feature.Error}";
                }

                features.Add(feature.Value);
            }

            return features;
        }

        /// <summary>
        ///     parse a lua table (represented by a dictionary of objects) into a <see cref="BarFeature"/>
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Result<BarFeature, string> Parse(Dictionary<object, object> values) {
            BarFeature feature = new();

            // https://github.com/beyond-all-reason/RecoilEngine/blob/master/rts/Sim/Features/FeatureDefHandler.cpp#L101
            feature.Name = _Str(values, "name") ?? "<missing name>";
            feature.Description = _Str(values, "description") ?? "";
            feature.Category = _Str(values, "category") ?? "";
            feature.Object = _Str(values, "object") ?? "<missing object>";

            feature.Energy = _Double(values, "energy", 0d);
            feature.Metal = _Double(values, "metal", 0d);
            feature.Damage = _Double(values, "health", _Double(values, "damage", 0.1d));
            feature.ReclaimTime = Math.Max(1d, _Double(values, "reclaimTime", (feature.Energy + feature.Metal) * 6d));

            feature.FootprintX = _Double(values, "footprintx", 1d);
            feature.FootprintZ = _Double(values, "footprintz", 1d);

            feature.Flammable = _Bool(values, "flammable", false);
            feature.Blocking = _Bool(values, "blocking", true);
            feature.Indestructible = _Bool(values, "indestructible", false);
            feature.Reclaimable = _Bool(values, "reclaimable", feature.Indestructible == false); // false if indestructible, true otherwise
            // -1: only resurrectable if 1st level corpse, 0: no rez, 1: always rez
            feature.Resurrectable = _Int(values, "resurrectable", -1);
            feature.AutoReclaimable = _Bool(values, "autoreclaimable", feature.Reclaimable);
            feature.Geothermal = _Bool(values, "geoThermal", false);
            feature.Floating = _Bool(values, "floating", false);
            feature.Upright = _Bool(values, "upright", false);

            return feature;
        }

    }
}
