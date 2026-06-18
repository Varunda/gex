using gex.Common.Models;
using gex.Models.Bar;
using Microsoft.Extensions.Logging;
using Salaros.Configuration;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.BarApi {

    public class BarMapRotationParser {

        private readonly ILogger<BarMapRotationParser> _Logger;

        public BarMapRotationParser(ILogger<BarMapRotationParser> logger) {
            _Logger = logger;
        }

        public Result<List<BarMapRotation>, string> Parse(string input) {
            ConfigParser config = new ConfigParser(input, new ConfigParserSettings() {
                MultiLineValues = MultiLineValues.Simple | MultiLineValues.AllowValuelessKeys
            });

            List<BarMapRotation> rotations = [];

            foreach (ConfigSection section in config.Sections) {
                BarMapRotation rotation = new();
                rotation.Name = section.SectionName;

                foreach (IConfigKeyValue key in section.Keys) {
                    rotation.Maps.Add(key.Name);
                }

                rotations.Add(rotation);
            }

            return rotations;
        }

    }
}
