
export class BarSeason {
    public season: number = 0;
    public gameTypes: string[] = [];

    public static parse(elem: any): BarSeason {
        return {
            season: elem.season,
            gameTypes: elem.gameTypes
        };
    }

}