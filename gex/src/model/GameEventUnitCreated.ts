

export class GameEventUnitCreated {
    public gameID: string = "";
    public frame: number = 0;
    public unitID: number = 0;
    public definitionID: number = 0;
    public teamID: number = 0;
    public unitX: number = 0;
    public unitY: number = 0;
    public unitZ: number = 0;
    public rotation: number = 0; // unit: radians

    public static parse(elem: any): GameEventUnitCreated {
        return {
            ...elem
        };
    }
}