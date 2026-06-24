
export class BarMatchPlayerLeft {
    public gameID: string = "";
    public playerID: number = 0;
    public reason: number = 0;
    public gameTime: number = 0;

    public static parse(elem: any): BarMatchPlayerLeft {
        return {
            ...elem
        };
    }

}