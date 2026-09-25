using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Code.Constants;
using gex.Common.Models.Event;
using gex.Common.Models.Match;
using gex.Coven.Models.Match;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.ViewModels.Match {

    public partial class BarMatchMilestonesViewModel : ViewModelBase {

        public BarMatchMilestonesViewModel() {

        }

        public BarMatchMilestonesViewModel(MatchWindowViewModel vm) {
            _Milestones = new ObservableCollection<BarMatchMilestone>(BarMatchMilestonesViewModel.Compute(vm));
        }

        [ObservableProperty]
        private ObservableCollection<BarMatchMilestone> _Milestones = [];

        private static List<BarMatchMilestone> Compute(MatchWindowViewModel vm) {
            if (vm.Output == null) {
                return [];
            }

            List<BarMatchMilestone> milestones = [];
            foreach (BarMatchEntity entity in vm.Entities) {
                milestones.AddRange(GetEntityMilestones(entity, vm.Match.Match, vm.Output));
            }

            return milestones;
        }

        private static List<BarMatchMilestone> GetEntityMilestones(BarMatchEntity entity, BarMatch match, GameOutput output) {
            List<BarMatchMilestone> milestones = [];

            GameEventUnitDef? firstLab = null;
            bool t1Made = false;
            bool t2Made = false;
            bool t3Made = false;
            bool firstAfus = false;
            bool geoMade = false;
            bool ageoMade = false;

            // only care about in duels
            bool vehicleSwap = match.Gamemode != BarGamemode.DUEL;
            bool airMade = match.Gamemode != BarGamemode.DUEL;

            Dictionary<string, GameEventUnitDef> unitDefs = output.UnitDefinitions.ToDictionary(iter => iter.DefinitionName);

            foreach (GameEventUnitCreated ev in output.UnitsCreated) {
                if (entity.TeamIDs.Contains(ev.TeamID) == false) {
                    continue;
                }

                if (ev.Completed == 0) {
                    continue;
                }

                GameEventUnitDef? unitDef = unitDefs.GetValueOrDefault(ev.DefinitionName);
                if (unitDef == null) {
                    continue;
                }

                if (firstLab == null && unitDef.IsFactory == true && unitDef.UnitGroup == "builder" && unitDef.Speed == 0) {
                    firstLab = unitDef;
                }

                if (unitDef.IsFactory && unitDef.Name == "Vehicle Plant" && unitDef.UnitGroup == "builder" && (firstLab?.Name.IndexOf("Bot") ?? 0) > -1 && vehicleSwap == false) {
                    milestones.Add(new BarMatchMilestone() {
                        Entity = entity,
                        Frame = ev.Completed,
                        Action = "Bot -> Vehicle swap",
                        Interest = 8
                    });
                    vehicleSwap = true;
                }

                if (airMade == false) {
                    if (unitDef.IsFactory == true && unitDef.UnitGroup == "builder" && unitDef.Speed == 0 && unitDef.Name.IndexOf("Air") > -1) {
                        milestones.Add(new BarMatchMilestone() {
                            Entity = entity,
                            Frame = ev.Completed,
                            Action = "Air made",
                            Interest = 4
                        });
                        airMade = true;
                    }
                }

                if (t1Made == false) {
                    if (unitDef.IsFactory == true && unitDef.UnitGroup == "builder") {
                        milestones.Add(new BarMatchMilestone() {
                            Entity = entity,
                            Frame = ev.Completed,
                            Action = "T1 made",
                            Interest = 1
                        });
                        t1Made = true;
                    }
                }

                if (t2Made == false) {
                    if (unitDef.IsFactory == true && unitDef.UnitGroup == "buildert2") {
                        milestones.Add(new BarMatchMilestone() {
                            Entity = entity,
                            Frame = ev.Completed,
                            Action = "T2 made",
                            Interest = 10
                        });
                        t2Made = true;
                    }
                }

                if (t3Made == false) {
                    if (unitDef.IsFactory == true && unitDef.UnitGroup == "buildert3") {
                        milestones.Add(new BarMatchMilestone() {
                            Entity = entity,
                            Frame = ev.Completed,
                            Action = "Gantry made",
                            Interest = 5
                        });
                        t3Made = true;
                    }
                }

                if (firstAfus == false) {
                    if (unitDef.EnergyProduction > 2000 && unitDef.BuildTime > 100_000) {
                        milestones.Add(new BarMatchMilestone() {
                            Entity = entity,
                            Frame = ev.Completed,
                            Action = "First AFUS",
                            Interest = 5
                        });
                        firstAfus = true;
                    }
                }

                if (geoMade == false) {
                    if (unitDef.Name.IndexOf("Geothermal") > -1 && unitDef.UnitGroup == "energy" && unitDef.EnergyProduction < 800) {
                        milestones.Add(new BarMatchMilestone() {
                            Entity = entity,
                            Frame = ev.Completed,
                            Action = "Geo built",
                            Interest = 3
                        });
                        geoMade = true;
                    }
                }

                if (ageoMade == false) {
                    if (unitDef.Name.IndexOf("Geothermal") > -1 && unitDef.UnitGroup == "energy" && unitDef.EnergyProduction > 800) {
                        milestones.Add(new BarMatchMilestone() {
                            Entity = entity,
                            Frame = ev.Completed,
                            Action = "Adv. Geo built",
                            Interest = 3
                        });
                        ageoMade = true;
                    }
                }
            }

            return milestones;
        }


    }
}
