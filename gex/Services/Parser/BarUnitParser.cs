using gex.Code.ExtensionMethods;
using gex.Models;
using gex.Models.Bar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace gex.Services.Parser {

    public class BarUnitParser : BaseLuaTableParser {

        private readonly ILogger<BarUnitParser> _Logger;
        private readonly LuaRunner _LuaRunner;
        private readonly BarWeaponDefinitionParser _WeaponDefinitionParser;

        /// <summary>
        ///		what functions and variables the Lua state running the unit info is allowed to use
        /// </summary>
        private static readonly List<string?> ALLOWED_VARS = [
            "ipairs", "math", "string", "table", "tonumber",
            "tostring", "type", "unpack", "pack", "next", "pairs"
        ];

        public BarUnitParser(ILogger<BarUnitParser> logger,
            BarWeaponDefinitionParser weaponDefinitionParser, LuaRunner luaRunner) {

            _Logger = logger;
            _LuaRunner = luaRunner;
            _WeaponDefinitionParser = weaponDefinitionParser;
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result<BarUnit, string>> Parse(string contents, CancellationToken cancel) {

            Result<object[], string> output = await _LuaRunner.Run(contents, TimeSpan.FromSeconds(1), cancel);
            if (output.IsOk == false) {
                return output.Error;
            }

            object[] unitInfo = output.Value;
            if (unitInfo.Length < 1) {
                return $"expected at least 1 object in unit data";
            }

            if (unitInfo[0] is not Dictionary<object, object> table) {
                return $"expected returned Lua script to be a Dictionary<object, object>, is a {unitInfo[0].GetType().FullName} instead";
            }

            if (table.Keys.Count < 1) {
                return $"expected at least 1 table in unit data";
            }

            List<object> keys = table.Keys.CopyToList<object>();
            if (table[keys[0]] is not Dictionary<object, object> info) {
                return $"expected a table that contained all the unit info";
            }

            BarUnit unit = new();
            unit.DefinitionName = keys[0]!.ToString()!;

            // basic info
            unit.Health = _Double(info, "health", _Double(info, "maxdamage", 0));
            unit.MetalCost = _Double(info, "metalcost", _Double(info, "buildcostmetal", 0));
            unit.EnergyCost = _Double(info, "energycost", _Double(info, "buildcostenergy", 0));
            unit.BuildTime = _Double(info, "buildtime", 0);
            unit.Speed = _Double(info, "speed", 0);
            unit.TurnRate = _Double(info, "turnrate", 0);
            // https://github.com/beyond-all-reason/RecoilEngine/blob/88207e2ee01dc9eccdfc08b8b12eb5c6b6b9ab10/rts/Sim/Units/UnitDef.cpp#L445
            // maxAcc = udTable.GetFloat("maxAcc", udTable.GetFloat("acceleration", 0.5f));
            // for displaying, the game uses 900 * acceleration
            // https://github.com/beyond-all-reason/Beyond-All-Reason/blob/2d264117ff0d4f735e867bf352a5db0cdf32c34d/luaui/Widgets/gui_unit_stats.lua#L431
            // DrawText(texts.move..":", format("%.1f / %.1f / %.0f ("..texts.speedaccelturn..")", uDef.speed, 900 * uDef.maxAcc, simSpeed * uDef.turnRate * (180 / 32767)))
            unit.Acceleration = _Double(info, "maxacc", _Double(info, "accrate", 0.5));

            // https://github.com/beyond-all-reason/RecoilEngine/blob/88207e2ee01dc9eccdfc08b8b12eb5c6b6b9ab10/rts/Sim/Units/UnitDef.cpp#L449
            // maxDec = udTable.GetFloat("maxDec", udTable.GetFloat("brakeRate", maxAcc));
            unit.Deceleration = _Double(info, "maxdec", unit.Acceleration);

            // eco stuff
            unit.EnergyProduced = _Double(info, "energymake", 0);
            unit.EnergyStorage = _Double(info, "energystorage", 0);
            unit.EnergyUpkeep = _Double(info, "energyupkeep", 0);
            unit.ExtractsMetal = _Double(info, "extractsmetal", 0);
            unit.MetalProduced = _Double(info, "metalmake", 0);
            unit.MetalStorage = _Double(info, "metalstorage", 0);
            unit.WindGenerator = _Double(info, "windgenerator", 0);
            // set below in the customparams handling!
            // unit.MetalExtractor = ?;

            // builder
            unit.BuildDistance = _Double(info, "builddistance", 0);
            unit.BuildPower = _Double(info, "workertime", 0);
            unit.IsBuilder = _Bool(info, "builder", false);
            unit.CanAssist = _Bool(info, "canassist", false);
            unit.CanReclaim = _Bool(info, "canreclaim", false);
            unit.CanRepair = _Bool(info, "canrepair", false);
            unit.CanRestore = _Bool(info, "canrestore", false);
            unit.CanResurrect = _Bool(info, "canresurrect", false);

            // los
            unit.SightDistance = _Double(info, "sightdistance", 0);
            unit.AirSightDistance = _Double(info, "airsightdistance", unit.SightDistance * 1.5d);
            unit.RadarDistance = _Double(info, "radardistance", 0);
            unit.SonarDistance = _Double(info, "sonardistance", 0);
            unit.JamDistance = _Double(info, "radardistancejam", 0);

            // transport stuff
            unit.TransportCapacity = _Double(info, "transportcapacity", 0);
            unit.TransportMass = _Double(info, "transportmass", 0);
            unit.TransportSize = _Double(info, "transportsize", 0);

            // misc
            unit.CloakCostStill = _Double(info, "cloakcost", 0);
            unit.CloakCostMoving = _Double(info, "cloakcostmoving", 0);
            unit.ExplodeAs = _Str(info, "explodeas") ?? "";
            unit.SelfDestructWeapon = _Str(info, "selfdestructas") ?? unit.ExplodeAs; // recoil uses this behavior
            unit.SelfDestructCountdown = _Double(info, "selfdestructcountdown", 5d);
            unit.IsStealth = _Bool(info, "stealth", false);

            object? customParams = info.GetValueOrDefault("customparams");
            if (customParams != null && customParams is Dictionary<object, object> parms) {
                unit.ModelAuthor = _Str(parms, "model_author");
                unit.MetalExtractor = _Int(parms, "metal_extractor", 0) == 1;
                unit.ParalyzeMultiplier = _Double(parms, "paralyzemultiplier", 1d);
            }

            // <definition name, definition>, key normalized to UPPER CASE
            Dictionary<string, BarWeaponDefinition> weaponDefsDict = [];

            // weapon parsing
            object? weaponDefsObj = info.GetValueOrDefault("weapondefs");
            if (weaponDefsObj != null && weaponDefsObj is Dictionary<object, object> weaponDefs) {
                List<string> weaponKeys = weaponDefs.Keys.CopyToList<string>();

                foreach (string weaponKey in weaponKeys) {
                    if (weaponDefs[weaponKey] is not Dictionary<object, object> wep) {
                        return $"expected weapondefs.{weaponKey} to be a Dictionary<object, object>, "
                            + $"was a {weaponDefs[weaponKey].GetType().FullName} instead";
                    }

                    Result<BarWeaponDefinition, string> weaponDef = _WeaponDefinitionParser.ParseLua(weaponKey, wep);
                    if (weaponDef.IsOk == false) {
                        return $"failed to parse weapon def [weaponKey={weaponKey}] [error={weaponDef.Error}]";
                    }

                    weaponDefsDict.Add(weaponDef.Value.DefinitionName.ToUpper(), weaponDef.Value);
                }
            }

            if (weaponDefsDict.Count > 0) {
                object? weaponSetupObj = info["weapons"];
                if (weaponSetupObj == null || weaponSetupObj is not Dictionary<object, object> weaponSetup) {
                    return "failed to find 'weapons' field that goes with weaponDefs";
                }

                Dictionary<string, BarUnitWeapon> existingWeapons = [];
                foreach (KeyValuePair<object, object> iter in weaponSetup) {
                    if (iter.Value is not Dictionary<object, object> weapon) {
                        return $"expected weapons.{iter.Key} to be a Dictionary<object, object> was a {iter.Value.GetType().FullName} instead";
                    }

                    string? defName = _Str(weapon, "def");
                    if (defName == null) {
                        return $"missing def in weapons field: {weapon}";
                    }

                    string key = defName.ToUpper();

                    BarWeaponDefinition? weaponDef = weaponDefsDict.GetValueOrDefault(key);
                    if (weaponDef == null) {
                        return $"missing matching weapondef from weapon '{defName}'";
                    }

                    if (existingWeapons.ContainsKey(key) == true) {
                        BarUnitWeapon existingWeapon = existingWeapons.GetValueOrDefault(key) 
                            ?? throw new Exception($"logic error: weapon must exist if the key does");

                        existingWeapon.Count += 1;
                        existingWeapons[key] = existingWeapon;
                    } else {
                        BarUnitWeapon weaponInst = new();
                        weaponInst.WeaponDefinition = weaponDef;
                        weaponInst.TargetCategory = _Str(weapon, "onlytargetcategory") ?? "";
                        weaponInst.Count = 1;

                        existingWeapons.Add(key, weaponInst);
                    }
                }

                unit.Weapons = existingWeapons.Values.OrderBy(iter => iter.WeaponDefinition.DefinitionName).ToList();
            }

            return unit;
        }

    }
}
