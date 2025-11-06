using gex.Common.Code.Constants;
using gex.Models.Bar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace gex.Services.Util {

    /// <summary>
    ///     not actually used lol. was an attempt to get APM from just commands, but the values calculated
    ///     were just too high to be reasonable. not enough info from just the demofile to get
    ///     accurate APM values
    /// </summary>
    public class ApmCalculatorUtil {

        public List<ApmStats> Calculate(string gameID, List<BarCommand> commands) {

            const double FRAME_TIMER = (1d / 30d);
            const double PERIOD_INTERVAL = 15d;

            Dictionary<byte, ApmStats> stats = [];
            Dictionary<ushort, float> unitLastAction = [];
            Dictionary<byte, HashSet<float>> actionsOnFrame = [];

            Dictionary<byte, Dictionary<double, int>> periods = [];

            foreach (BarCommand cmd in commands) {
                if (cmd.OptionInternalOrder == true || cmd.ID == BarCommandId.FAILED) {
                    continue;
                }

                // allow at most 1 action per frame
                if (actionsOnFrame.TryGetValue(cmd.PlayerID, out HashSet<float>? actionedFrames) == true && actionedFrames != null) {
                    if (actionedFrames.Contains(cmd.FullGameTime)) {
                        continue;
                    }
                }

                ApmStats apm = stats.GetValueOrDefault(cmd.PlayerID) ?? new ApmStats() {
                    GameID = gameID,
                    PlayerID = cmd.PlayerID,
                };

                // https://github.com/beyond-all-reason/Beyond-All-Reason/blob/4028a6408224453fa1ad6722f93b19d1c6920bff/luarules/gadgets/game_apm_broadcast.lua#L87
                // an action is only counted per unit every 7 frames. if 1 unit gets 7 actions within 7 frames, only 1 is counted
                bool validAction = false;
                foreach (ushort unitID in cmd.UnitIDs) {
                    if (unitLastAction.ContainsKey(unitID)) {
                        float actionTime = unitLastAction.GetValueOrDefault(unitID);
                        if (cmd.FullGameTime - actionTime < (FRAME_TIMER * 7)) {
                            continue;
                        }
                    }

                    validAction = true;
                    unitLastAction[unitID] = cmd.FullGameTime;
                }

                if (validAction == false) {
                    continue;
                }

                double period = Math.Floor(cmd.FullGameTime / 15d) * 15d;
                int cmdsInPrd = (periods.GetValueOrDefault(cmd.PlayerID) ?? []).GetValueOrDefault(period);
                cmdsInPrd += 1;
                if (periods.ContainsKey(cmd.PlayerID) == false) {
                    periods[cmd.PlayerID] = new Dictionary<double, int>();
                }
                periods[cmd.PlayerID][period] = cmdsInPrd;

                apm.ActionCount += 1;

                HashSet<float> frame = actionsOnFrame.GetValueOrDefault(cmd.PlayerID) ?? new HashSet<float>();
                frame.Add(cmd.FullGameTime);
                actionsOnFrame[cmd.PlayerID] = frame;

                stats[cmd.PlayerID] = apm;
            }

            foreach (KeyValuePair<byte, Dictionary<double, int>> iter in periods) {
                if (stats.TryGetValue(iter.Key, out ApmStats? playerApm) == false || playerApm == null) {
                    continue;
                }

                foreach (KeyValuePair<double, int> period in iter.Value) {
                    playerApm.Periods.Add(new ApmPeriod() {
                        TimeStart = period.Key,
                        TimeDuration = PERIOD_INTERVAL,
                        ActionCount = period.Value
                    });
                }
            }

            return stats.Values.ToList();
        }

    }
}
