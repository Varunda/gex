using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Bar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Parser {

    public class BarWeaponDefinitionParser : BaseLuaTableParser {

        private readonly ILogger<BarWeaponDefinitionParser> _Logger;
        private readonly LuaRunner _LuaRunner;

        public BarWeaponDefinitionParser(ILogger<BarWeaponDefinitionParser> logger,
            LuaRunner luaRunner) {

            _Logger = logger;
            _LuaRunner = luaRunner;
        }

        public Result<BarWeaponDefinition, string> ParseLua(string defName, Dictionary<object, object> wep) {
            BarWeaponDefinition weapon = new();
            weapon.DefinitionName = defName;

            weapon.Name = _Str(wep, "name") ?? "<no name given>";
            weapon.AreaOfEffect = _Double(wep, "areaofeffect", 0);
            weapon.Burst = _Double(wep, "burst", 0);
            weapon.BurstRate = _Double(wep, "burstrate", 0);
            weapon.Range = _Double(wep, "range", 0);
            weapon.EdgeEffectiveness = _Double(wep, "edgeeffectiveness", 0);
            weapon.FlightTime = _Double(wep, "flighttime", 0);
            weapon.ImpulseFactor = _Double(wep, "impulsefactor", 0);
            weapon.ImpactOnly = _Bool(wep, "impactonly", false);
            weapon.ReloadTime = _Double(wep, "reloadtime", 0);
            weapon.WeaponType = _Str(wep, "weapontype") ?? "<weapontype not given>";
            weapon.Tracks = _Bool(wep, "tracks", false);
            weapon.Velocity = _Double(wep, "weaponvelocity", 0);
            weapon.WaterWeapon = _Bool(wep, "waterweapon", false);
            weapon.IsParalyzer = _Int(wep, "paralyzer", 0) == 1;
            weapon.ParalyzerTime = _Double(wep, "paralyzetime", 0);
            weapon.EnergyPerShot = _Double(wep, "energypershot", 0);
            weapon.MetalPerShot = _Double(wep, "metalpershot", 0);

            object? wepCustomParams = wep.GetValueOrDefault("customparams");
            if (wepCustomParams != null && wepCustomParams is Dictionary<object, object> wepCustomParms) {
                weapon.IsBogus = _Int(wepCustomParms, "bogus", 0) == 1;
                weapon.ParalyzerExceptions = _Str(wepCustomParms, "paralyzetime_exception") ?? "";
            }

            if (weapon.WeaponType != "Shield") {
                if (wep.GetValueOrDefault("damage") is Dictionary<object, object> dmgs) {
                    foreach (KeyValuePair<object, object> iter in dmgs) {
                        string key = iter.Key.ToString()!;
                        weapon.Damages.Add(key, _Double(dmgs, key, 0));
                    }
                }
            } else {
                object? shieldObj = wep["shield"];
                if (shieldObj == null || shieldObj is not Dictionary<object, object> shield) {
                    return $"expected non-null shield and for it to be a Dictionary<object, object>, was a {shieldObj?.GetType().FullName ?? ""}";
                }

                BarUnitShield s = new();
                s.EnergyUpkeep = _Double(shield, "energyupkeep", 0);
                s.Force = _Double(shield, "force", 0);
                s.Power = _Double(shield, "power", 0);
                s.PowerRegen = _Double(shield, "powerregen", 0);
                s.PowerRegenEnergy = _Double(shield, "powerregenenergy", 0);
                s.Radius = _Double(shield, "radius", 0);
                s.StartingPower = _Double(shield, "startingpower", 0);
                s.Repulser = _Bool(shield, "repulser", false);

                weapon.ShieldData = s;
            }

            return weapon;
        }

        /// <summary>
        ///		parse a Lua script into a weapon definition
        /// </summary>
        /// <param name="contents">lua script that will return weapon definitions</param>
        /// <param name="cancel">cancellation token</param>
        /// <returns></returns>
        public async Task<Result<List<BarWeaponDefinition>, string>> Parse(string contents, CancellationToken cancel) {
            Result<object[], string> luaOutput = await _LuaRunner.Run(contents, TimeSpan.FromSeconds(1), cancel);
            if (luaOutput.IsOk == false) {
                return $"failed to execute Lua [error={luaOutput.Error}]";
            }

            object[] unitInfo = luaOutput.Value;
            if (unitInfo.Length < 1) {
                return $"expected at least 1 object in lua table";
            }

            if (unitInfo[0] is not Dictionary<object, object> table) {
                return $"expected returned Lua script to be a Dictionary<object, object>, is a {unitInfo[0].GetType().FullName} instead";
            }

            List<object> keys = table.Keys.CopyToList<object>();
            if (table.Keys.Count < 1) {
                return $"expected at least one key in lua from weapon definition, as that contains all the data";
            }

            if (table[keys[0]] is not Dictionary<object, object> info) {
                return $"expected a table that contained all the weapon definitions";
            }

            List<BarWeaponDefinition> ret = [];

            foreach (object key in keys) {
                if (table[key] is not Dictionary<object, object> wep) {
                    return $"expected key '{key}' to be a Dictionary<object, object>, was a {table[key]?.GetType().FullName ?? "<null>"} instead";
                }

                Result<BarWeaponDefinition, string> def = ParseLua(key.ToString()!, wep);
                if (def.IsOk == false) {
                    return def.Error;
                }

                ret.Add(def.Value);
            }

            return ret;
        }

    }
}
