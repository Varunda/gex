
export class MapStatsDailyOpeningLab {
    public mapFilename: string = "";
    public gamemode: number = 0;
    public day: Date = new Date();
    public definitionName: string = "";

    public count: number = 0;
    public wins: number = 0;

    public timestamp: Date = new Date();

    public static parse(elem: any): MapStatsDailyOpeningLab {
        return {
            ...elem,
            day: new Date(elem.day),
            timestamp: new Date(elem.timestamp)
        };
    }
}