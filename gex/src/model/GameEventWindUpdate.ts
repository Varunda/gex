
export class GameEventWindUpdate {

    public gameID: string = "";
    public frame: number = 0;
    public value: number = 0;

    public static parse(elem: any): GameEventWindUpdate {
        return {
            ...elem
        };
    }

}