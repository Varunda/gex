

export class GameEventCommanderPositionUpdate {
    public gameID: string = "";
    public frame: number = 0;
    public unitID: number = 0;
    public unitX: number = 0;
    public unitY: number = 0;
    public unitZ: number = 0;

    public static parse(elem: any): GameEventCommanderPositionUpdate {
        return {
            ...elem
        };
    }

}