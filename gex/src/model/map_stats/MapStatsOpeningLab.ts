
export class MapStatsOpeningLab {
    public mapFilename: string = "";
    public gamemode: number = 0;
    public defName: string = "";
    public timestamp: Date = new Date();
    public countTotal: number = 0;
    public winTotal: number = 0;
    public countMonth: number = 0;
    public winMonth: number = 0;
    public countWeek: number = 0;
    public winWeek: number = 0;
    public countDay: number = 0;
    public winDay: number = 0;

    public static parse(elem: any): MapStatsOpeningLab {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }
}