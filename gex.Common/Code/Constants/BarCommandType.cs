using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Code.Constants {

    public class BarCommandType {

        // https://github.com/beyond-all-reason/RecoilEngine/blob/master/rts/Sim/Units/CommandAI/Command.h

        public const int ICON = 0;  // expect 0 parameters in return

        public const int ICON_MODE = 5;  // expect 1 parameter in return (number selected mode)

        public const int ICON_MAP = 10;  // expect 3 parameters in return (mappos)

        public const int ICON_AREA = 11;  // expect 4 parameters in return (mappos+radius)

        public const int ICON_UNIT = 12;  // expect 1 parameters in return (unitid)

        public const int ICON_UNIT_OR_MAP = 13;  // expect 1 parameters in return (unitid) or 3 parameters in return (mappos)

        public const int ICON_FRONT = 14;  // expect 3 or 6 parameters in return (middle of front and right side of front if a front was defined)

        public const int ICON_UNIT_OR_AREA = 16;  // expect 1 parameter in return (unitid) or 4 parameters in return (mappos+radius)

        public const int NEXT = 17;  // used with CMD_INTERNAL

        public const int PREV = 18;  // used with CMD_INTERNAL

        public const int ICON_UNIT_FEATURE_OR_AREA = 19;  // expect 1 parameter in return (unitid or featureid+unitHandler.MaxUnits() (id>unitHandler.MaxUnits()=feature)) or 4 parameters in return (mappos+radius)

        public const int ICON_BUILDING = 20;  // expect 3 parameters in return (mappos)

        public const int CUSTOM = 21;  // used with CMD_INTERNAL

        public const int ICON_UNIT_OR_RECTANGLE = 22;  // expect 1 parameter in return (unitid)
                                                               //     or 3 parameters in return (mappos)
                                                               //     or 6 parameters in return (startpos+endpos)

        public const int NUMBER = 23;  // expect 1 parameter in return (number)

    }
}
