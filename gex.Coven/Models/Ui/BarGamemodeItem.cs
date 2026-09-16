using gex.Common.Code.Constants;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Coven.Models.Ui {

    public class BarGamemodeItem {

        public int? ID { get; set; } = null;

        public string Name { get; set; } = "null";

        public static readonly List<BarGamemodeItem> Items = [
            new BarGamemodeItem() { ID = null, Name = "All" },
            new BarGamemodeItem() { ID = BarGamemode.DUEL, Name = BarGamemode.GetName(BarGamemode.DUEL) },
            new BarGamemodeItem() { ID = BarGamemode.SMALL_TEAM, Name = BarGamemode.GetName(BarGamemode.SMALL_TEAM) },
            new BarGamemodeItem() { ID = BarGamemode.LARGE_TEAM, Name = BarGamemode.GetName(BarGamemode.LARGE_TEAM) },
            new BarGamemodeItem() { ID = BarGamemode.FFA, Name = BarGamemode.GetName(BarGamemode.FFA) },
            new BarGamemodeItem() { ID = BarGamemode.TEAM_FFA, Name = BarGamemode.GetName(BarGamemode.TEAM_FFA) },
            new BarGamemodeItem() { ID = BarGamemode.DEFAULT, Name = BarGamemode.GetName(BarGamemode.DEFAULT) },
        ];

    }
}
