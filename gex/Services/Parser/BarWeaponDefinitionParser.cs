using gex.Code.ExtensionMethods;
using gex.Common.Models;
using gex.Models.Bar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
            weapon.Projectiles = _Int(wep, "projectiles", 1);
            weapon.Range = _Double(wep, "range", 0);
            weapon.EdgeEffectiveness = _Double(wep, "edgeeffectiveness", 0);
            weapon.FlightTime = _Double(wep, "flighttime", 0);
            weapon.ImpulseFactor = _Double(wep, "impulsefactor", 0);
            weapon.ImpactOnly = _Bool(wep, "impactonly", false);
            weapon.ReloadTime = _Double(wep, "reloadtime", 0);
            weapon.WeaponType = _Str(wep, "weapontype") ?? "<weapontype not given>";
            weapon.Tracks = _Bool(wep, "tracks", false);
            weapon.SprayAngle = _Double(wep, "sprayangle", 0);
            weapon.Velocity = _Double(wep, "weaponvelocity", 0);
            weapon.WaterWeapon = _Bool(wep, "waterweapon", false);
            weapon.IsParalyzer = _Int(wep, "paralyzer", 0) == 1;
            weapon.ParalyzerTime = _Double(wep, "paralyzetime", 0);
            weapon.EnergyPerShot = _Double(wep, "energypershot", 0);
            weapon.MetalPerShot = _Double(wep, "metalpershot", 0);
            weapon.IsStockpile = _Bool(wep, "stockpile", false);
            weapon.StockpileTime = _Double(wep, "stockpiletime", 0);

            object? wepCustomParams = wep.GetValueOrDefault("customparams");
            if (wepCustomParams != null && wepCustomParams is Dictionary<object, object> wepCustomParms) {
                weapon.IsBogus = _Int(wepCustomParms, "bogus", 0) == 1;
                weapon.ParalyzerExceptions = _Str(wepCustomParms, "paralyzetime_exception") ?? "";
                weapon.StockpileLimit = _Int(wepCustomParms, "stockpilelimit", 0);
                weapon.SweepFire = _Double(wepCustomParms, "sweepfire", 0);
                weapon.ChainForkDamage = _Double(wepCustomParms, "spark_forkdamage", 0);
                weapon.ChainMaxUnits = _Int(wepCustomParms, "spark_maxunits", 0);
                weapon.ChainForkRange = _Double(wepCustomParms, "spark_range", 0);
                weapon.TimedAreaDamage = _Double(wepCustomParms, "area_onhit_damage", 0);
                weapon.TimedAreaRange = _Double(wepCustomParms, "area_onhit_range", 0);
                weapon.TimedAreaTime = _Double(wepCustomParms, "area_onhit_time", 0);
                weapon.ClusterWeaponDefinition = _Str(wepCustomParms, "cluster_def")?.ToUpper();
                weapon.ClusterNumber = _Int(wepCustomParms, "cluster_number", 0);

                if (string.IsNullOrEmpty(_Str(wepCustomParms, "carried_unit")) == false) {
                    BarUnitCarriedUnit carried = new();
                    carried.DefinitionName = _Str(wepCustomParms, "carried_unit") ?? throw new Exception($"how is this null here");
                    carried.EngagementRange = _Double(wepCustomParms, "engagementrange", 0);
                    carried.SpawnSurface = _Str(wepCustomParms, "spawns_surface") ?? "";
                    carried.SpawnRate = _Double(wepCustomParms, "spawnrate", 0);
                    carried.MaxUnits = _Int(wepCustomParms, "maxunits", 0);
                    carried.EnergyCost = _Double(wepCustomParms, "energycost", 0);
                    carried.MetalCost = _Double(wepCustomParms, "metalcost", 0);
                    carried.ControlRadius = _Double(wepCustomParms, "controlradius", 0);
                    carried.DecayRate = _Double(wepCustomParms, "decayrate", 0);
                    carried.EnableDocking = _Bool(wepCustomParms, "enabledocking", false);
                    carried.DockingArmor = _Double(wepCustomParms, "dockingarmor", 0);
                    carried.DockingHealRate = _Double(wepCustomParms, "dockinghealrate", 0);
                    carried.DockToHealThreshold = _Double(wepCustomParms, "docktohealthreshold", 0);
                    // hey BAR
                    // can we talk about how this is the ONLY KEY that is camelCase?
                    // like hey BAR, what the fuck, you got a great thing going on, everything gets turned into lowercase keys
                    // great, i love that, very consistent...
                    // except for this one property
                    carried.DockingHelperSpeed = _Double(wepCustomParms, "dockingHelperSpeed", 0);

                    weapon.CarriedUnit = carried;
                }
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
