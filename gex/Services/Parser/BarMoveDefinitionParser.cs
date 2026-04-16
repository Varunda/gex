using gex.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Models.Bar;
using gex.Services.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Parser {

    public class BarMoveDefinitionParser : BaseLuaTableParser {

        private readonly ILogger<BarMoveDefinitionParser> _Logger;

        private readonly LuaRunner _LuaRunner;

        public BarMoveDefinitionParser(ILogger<BarMoveDefinitionParser> logger,
            LuaRunner luaRunner) {

            _Logger = logger;
            _LuaRunner = luaRunner;
        }

        public async Task<Result<Dictionary<string, BarMoveDefinition>, string>> GetAll(string contents, CancellationToken cancel) {
            Result<object[], string> lua = await _LuaRunner.Run(contents, TimeSpan.FromSeconds(2), cancel);
            if (lua.IsOk == false) {
                _Logger.LogError($"failed to run lua [error={lua.Error}]");
                return lua.Error;
            }

            object[] defs = lua.Value;
            if (defs.Length < 1) {
                return $"expected at least 1 object in unit data";
            }

            if (defs[0] is not Dictionary<object, object> table) {
                return $"expected returned Lua script to be a Dictionary<object, object>, is a {defs[0].GetType().FullName} instead";
            }

            if (table.Keys.Count < 1) {
                return $"expected at least 1 table in unit data";
            }

            List<BarMoveDefinition> moveDefs = [];
            foreach (object moveDefIter in table.Values) {

                if (moveDefIter is not Dictionary<object, object> moveDef) {
                    return $"expected value in table to be a Dictionary<object, object> is a {moveDefIter.GetType().FullName} instead";
                }

                BarMoveDefinition def = new();
                def.Name = _Str(moveDef, "name") ?? throw new Exception($"missing name");
                def.FootprintX = _Double(moveDef, "footprintx", 0d);
                def.FootprintZ = _Double(moveDef, "footprintz", 0d);
                def.CrushStrength = _Double(moveDef, "crushstrength", 0d);
                def.DepthMod = _Double(moveDef, "depthMod", 0d);
                def.MaxSlope = _Double(moveDef, "maxslope", 0d);
                def.MinWaterDepth = _Double(moveDef, "minwaterdepth", 0d);
                def.MaxWaterDepth = _Double(moveDef, "maxwaterdepth", 0d);
                def.MaxWaterSlope = _Double(moveDef, "maxwaterslope", 0d);
                def.SlopeMod = _Double(moveDef, "slopeMod", 0d);
                def.SpeedModClass = _Double(moveDef, "speedModClass", 0d);
                def.Submarine = _Bool(moveDef, "subMarine", false);
                def.OverrideUnitWaterline = _Bool(moveDef, "overrideUnitWaterline", false);

                moveDefs.Add(def);
            }

            return moveDefs.ToDictionary(iter => iter.Name);
        }

    }
}
