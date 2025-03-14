

export class GameEventArmyValueUpdate {
    public gameID: string = "";
    public frame: number = 0;
    public teamID: number = 0;
    public value: number = 0;

    public static parse(elem: any): GameEventArmyValueUpdate {
        return {
            ...elem
        };
    }
}