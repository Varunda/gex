using gex.Common.Models;
using gex.Models.Bar;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using ZstdSharp;

namespace gex.Services.Parser {

    public class BarFeatureDefintionParser : BaseLuaTableParser {

        private readonly ILogger<BarFeatureDefintionParser> _Logger;

        public BarFeatureDefintionParser(ILogger<BarFeatureDefintionParser> logger) {
            _Logger = logger;
        }

        public Result<BarUnitFeatureDefinition, string> Parse(Dictionary<object, object> fields) {
            BarUnitFeatureDefinition def = new();

            def.Blocking = _Bool(fields, "blocking", true);
            def.Category = _Str(fields, "category");
            def.Damage = _Double(fields, "damage", 0d);
            def.Metal = _Double(fields, "metal", 0d);
            def.Indestructible = _Bool(fields, "indestructible", false);
            def.Reclaimable = _Bool(fields, "reclaimable", def.Indestructible == false); // false if indestructible, true otherwise
            // -1: only resurrectable if 1st level corpse, 0: no rez, 1: always rez
            def.Resurrectable = _Int(fields, "resurrectable", -1);

            return def;
        }


    }
}
