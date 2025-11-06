

export class GameEventExtraStatsUpdate {
    public gameID: string = "";
    public frame: number = 0;
    public teamID: number = 0;
    public totalValue: number = 0;
    public armyValue: number = 0;
    public defenseValue: number = 0;
    public utilValue: number = 0;
    public ecoValue: number = 0;
    public otherValue: number = 0;
    public buildPowerAvailable: number = 0;
    public buildPowerUsed: number = 0;
    public metalCurrent: number = 0;
    public energyCurrent: number = 0;
    public actions: number = 0;

    public static parse(elem: any): GameEventExtraStatsUpdate {
        return {
            ...elem
        };
    }
}