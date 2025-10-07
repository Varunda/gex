
export class GameEventUnitResources {
    public gameID: string = "";
    public frame: number = 0;
    public unitID: number = 0;
    public teamID: number = 0;
    public definitionID: number = 0;
    public metalMade: number = 0;
    public metalUsed: number = 0;
    public energyMade: number = 0;
    public energyUsed: number = 0;

    public static parse(elem: any): GameEventUnitResources {
        return {
            ...elem
        };
    }

}