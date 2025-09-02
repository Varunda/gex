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

    public class BarUnitParser {

        private readonly ILogger<BarUnitParser> _Logger;

        /// <summary>
        ///		what functions and variables the Lua state running the unit info is allowed to use
        /// </summary>
        private static readonly List<string?> ALLOWED_VARS = [
            "ipairs", "math", "string", "table", "tonumber",
            "tostring", "type", "unpack", "pack", "next", "pairs"
        ];

        public BarUnitParser(ILogger<BarUnitParser> logger) { 
            _Logger = logger;
        }

        /// <summary>
        ///		
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result<BarUnit, string>> Parse(string contents, CancellationToken cancel) {

            Stopwatch timer = Stopwatch.StartNew();
            Stopwatch stepTimer = Stopwatch.StartNew();

            using NLua.Lua l = new();
            object[] output = l.DoString("return _G");
            if (output.Length != 1 || output[0] is not NLua.LuaTable globals) {
                return $"failed to get globals of LuaState for unit info";
            }

            // remove all objects from the lua state that are not allowed
            foreach (object? key in globals.Keys) {
                if (key == null) {
                    continue;
                }

                string? iter = key.ToString();
                if (ALLOWED_VARS.Contains(iter) == false) {
                    //_Logger.LogTrace($"setting var to null [iter={iter}]");
                    l[iter] = null;
                }
            }

            long setupLuaMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            // compiled lua header byte
            if (contents[0] == 27) {
                return $"refusing to run compiled lua";
            }

            object[]? unitInfo = null;
            try {
                Task executeLua = new(() => {
                    // raptors queen expects this function to exist
                    unitInfo = l.DoString(@"
                        local Spring = {
                            Utilities = {
                                Gametype = { }
                            }
                        }
                        function Spring.GetModOptions()
							local t = {}
							t[""xmas""] = false
                            t[""assistdronesbuildpowermultiplier""] = 1
							return t
                        end
                        function Spring.Utilities.Gametype.IsRaptors() 
                            return false
                        end
                    " + contents);
                });
                executeLua.Start();

                // give at most 1 second for unit info to parse, if it takes longer then something went very wrong
                await executeLua.WaitAsync(TimeSpan.FromSeconds(1), cancel);
            } catch (Exception ex) {
                _Logger.LogError(ex, $"failed to parse unit data");
                return $"failed to run lua for unit data: {ex.Message}";
            }

            long runLuaMs = stepTimer.ElapsedMilliseconds; stepTimer.Restart();

            if (unitInfo == null) {
                return $"failed to execute unit data lua in 1 second";
            }

            if (unitInfo.Length < 1) {
                return $"expected at least 1 object in unit data";
            }

            if (unitInfo[0] is not NLua.LuaTable table) {
                return $"expected returned Lua script to be an object, is a {unitInfo[0].GetType().FullName} instead";
            }

            if (table.Keys.Count < 1) {
                return $"expected at least 1 table in unit data";
            }

            List<object> keys = table.Keys.CopyToList<object>();
            if (keys.Count < 1) {
                return $"expected at least one key in lua from unit info, as that contains all the data";
            }

            if (table[keys[0]] is not NLua.LuaTable info) {
                return $"expected a table that contained all the unit info";
            }

            BarUnit unit = new();
            unit.DefinitionName = keys[0]!.ToString()!;

            // basic info
            unit.Health = info.GetDouble("health", 0);
            unit.MetalCost = info.GetDouble("metalcost", 0);
            unit.EnergyCost = info.GetDouble("energycost", 0);
            unit.BuildTime = info.GetDouble("buildtime", 0);
            unit.Speed = info.GetDouble("speed", 0);
            unit.TurnRate = info.GetDouble("turnrate", 0);

            // eco stuff
            unit.EnergyProduced = info.GetDouble("energymake", 0);
            unit.EnergyStorage = info.GetDouble("energystorage", 0);
            unit.EnergyUpkeep = info.GetDouble("energyupkeep", 0);
            unit.ExtractsMetal = info.GetDouble("extractsmetal", 0);
            unit.MetalProduced = info.GetDouble("metalmake", 0);
            unit.MetalStorage = info.GetDouble("metalstorage", 0);
            unit.WindGenerator = info.GetDouble("windgenerator", 0);
            // set below in the customparams handling!
            // unit.MetalExtractor = ?;

            // builder
            unit.BuildDistance = info.GetDouble("builddistance", 0);
            unit.BuildPower = info.GetDouble("workertime", 0);

            // los
            unit.SightDistance = info.GetDouble("sightdistance", 0);
            unit.AirSightDistance = info.GetDouble("airsightdistance", 0);
            unit.RadarDistance = info.GetDouble("radardistance", 0);
            unit.SonarDistance = info.GetDouble("sonardistance", 0);
            unit.JamDistance = info.GetDouble("radardistancejam", 0);

            // transport stuff
            unit.TransportCapacity = info.GetDouble("transportcapacity", 0);
            unit.TransportMass = info.GetDouble("transportmass", 0);
            unit.TransportSize = info.GetDouble("transportsize", 0);

            // misc
            unit.CloakCostStill = info.GetDouble("cloakcost", 0);
            unit.CloakCostMoving = info.GetDouble("cloakcostmoving", 0);
            unit.CanResurrect = info.GetBoolean("canresurrect", false);

            object? customParams = info["customparams"];
            if (customParams != null && customParams is NLua.LuaTable parms) {
                unit.ModelAuthor = parms.GetString("model_author");

                unit.MetalExtractor = parms.GetBoolean("metal_extractor", false);
            }

            // weapon parsing
            object? weaponDefsObj = info["weapondefs"];
            if (weaponDefsObj != null && weaponDefsObj is NLua.LuaTable weaponDefs) {
                List<string> weaponKeys = weaponDefs.Keys.CopyToList<string>();

                foreach (string weaponKey in weaponKeys) {
                    BarUnitWeapon weapon = new();
                    weapon.DefinitionName = weaponKey;

                    if (weaponDefs[weaponKey] is not NLua.LuaTable wep) {
                        return $"expected weapondefs.{weaponKey} to be a LuaTable, "
                            + $"was a {weaponDefs[weaponKey].GetType().FullName} instead";
                    }

                    weapon.Name = wep.GetString("name") ?? "<no name given>";
                    weapon.AreaOfEffect = wep.GetDouble("areaofeffect", 0);
                    weapon.Burst = wep.GetDouble("burst", 0);
                    weapon.BurstRate = wep.GetDouble("burstrate", 0);
                    weapon.Range = wep.GetDouble("range", 0);
                    weapon.FlightTime = wep.GetDouble("flighttime", 0);
                    weapon.ImpulseFactor = wep.GetDouble("impulsefactor", 0);
                    weapon.ReloadTime = wep.GetDouble("reloadtime", 0);
                    weapon.WeaponType = wep.GetString("weapontype") ?? "<weapontype not given>";
                    weapon.Tracks = wep.GetBoolean("tracks", false);
                    weapon.Velocity = wep.GetDouble("weaponvelocity", 0);
                    weapon.WaterWeapon = wep.GetBoolean("waterweapon", false);
                    weapon.IsParalyzer = wep.GetBoolean("paralyzer", false);
                    weapon.ParalyzerTime = wep.GetDouble("paralyzetime", 0);
                    weapon.EnergyPerShot = wep.GetDouble("energypershot", 0);
                    weapon.MetalPerShot = wep.GetDouble("metalpershot", 0);

                    object? wepCustomParams = wep["customparams"];
                    if (wepCustomParams != null && wepCustomParams is NLua.LuaTable wepCustomParms) {
                        weapon.IsBogus = wepCustomParms.GetBoolean("bogus", false);
                    }

                    if (weapon.WeaponType != "Shield") {
                        object? damages = wep["damage"];
                        if (damages == null || damages is not NLua.LuaTable dmg) {
                            return $"expected non-null damage and for it to be a LuaTable, was a {damages?.GetType().FullName ?? ""}";
                        }

                        List<string> types = dmg.Keys.CopyToList<string>();
                        foreach (string type in types) {
                            weapon.Damages[type] = dmg.GetDouble(type, 0);
                        }
                    } else {
                        object? shieldObj = wep["shield"];
                        if (shieldObj == null || shieldObj is not NLua.LuaTable shield) {
                            return $"expected non-null shield and for it to be a LuaTable, was a {shieldObj?.GetType().FullName ?? ""}";
                        }

                        BarUnitShield s = new();
                        s.EnergyUpkeep = shield.GetDouble("energyupkeep", 0);
                        s.Force = shield.GetDouble("force", 0);
                        s.Power = shield.GetDouble("power", 0);
                        s.PowerRegen = shield.GetDouble("powerregen", 0);
                        s.PowerRegenEnergy = shield.GetDouble("powerregenenergy", 0);
                        s.Radius = shield.GetDouble("radius", 0);
                        s.StartingPower = shield.GetDouble("startingpower", 0);
                        s.Repulser = shield.GetBoolean("repulser", false);

                        weapon.ShieldData = s;
                    }

                    unit.Weapons.Add(weapon);
                }

                object? weaponSetupObj = info["weapons"];
                if (weaponSetupObj == null || weaponSetupObj is not NLua.LuaTable weaponSetup) {
                    return "failed to find 'weapons' field that goes with weaponDefs";
                } else {
                    List<NLua.LuaTable> weapons = weaponSetup.Values.CopyToList<NLua.LuaTable>();
                    foreach (NLua.LuaTable weapon in weapons) {
                        string? defName = weapon.GetString("def");
                        if (defName == null) {
                            return $"missing def in weapons field: {weapon}";
                        }

                        BarUnitWeapon? matchingWeapon = unit.Weapons.FirstOrDefault(iter => iter.DefinitionName.ToUpper() == defName.ToUpper());
                        if (matchingWeapon == null) {
                            return $"missing matching weapondef from weapon '{defName}'";
                        }

                        matchingWeapon.TargetCategory = weapon.GetString("onlytargetcategory") ?? "";
                    }
                }

                unit.Weapons = unit.Weapons.OrderBy(iter => iter.DefinitionName).ToList();
            }

            return unit;
        }

    }
}
