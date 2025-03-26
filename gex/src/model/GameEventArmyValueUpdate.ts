

export class GameEventExtraStatsUpdate {
    public gameID: string = "";
    public frame: number = 0;
    public teamID: number = 0;
    public armyValue: number = 0;
    public buildPowerAvailable: number = 0;
    public buildPowerUsed: number = 0;

    public static parse(elem: any): GameEventExtraStatsUpdate {
        return {
            ...elem
        };
    }
}