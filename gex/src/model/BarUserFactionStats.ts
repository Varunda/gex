
export class BarUserFactionStats {
    public userID: number = 0;
    public faction: number = 0;
    public gamemode: number = 0;
    public playCount: number = 0;
    public winCount: number = 0;
    public lossCount: number = 0;
    public tieCount: number = 0;
    public lastUpdated: Date = new Date();

    public static parse(elem: any): BarUserFactionStats {
        return {
            ...elem,
            lastUpdated: new Date(elem.lastUpdated)
        }
    }
}