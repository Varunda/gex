using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Code.Constants {

    public enum MapSymmetryAxis : int {

        MISSING = 0,

        // top and bottom are the same
        MIRRORED_VERTICAL = 1, 

        // left and right are the same
        MIRRORED_HORIZONTAL = 2,

        // same across the diagonal
        MIRRORED_DIAGONAL = 3,

        /// <summary>
        ///     left and right are the same, but left 
        /// </summary>
        FLIPPED_VERTICAL = 4,

        /// <summary>
        ///     top and bottom are the same, but top is at the bottom
        /// </summary>
        FLIPPED_HORIZONTAL = 5,

    }
}
