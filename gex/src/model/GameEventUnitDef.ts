
export class GameEventUnitDef {
    public definitionID: number = 0;
    public definitionName: string = "";
    public name: string = "";
    public tooltip: string = "";
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

    public static parse(elem: any): GameEventUnitDef {
        return {
            ...elem
        };
    }
}
