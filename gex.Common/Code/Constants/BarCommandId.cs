using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Code.Constants {

    public class BarCommandId {

        // ids lower than 0 are for BUILD, and the absolute value is the unit def ID
        // https://github.com/beyond-all-reason/RecoilEngine/blob/master/rts/Sim/Units/CommandAI/Command.h
        public const int BUILD = -1;

        public const int STOP = 0;

        public const int INSERT = 1;

        public const int REMOVE = 2;

        public const int WAIT = 5;

        public const int TIMEWAIT = 6;

        public const int DEATHWAIT = 7;

        public const int SQUADWAIT = 8;

        public const int GATHERWAIT = 9;

        public const int MOVE = 10;

        public const int PATROL = 15;

        public const int FIGHT = 16;

        public const int ATTACK = 20;

        public const int AREA_ATTACK = 21;

        public const int GUARD = 25;

        // removed https://github.com/beyond-all-reason/RecoilEngine/commit/d33c07aa26d78668112a0846fe3b1223f88dd3ca
        public const int AISELECT = 30;

        public const int GROUPSELECT = 35;

        public const int GROUPADD = 36;

        public const int GROUPCLEAR = 37;

        public const int REPAIR = 40;

        public const int FIRE_STATE = 45;

        public const int MOVE_STATE = 50;

        public const int SETBASE = 55;

        public const int INTERNAL = 60;

        public const int SELFD = 65;

        public const int LOAD_UNITS = 75;

        public const int LOAD_ONTO = 76;

        public const int UNLOAD_UNITS = 80;

        public const int UNLOAD_UNIT = 81;

        public const int ONOFF = 85;

        public const int RECLAIM = 90;

        public const int CLOAK = 95;

        public const int STOCKPILE = 100;

        public const int MANUALFIRE = 105;

        public const int RESTORE = 110;

        public const int REPEAT = 115;

        public const int TRAJECTORY = 120;

        public const int RESURRECT = 125;

        public const int CAPTURE = 130;

        public const int AUTOREPAIRLEVEL = 135;

        public const int IDLEMODE = 145;

        public const int FAILED = 150;

    }
}
