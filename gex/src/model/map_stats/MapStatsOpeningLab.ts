
export class MapStatsOpeningLab {
    public mapFilename: string = "";
    public gamemode: number = 0;
    public defName: string = "";
    public timestamp: Date = new Date();
    public countTotal: number = 0;
    public countWin: number = 0;

    public static parse(elem: any): MapStatsOpeningLab {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }
}