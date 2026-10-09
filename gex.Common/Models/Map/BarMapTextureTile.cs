using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Map {

    public class BarMapTextureTile {

        public int Row { get; set; }

        public int Column { get; set; }

        public int TileIndex { get; set; }

        public byte[] Data { get; set; } = [];
        
    }
}
