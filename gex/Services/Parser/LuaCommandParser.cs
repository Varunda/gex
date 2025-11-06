using gex.Common.Code.Constants;
using gex.Common.Models;
using gex.Models.Bar;
using gex.Models.Bar.Commands;
using gex.Models.Bar.Commands.Types;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace gex.Services.Parser {

    public class LuaCommandParser {

        private readonly ILogger<LuaCommandParser> _Logger;

        public LuaCommandParser(ILogger<LuaCommandParser> logger) {
            _Logger = logger;
        }

        public Result<BarCommand, string> Parse(int commandId, byte options, Span<float> parameters) {
            try {
                BarCommand cmd;

                if (commandId < 0) {
                    int unitDefId = Math.Abs(commandId);
                    commandId = BarCommandId.BUILD;

                    cmd = new BarCommandBuild(parameters) {
                        UnitDefinitionID = unitDefId
                    };
                } else if (commandId == BarCommandId.STOP) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.INSERT) { // insert
                    BarCommandInsert ins = new(parameters);
                    Result<BarCommand, string> subCmd = Parse((int)parameters[1], (byte) parameters[2], parameters[3..]);
                    if (subCmd.IsOk == false) {
                        return subCmd;
                    }
                    ins.Command = subCmd.Value;
                    cmd = ins;
                } else if (commandId == BarCommandId.REMOVE) { // insert
                    BarCommandInsert ins = new(parameters);
                    /*
                    Result<BarCommand, string> subCmd = Parse((int)parameters[1], (byte) parameters[2], parameters[3..]);
                    if (subCmd.IsOk == false) {
                        return subCmd;
                    }
                    ins.Command = subCmd.Value;
                    */
                    cmd = ins;
                } else if (commandId == BarCommandId.WAIT) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.TIMEWAIT) { // number
                    cmd = new BarCommandTypeNumber(parameters);
                } else if (commandId == BarCommandId.DEATHWAIT) { // unit icon or rect
                    cmd = new BarCommandTypeUnitIconOrRect(parameters);
                } else if (commandId == BarCommandId.SQUADWAIT) { // number
                    cmd = new BarCommandTypeNumber(parameters);
                } else if (commandId == BarCommandId.GATHERWAIT) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.MOVE) { // icon map
                    cmd = new BarCommandTypeIconMap(parameters);
                } else if (commandId == BarCommandId.PATROL) { // icon map
                    cmd = new BarCommandTypeIconMap(parameters);
                } else if (commandId == BarCommandId.FIGHT) { // icon map
                    cmd = new BarCommandTypeIconMap(parameters);
                } else if (commandId == BarCommandId.ATTACK) { // icon unit or map
                    cmd = new BarCommandTypeIconUnitOrMap(parameters);
                } else if (commandId == BarCommandId.AREA_ATTACK) { // icon area
                    cmd = new BarCommandTypeIconArea(parameters);
                } else if (commandId == BarCommandId.GUARD) { // icon unit
                    cmd = new BarCommandTypeIconUnit(parameters);
                } else if (commandId == BarCommandId.AISELECT) { // number
                    cmd = new BarCommandTypeNumber(parameters);
                } else if (commandId == BarCommandId.GROUPSELECT) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.GROUPADD) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.GROUPCLEAR) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.REPAIR) { // icon unit or area
                    cmd = new BarCommandTypeIconUnitOrArea(parameters);
                } else if (commandId == BarCommandId.FIRE_STATE) { // icon mode
                    cmd = new BarCommandTypeIconMode(parameters);
                } else if (commandId == BarCommandId.MOVE_STATE) { // icon mode
                    cmd = new BarCommandTypeIconMode(parameters);
                } else if (commandId == BarCommandId.SETBASE) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.INTERNAL) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.SELFD) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.LOAD_UNITS) { // icon unit or area
                    cmd = new BarCommandTypeIconUnitOrArea(parameters);
                } else if (commandId == BarCommandId.LOAD_ONTO) { // icon unit
                    cmd = new BarCommandTypeIconUnit(parameters);
                } else if (commandId == BarCommandId.UNLOAD_UNITS) { // icon unit or area
                    cmd = new BarCommandTypeIconUnitOrArea(parameters);
                } else if (commandId == BarCommandId.UNLOAD_UNIT) { // icon map
                    cmd = new BarCommandTypeIconMap(parameters);
                } else if (commandId == BarCommandId.ONOFF) { // icon mode
                    cmd = new BarCommandTypeIconMode(parameters);
                } else if (commandId == BarCommandId.RECLAIM) { // icon unit feature or area
                    cmd = new BarCommandTypeIconUnitFeatureOrArea(parameters);
                } else if (commandId == BarCommandId.CLOAK) { // icon mode
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.STOCKPILE) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else if (commandId == BarCommandId.MANUALFIRE) { // icon map
                    cmd = new BarCommandTypeIconUnitOrMap(parameters);
                } else if (commandId == BarCommandId.RESTORE) { // icon area
                    cmd = new BarCommandTypeIconArea(parameters);
                } else if (commandId == BarCommandId.REPEAT) { // icon mode
                    cmd = new BarCommandTypeIconMode(parameters);
                } else if (commandId == BarCommandId.TRAJECTORY) { // icon mode
                    cmd = new BarCommandTypeIconMode(parameters);
                } else if (commandId == BarCommandId.RESURRECT) { // icon unit feature or area
                    cmd = new BarCommandTypeIconUnitFeatureOrArea(parameters);
                } else if (commandId == BarCommandId.CAPTURE) { // icon unit or area
                    cmd = new BarCommandTypeIconUnitOrArea(parameters);
                } else if (commandId == BarCommandId.AUTOREPAIRLEVEL) { // icon mode
                    cmd = new BarCommandTypeIconMode(parameters);
                } else if (commandId == BarCommandId.IDLEMODE) { // icon mode
                    cmd = new BarCommandTypeIconMode(parameters);
                } else if (commandId == BarCommandId.FAILED) { // icon
                    cmd = new BarCommandTypeIcon(parameters);
                } else {
                    cmd = new BarCommandCustom(parameters);
                }

                cmd.ID = commandId;
                cmd.OptionMetaKey = (options & 0x02) == 0x02;
                cmd.OptionInternalOrder = (options & 0x04) == 0x04;
                cmd.OptionRightMouseKey = (options & 0x08) == 0x08;
                cmd.OptionShiftKey = (options & 0x10) == 0x10;
                cmd.OptionControlKey = (options & 0x20) == 0x20;
                cmd.OptionAltKey = (options & 0x40) == 0x40;

                return cmd;
            } catch (Exception ex) {
                Debug.Fail(ex.Message, ex.StackTrace);
                return ex.Message;
            }
        }

    }
}
