using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using gex.Common.Models.Event;
using gex.Coven.Models.Match;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels.Match {

    public partial class BarMatchViewUnitStats : ViewModelBase {

        public BarMatchViewUnitStats() {

        }

        public BarMatchViewUnitStats(MatchWindowViewModel vm) {
            UnitStats = new ObservableCollection<BarMatchUnitStats>(Compute(vm));
            Entities = new ObservableCollection<BarMatchEntity>(vm.Entities);
            if (Entities.Any()) {
                SelectedEntity = Entities[0];
            }
        }

        [ObservableProperty]
        private ObservableCollection<BarMatchEntity> _Entities = [];

        [ObservableProperty]
        private BarMatchEntity _SelectedEntity = new();

        [ObservableProperty]
        private ObservableCollection<BarMatchUnitStats> _UnitStats = [];

        [ObservableProperty]
        private ObservableCollection<BarMatchUnitStats> _SelectedUnitStats = [];

        [RelayCommand]
        public void SelectEntity(string name) {
            BarMatchEntity? ent = Entities.FirstOrDefault(iter => iter.Name == name);
            if (ent == null) {
                return;
            }

            SelectedEntity = ent;
            SelectedUnitStats = new ObservableCollection<BarMatchUnitStats>(UnitStats.Where(iter => iter.EntityMatches(ent)));
        }

        public static List<BarMatchUnitStats> Compute(MatchWindowViewModel vm) {
            if (vm.Output == null) {
                return [];
            }

            Dictionary<int, List<BarMatchEntity>> entities = [];
            foreach (BarMatchEntity entity in vm.Entities) {
                foreach (int teamID in entity.TeamIDs) {
                    List<BarMatchEntity> ents = entities.GetValueOrDefault(teamID) ?? new List<BarMatchEntity>();
                    ents.Add(entity);
                    entities[teamID] = ents;
                }
            }

            Dictionary<string, BarMatchUnitStats> map = [];
            Dictionary<int, GameEventUnitDef> unitDefs = vm.Output.UnitDefinitions.ToDictionary(iter => iter.DefinitionID);

            Dictionary<int, long> lastFrameBeforeKilled = [];
            foreach (GameEventTeamDied ev in vm.Output.TeamDiedEvents) {
                lastFrameBeforeKilled.Add(ev.TeamID, ev.Frame - 1);
            }

            BarMatchUnitStats getUnitStats(int teamID, int definitionID) {

                string key = $"{teamID}-{definitionID}";

                BarMatchUnitStats? stats = map.GetValueOrDefault(key);
                if (stats != null) {
                    return stats;
                }

                GameEventUnitDef? unitDef = unitDefs.GetValueOrDefault(definitionID);

                stats = new BarMatchUnitStats();
                stats.Name = unitDef?.Name ?? $"<missing {definitionID}>";
                stats.DefinitionName = unitDef?.DefinitionName ?? $"missing_{definitionID}";
                stats.Entities = entities.GetValueOrDefault(teamID) ?? [];

                map.Add(key, stats);
                return stats;
            }

            void updateAttackerStats(int teamID, GameEventUnitKilled ev) {
                if (ev.AttackerID == null || ev.AttackerDefinitionID == null || ev.AttackerTeam == null) {
                    return;
                }

                BarMatchUnitStats attacker = getUnitStats(teamID, ev.AttackerDefinitionID.Value);
                if (ev.WeaponDefinitionID == -12) {
                    attacker.Reclaims += 1;
                } else {
                    attacker.Kills += 1;

                    GameEventUnitDef? unitDef = unitDefs.GetValueOrDefault(ev.DefinitionID);
                    if (unitDef != null) {
                        if (ev.TeamID == ev.AttackerTeam.Value) {
                            attacker.UnitsTeamKilled[unitDef.DefinitionName] = attacker.UnitsTeamKilled.GetValueOrDefault(unitDef.DefinitionName) + 1;
                        } else {
                            attacker.UnitsKilled[unitDef.DefinitionName] = attacker.UnitsKilled.GetValueOrDefault(unitDef.DefinitionName) + 1;
                        }

                        attacker.MetalKilled += unitDef.MetalCost;
                        attacker.EnergyKilled += unitDef.EnergyCost;
                        attacker.BuildPowerKilled += unitDef.BuildPower;

                        if (unitDef.MetalMake > 0 || unitDef.IsMetalExtractor == true || unitDef.ExtractsMetal > 0 || unitDef.EnergyConversionCapacity > 0) {
                            attacker.MetalEcoKilledMetal += unitDef.MetalCost;
                            attacker.MetalEcoKilledEnergy += unitDef.EnergyCost;
                        }

                        if (unitDef.EnergyProduction > 0 || unitDef.WindGenerator > 0 || unitDef.TidalGenerator > 0 || (unitDef.EnergyUpkeep < 0)) {
                            attacker.EnergyEcoKilledMetal += unitDef.MetalCost;
                            attacker.EnergyEcoKilledEnergy += unitDef.EnergyCost;
                        }

                        if (unitDef.Speed == 0) {
                            attacker.StaticKills += 1;
                        } else if (unitDef.Speed > 0) {
                            attacker.StaticKills += 1;
                        }
                    }
                }
            }

            foreach (GameEventUnitCreated ev in vm.Output.UnitsCreated) {
                if (ev.Completed == 0) {
                    continue;
                }

                BarMatchUnitStats stats = getUnitStats(ev.TeamID, ev.DefinitionID);
                stats.Made += 1;
            }

            foreach (GameEventUnitKilled ev in vm.Output.UnitsKilled) {
                long lastFrame = lastFrameBeforeKilled.GetValueOrDefault(ev.TeamID);
                if (ev.Frame > lastFrame) {
                    continue;
                }

                BarMatchUnitStats stats = getUnitStats(ev.TeamID, ev.DefinitionID);
                if (ev.TeamID == ev.AttackerTeam && ev.WeaponDefinitionID == -12) {
                    stats.Reclaimed += 1;
                } else {
                    stats.Lost += 1;
                }

                if (ev.AttackerID != null && ev.AttackerDefinitionID != null && ev.AttackerTeam != null) {
                    updateAttackerStats(ev.TeamID, ev);
                }
            }

            foreach (GameEventUnitGiven ev in vm.Output.UnitsGiven) {
                BarMatchUnitStats stats = getUnitStats(ev.TeamID, ev.DefinitionID);
                stats.Made += 1;
            }

            foreach (GameEventUnitDamage ev in vm.Output.UnitDamage) {
                long lastFrame = lastFrameBeforeKilled.GetValueOrDefault(ev.TeamID);
                if (ev.Frame > lastFrame) {
                    continue;
                }

                BarMatchUnitStats stats = getUnitStats(ev.TeamID, ev.DefinitionID);
                stats.DamageDealt += ev.DamageDealt;
                stats.DamageTaken += ev.DamageTaken;
            }

            List<BarMatchUnitStats> allStats = map.Values.ToList();
            foreach (BarMatchUnitStats stats in allStats) {
                if (stats.UnitDefinition?.IsCommander == true) {
                    stats.Rank = 999999;
                } else {
                    stats.Rank = stats.Made;
                }

                stats.DamageRatio = stats.DamageDealt / Math.Max(1d, stats.DamageTaken);
                stats.MetalRatio = stats.MetalKilled / Math.Max(1d, stats.Made * (stats.UnitDefinition?.MetalCost ?? 1));
                stats.EnergyRatio = stats.EnergyKilled / Math.Max(1d, stats.Made * (stats.UnitDefinition?.EnergyCost ?? 1));
            }

            return allStats;
        }

    }
}
