using gex.Common.Models;
using gex.Common.Models.Bar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Services.Parser {

    public class BarFeatureSetParser : BaseLuaTableParser {

        private readonly ILogger<BarFeatureSetParser> _Logger;
        private readonly LuaRunner _LuaRunner;

        public BarFeatureSetParser(ILogger<BarFeatureSetParser> logger, LuaRunner luaRunner) {
            _Logger = logger;
            _LuaRunner = luaRunner;
        }

        public async Task<Result<BarFeatureSet, string>> Parse(string lua, CancellationToken cancel) {
            Result<object[], string> output = await _LuaRunner.Run(lua, TimeSpan.FromSeconds(5), cancel);

            object[] unitInfo = output.Value;
            if (unitInfo.Length < 1) {
                return $"expected at least 1 object in feature set data";
            }

            if (unitInfo[0] is not Dictionary<object, object> table) {
                return $"expected returned Lua script to be a Dictionary<object, object>, is a {unitInfo[0].GetType().FullName} instead";
            }

            if (table.Keys.Count < 1) {
                return $"expected at least 1 table in feature set data";
            }

            object? objectListObj = table.GetValueOrDefault("objectlist");
            if (objectListObj is null || objectListObj is not Dictionary<object, object> objectList) {
                return $"expected 'objectlist' to be a Dictionary<object, object>, is a {objectListObj?.GetType().FullName} instead";
            }

            object? buildingListObj = table.GetValueOrDefault("buildinglist");
            if (buildingListObj is null || buildingListObj is not Dictionary<object, object> buildingList) {
                return $"expected 'buildinglist' to be a Dictionary<object, object>, is a {buildingListObj?.GetType().FullName} instead";
            }
            if (buildingList.Count > 0) {
                throw new NotImplementedException($"buildinglist is not yet implemented");
            }

            object? unitListObj = table.GetValueOrDefault("unitlist");
            if (unitListObj is null || unitListObj is not Dictionary<object, object> unitList) {
                return $"expected 'unitlist' to be a Dictionary<object, object>, is a {unitListObj?.GetType().FullName} instead";
            }
            if (unitList.Count > 0) {
                throw new NotImplementedException($"unitlist is not yet implemented");
            }

            List<BarFeatureInstance> objects = [];
            foreach (KeyValuePair<object, object> obj in objectList) {
                if (obj.Value is not Dictionary<object, object> values) {
                    return $"expected object within objectList to be a Dictionary<object, object>,"
                        + $" is a {obj.Value.GetType().FullName} instead";
                }

                Result<BarFeatureInstance, string> instance = ParseObjectEntry(values);
                if (instance.IsOk == false) {
                    return $"failed to parse entry in object list: {instance.Error}";
                }

                objects.Add(instance.Value);
            }

            BarFeatureSet set = new();
            set.ObjectList = objects;

            return set;
        }

        /// <summary>
        ///     parse out a <see cref="BarFeatureInstance"/> that is in the object list
        /// </summary>
        /// <param name="values"></param>
        private Result<BarFeatureInstance, string> ParseObjectEntry(Dictionary<object, object> values) {
            BarFeatureInstance instance = new();

            if (_Str(values, "name") is null) {
                return $"missing 'name' field";
            }

            instance.FeatureName = _Str(values, "name")!;
            instance.X = _Double(values, "x", 0d);
            instance.Z = _Double(values, "z", 0d);

            object? rotObj = values.GetValueOrDefault("rot");
            if (rotObj is not null && rotObj is string rotStr) {
                if (double.TryParse(rotStr, out double rotD) == false) {
                    return $"failed to parse rotation string '{rotStr}' to a double";
                }

                instance.Rotation = rotD;
            } else if (rotObj is not null && rotObj is int rotInt) {
                instance.Rotation = rotInt;
            } else if (rotObj is not null && rotObj is double rotD) {
                instance.Rotation = rotD;
            }

            instance.ResurrectAs = _Str(values, "resurrectAs");
            instance.ReclaimLeft = _Double(values, "reclaimLeft", 0d);

            return instance;
        }


    }
}
