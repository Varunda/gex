
export class GameEventUnitPosition {
    public action: "unit_position" = "unit_position";
    public gameID: string = "";
    public frame: number = 0;
    public unitID: number = 0;
    public teamID: number = 0;
    public x: number = 0;
    public y: number = 0;
    public z: number = 0;

    public static parse(elem: any): GameEventUnitPosition {
        return {
            ...elem
        };
    }

}