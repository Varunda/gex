
export class MapStatsByGamemode {
    public mapFileName: string = "";
    public gamemode: number = 0;
    public timestamp: Date = new Date();
    public playCountDay: number = 0;
    public playCountWeek: number = 0;
    public playCountMonth: number = 0;
    public playCountAllTime: number = 0;
    public durationAverageMs: number = 0;
    public durationMedianMs: number = 0;

    public static parse(elem: any): MapStatsByGamemode {
        return {
            ...elem,
            timestamp: new Date(elem.timstamp)
        };
    }

}