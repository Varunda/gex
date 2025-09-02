using Dapper.ColumnMapper;
using gex.Code;
using System.Collections.Generic;

namespace gex.Services.Db {

    [DapperColumnsMapped]
    public class UnitNameToDefinitionSet {

        [ColumnMapping("name")]
        public string Name { get; set; } = "";

        [ColumnMapping("definition_names")]
        public string[] DefinitionNames { get; set; } = [];

    }
}
