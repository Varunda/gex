using gex.Code.ExtensionMethods;
using gex.Commands;
using gex.Models.Event;
using gex.Services.Db.Event;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace gex.Code.Commands {

    [Command]
    public class UnitDefCommand {

        private readonly ILogger<UnitDefCommand> _Logger;
        private readonly GameEventUnitDefDb _UnitDefDb;

        public UnitDefCommand(IServiceProvider services) {
            _Logger = services.GetRequiredService<ILogger<UnitDefCommand>>();
            _UnitDefDb = services.GetRequiredService<GameEventUnitDefDb>();
        }

        public async Task Diff(string hash1, string hash2) {
            _Logger.LogInformation($"comparing unit defs {hash1} and {hash2}");

            List<GameEventUnitDef> set1 = await _UnitDefDb.GetByHash(hash1);
            List<GameEventUnitDef> set2 = await _UnitDefDb.GetByHash(hash2);

            string output = "";

            HashSet<int> defIdsSet = new(set1.Select(iter => iter.DefinitionID));
            defIdsSet.AddRange(set2.Select(iter => iter.DefinitionID));

            List<int> defIds = defIdsSet.ToList().Order().ToList();

            foreach (int defId in defIds) {
                GameEventUnitDef? def1 = set1.FirstOrDefault(iter => iter.DefinitionID == defId);
                GameEventUnitDef? def2 = set2.FirstOrDefault(iter => iter.DefinitionID == defId);

                if (def1 == null && def2 == null) {
                    continue;
                } else if (def1 == null && def2 != null) {
                    output += $"{defId}\tnull\t{def2.GetDefinitionHash()}\n";
                } else if (def1 != null && def2 == null) {
                    output += $"{defId}\t{def1.GetDefinitionHash()}\tnull\n";
                } else if (def1 != null && def2 != null && def1.GetDefinitionHash() != def2.GetDefinitionHash()) {
                    output += $"{defId}\t{def1.GetDefinitionHash()}\t{def2.GetDefinitionHash()}\n";
                    output += "\t" + JsonSerializer.Serialize(def1) + "\n";
                    output += "\t" + JsonSerializer.Serialize(def2) + "\n";
                }
            }

            _Logger.LogInformation(output);
        }

    }
}
