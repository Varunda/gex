using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Common.Models.Bar {

    public class BarFeature {

        /// <summary>
        ///     internal name of the feature
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        ///     human friendly description of the feature
        /// </summary>
        public string Description { get; set; } = "";

        /// <summary>
        ///     category of the feature, such as "Vegetation"
        ///     TODO 2026-10-19: does this actually do anything?
        /// </summary>
        public string Category { get; set; } = "";

        /// <summary>
        ///     model name
        /// </summary>
        public string Object { get; set; } = "";

        /// <summary>
        ///     how much energy is this worth
        /// </summary>
        public double Energy { get; set; }

        /// <summary>
        ///     how much metal is this worth
        /// </summary>
        public double Metal { get; set; }

        /// <summary>
        ///     health?
        /// </summary>
        public double Damage { get; set; }

        /// <summary>
        ///     equal to (metal + energy) * 6.0 by default
        /// </summary>
        public double ReclaimTime { get; set; } = 1f;

        /// <summary>
        ///     footprint in the X dimension
        /// </summary>
        public double FootprintX { get; set; }

        /// <summary>
        ///     footprint in the Y dimension
        /// </summary>
        public double FootprintZ { get; set; }

        /// <summary>
        ///     does this feature block movemenet?
        /// </summary>
        public bool Blocking { get; set; }

        /// <summary>
        ///     will this feature be upright on slopes? or tilt to match the slope
        /// </summary>
        public bool Upright { get; set; }

        /// <summary>
        ///     does this feature float on top of water?
        /// </summary>
        public bool Floating { get; set; }

        /// <summary>
        ///     if this feature will burn
        /// </summary>
        public bool Flammable { get; set; } = false;

        /// <summary>
        ///     can this feature be reclaimed?
        /// </summary>
        public bool Reclaimable { get; set; } = true;

        /// <summary>
        ///     will this automatically be reclaimed by idle builders?
        /// </summary>
        public bool AutoReclaimable { get; set; }

        /// <summary>
        ///     will this feature never take damage?
        /// </summary>
        public bool Indestructible { get; set; } = false;

        /// <summary>
        ///     can a geothermal be built on top of it?
        /// </summary>
        public bool Geothermal { get; set; } = false;

        /// <summary>
        ///     3 different valid values:
        ///     -1: can rez if the corpse is 1st level,
        ///     0: cannot rez ever,
        ///     1: can always rez
        /// </summary>
        public int Resurrectable { get; set; }

        /// <summary>
        ///     default geothermal vent feature
        /// </summary>
        public static BarFeature DEFAULT_GEOTHERML = new() {
            Blocking = false,
            Flammable = false,
            Indestructible = false,
            Reclaimable = false,
            Geothermal = true,
            Energy = 0,
            Metal = 0,
            ReclaimTime = 0,
            Damage = 0,
            FootprintX = 0,
            FootprintZ = 0,
            Name = "geovent",
        };

        /// <summary>
        ///     default tree feature
        /// </summary>
        public static BarFeature DEFAULT_TREE = new() {
            Blocking = true,
            Flammable = true,
            Indestructible = false,
            Reclaimable = true,
            Metal = 0,
            Energy = 250,
            ReclaimTime = 1500,
            Damage = 5,
            FootprintX = 2,
            FootprintZ = 2,
            Description = "Tree",
            Name = "treetype",
        };

    }
}
