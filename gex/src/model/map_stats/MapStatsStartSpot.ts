

export class MapStatsStartSpot {
    public mapFilename: string = "";
    public gamemode: number = 0;
    public timestamp: Date = new Date();
    public startX: number = 0;
    public startZ: number = 0;
    public countTotal: number = 0;
    public countWin: number = 0;

    public static parse(elem: any): MapStatsStartSpot {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        }

    }
}