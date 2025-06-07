import ColorUtils from "util/Color";

export class GameEventUnitDef {
    public definitionID: number = 0;
    public definitionName: string = "";
    public name: string = "";
    public disambiguatedName: string = "";
    public tooltip: string = "";
    public color: string = "";

    public metalCost: number = 0;
    public energyCost: number = 0;

    public health: number = 0;
    public speed: number = 0;
    public sizeX: number = 0;
    public sizeZ: number = 0;

    public buildTime: number = 0;
    public unitGroup: string = "";
    public buildPower: number = 0;
    public metalMake: number = 0;
    public isMetalExtractor: number = 0;
    public extractsMetal: number = 0;
    public metalStorage: number = 0;
    public energyStorage: number = 0;
    public windGenerator: number = 0;
    public tidalGenerator: number = 0;
    public energyProduction: number = 0;
    public energyUpkeep: number = 0;
    public energyConversionCapacity: number = 0;
    public energyConversionEfficiency: number = 0;
    public sightDistance: number = 0;
    public airSightDistance: number = 0;
    public radarDistance: number = 0;
    public attackRange: number = 0;

    public isCommander: boolean = false;
    public isReclaimer: boolean = false;
    public isFactory: boolean = false;
    public weaponCount: number = 0;

    public category: string = "";

    public static parse(elem: any): GameEventUnitDef {
        const ret: GameEventUnitDef = {
            ...elem
        };

        ret.disambiguatedName = ret.name;

        ret.category = `unknown ${ret.unitGroup}`;

        if (ret.unitGroup == "util" && ret.radarDistance > 0) {
            ret.category = "radar";
        } else if (ret.isCommander == true) {
            ret.category = "commander";
        } else if (ret.isFactory == true) {
            ret.category = "factory";
        } else if (ret.weaponCount > 0 && ret.speed > 0) {
            ret.category = "attack";
        } else if (ret.weaponCount > 0 && ret.speed == 0) {
            ret.category = "defense";
        } else if (ret.unitGroup == "metal" || ret.unitGroup == "energy") {
            ret.category = "eco";
        } else if ((ret.unitGroup == "builder" || ret.unitGroup == "buildert2") && ret.speed > 0) {
            ret.category = "con";
        } else if (ret.unitGroup == "builder" && ret.speed == 0) {
            ret.category = "nano";
        } else if (ret.unitGroup == "util") {
            ret.category = "util";
        } else if (ret.weaponCount == 0 && ret.speed > 0) {
            ret.category = "trans";
        }

        if (ret.definitionName.startsWith("arm")) {
            ret.color = ColorUtils.Armada;
        } else if (ret.definitionName.startsWith("cor")) {
            ret.color = ColorUtils.Cortex;
        } else if (ret.definitionName.startsWith("leg")) {
            ret.color = ColorUtils.Legion;
        } else {

        }

        return ret;
    }
}
