
export class GameEventUnitsGiven {
    public gameID: string = "";
    public action: string = "unit_taken";
    public unitID: number = 0;
    public teamID: number = 0;
    public newTeamID: number = 0;
    public definitionID: number = 0;
    public unitX: number = 0;
    public unitY: number = 0;
    public unitZ: number = 0;
    public frame: number = 0;

    public static parse(elem: any): GameEventUnitsGiven {
        return {
            ...elem
        };
    }
}