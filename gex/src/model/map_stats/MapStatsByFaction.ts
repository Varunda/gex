
export class MapStatsByFaction {
    public mapFileName: string = "";
    public gamemode: number = 0;
    public faction: number = 0;
    public timestamp: Date = new Date();

    public playCountAllTime: number = 0;
    public winCountAllTime: number = 0;
    public playCountMonth: number = 0;
    public winCountMonth: number = 0;
    public playCountWeek: number = 0;
    public winCountWeek: number = 0;
    public playCountDay: number = 0;
    public winCountDay: number = 0;

    public static parse(elem: any): MapStatsByFaction {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        }

    }
}