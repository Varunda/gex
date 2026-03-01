import { GameEventUnitDef } from "model/GameEventUnitDef";

export type UnitEcoType = "connt1" | "connt2" | "nano" | "rezbot" | "combatengi" |
    | "mext1" | "mext2" | "econvt1" | "econvt2"
    | "solar" | "advsolar" | "wind" | "tidal" | "geot1" | "geot2" | "fusion" | "advfusion";

export default class UnitUtil {

    public static BUILDER_CONN_T1_DEFS: string[] = [];
    public static BUILDER_CONN_T2_DEFS: string[] = [];
    public static BUILDER_COMBAT_ENG_DEFS: string[] = [ "corfast", "cormls", "armconsul", "armmls", "legaceb", "leganavyengineer" ];
    public static BUILDER_NANO_DEFS: string[] = [];
    public static BUILDER_REZBOT_DEFS: string[] = [ "armrectr", "cornecro", "legrezbot" ];

    public static METAL_MEX_T1_DEFS: string[] = [ "armmex", "cormex", "legmex", "legmext15" ]; // where does the legion t1.5 mex go? idk lmao
    public static METAL_MEX_T2_DEFS: string[] = [ "armmoho", "armuwmme", "cormoho", "coruwmme", "legmoho", "leganavalmex", "legmohocon" ];
    public static METAL_ECONV_T1_DEFS: string[] = [ "armmakr", "armfmkr", "cormakr", "corfmkr", "legeconv", "legfeconv" ];
    public static METAL_EVONV_T2_DEFS: string[] = [ "armmmkr", "armuwmmm", "cormmkr", "coruwmmm", "legadveconv", "leganavaleconv", ];

    public static ENERGY_WIND_DEFS: string[] = [];
    public static ENERGY_SOLAR_DEFS: string[] = [ "armsolar", "corsolar", "legsolar" ];
    public static ENERGY_ADV_SOLAR_DEFS: string[] = [ "armadvsol", "coradvsol", "legadvsol" ];
    public static ENERGY_GEO_T1_DEFS: string[] = [ "armgeo", "armuwgeo", "corgeo", "coruwgeo", "leggeo", "leguwgeo" ];
    public static ENERGY_GEO_T2_DEFS: string[] = [ "armgmm", "armageo", "armuwageo", "corbhmth", "corageo", "coruwageo", "legrampart", "legageo", "leganavaladvgeo" ];
    public static ENERGY_FUSION_DEFS: string[] = [ "armfus", "armuwfus", "corfus", "coruwfus", "legfus", "leganavalfusion"];
    public static ENERGY_ADV_FUSION_DEFS: string[] = [ "armafus", "corafus", "legafus" ];

    public static isCommander(defName: string, defs: Map<number, GameEventUnitDef>): boolean {
        return true;
    }

    public static isMetalEco(def: GameEventUnitDef): boolean {
        return def.metalMake > 0 || def.isMetalExtractor > 0 || def.extractsMetal > 0 || def.energyConversionCapacity > 0;
    }

    public static isEnergyEco(def: GameEventUnitDef): boolean {
        return def.energyProduction > 0 || def.windGenerator > 0 || def.tidalGenerator > 0 || (def.energyUpkeep < 0);
    }

    public static getEcoType(def: GameEventUnitDef): UnitEcoType | null {
        const defName: string = def.definitionName;

        if (UnitUtil.BUILDER_REZBOT_DEFS.indexOf(def.definitionName) > -1) {
            return "rezbot";
        } else if (def.unitGroup == "builder" && def.speed > 0) {
            return "connt1";
        } else if (def.unitGroup == "buildert2" && def.speed > 0 && defName != "armfark" && UnitUtil.BUILDER_COMBAT_ENG_DEFS.indexOf(defName) == -1) {
            return "connt2";
        } else if (def.unitGroup == "builder" && def.speed == 0) {
            return "nano";
        }
        
        // metal
        else if (UnitUtil.METAL_MEX_T1_DEFS.indexOf(defName) > -1) {
            return "mext1";
        } else if (UnitUtil.METAL_MEX_T2_DEFS.indexOf(defName) > -1) {
            return "mext2"
        } else if (UnitUtil.METAL_ECONV_T1_DEFS.indexOf(defName) > -1) {
            return "econvt1";
        } else if (UnitUtil.METAL_EVONV_T2_DEFS.indexOf(defName) > -1) {
            return "econvt2";
        }

        // energy
        else if (def.windGenerator > 1) {
            return "wind";
        } else if (UnitUtil.ENERGY_SOLAR_DEFS.indexOf(defName) > -1) {
            return "solar";
        } else if (UnitUtil.ENERGY_ADV_SOLAR_DEFS.indexOf(defName) > -1) {
            return "advsolar";
        } else if (def.tidalGenerator > 0) {
            return "tidal";
        } else if (UnitUtil.ENERGY_GEO_T1_DEFS.indexOf(defName) > -1) {
            return "geot1";
        } else if (UnitUtil.ENERGY_GEO_T2_DEFS.indexOf(defName) > -1) {
            return "geot2";
        } else if (UnitUtil.ENERGY_FUSION_DEFS.indexOf(defName) > -1) {
            return "fusion";
        } else if (UnitUtil.ENERGY_ADV_FUSION_DEFS.indexOf(defName) > -1) {
            return "advfusion";
        }

        return null;
    }

}