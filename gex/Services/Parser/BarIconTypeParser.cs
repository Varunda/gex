using gex.Common.Models;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.Cms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Parser {

    public class BarIconTypeParser {

        private readonly ILogger<BarIconTypeParser> _Logger;
        private readonly LuaRunner _LuaRunner;

        public BarIconTypeParser(ILogger<BarIconTypeParser> logger,
            LuaRunner luaRunner) {

            _Logger = logger;
            _LuaRunner = luaRunner;
        }

        public async Task<Result<Dictionary<string, string>, string>> Parse(string lua, CancellationToken cancel) {
            Result<object[], string> result = await _LuaRunner.Run(lua, TimeSpan.FromSeconds(1), cancel);
            if (result.IsOk == false) {
                return result.Error;
            }

            if (result.Value[0] is not Dictionary<object, object> dict) {
                return $"expected a Dictionary<object,object>, got {result.Value[0].GetType().Name} instead";
            }

            Dictionary<string, string> ret = [];

            foreach (KeyValuePair<object, object> entry in dict) {
                string defName = entry.Key.ToString()!;
                if (defName == "default" || defName == "default_scav") {
                    continue;
                }

                if (entry.Value is not Dictionary<object, object> value) {
                    _Logger.LogWarning($"skipping entry in dict, value is not a dict<object, object> [defName={defName}]");
                    Debug.Fail("not a dict?");
                    continue;
                }

                string? icon = value.GetValueOrDefault("bitmap")?.ToString();
                if (icon == null) {
                    _Logger.LogWarning($"missing bitmap value [defName={defName}]");
                    Debug.Fail("missing bitmap value");
                    continue;
                }

                string[] parts = icon.Split("/");
                if (parts.Length == 1) {
                    ret.Add(defName, parts[0]);
                } else if (parts.Length == 2) {
                    ret.Add(defName, parts[1]);
                } else {
                    ret.Add(defName, parts[2]);
                }
            }

            // remove the _scav icons, as gex doesn't do PvE
            List<string> scavsToRemove = [];
            foreach (KeyValuePair<string, string> iter in ret) {
                if (iter.Key.EndsWith("_scav") == false) {
                    continue;
                }

                if (ret.ContainsKey(iter.Key[..^5])) {
                    scavsToRemove.Add(iter.Key);
                } 
            }

            foreach (string iter in scavsToRemove) {
                ret.Remove(iter);
            }

            return ret;
        }

    }
}
