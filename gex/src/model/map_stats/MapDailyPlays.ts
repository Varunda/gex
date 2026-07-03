
export class MapDailyPlays {

    public mapName: string = "";
    public day: Date = new Date();
    public count: number = 0;

    public static parse(elem: any): MapDailyPlays {
        return {
            mapName: elem.mapName,
            day: new Date(elem.day),
            count: elem.count
        };
    }

}