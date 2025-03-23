
export class BarUserMapStats {
    public userID: number = 0;
    public map: string = "";
    public gamemode: number = 0;
    public playCount: number = 0;
    public winCount: number = 0;
    public lossCount: number = 0;
    public tieCount: number = 0;
    public lastUpdated: Date = new Date();

    public static parse(elem: any): BarUserMapStats {
        return {
            ...elem,
            lastUpdated: new Date(elem.lastUpdated)
        }
    }

}