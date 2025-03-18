

export class GameEventTeamsStats {

    public gameID: string = "";
    public frame: number = 0;
    public teamID: number = 0;
    public metalProduced: number = 0;
    public metalUsed: number = 0;
    public metalExcess: number = 0;
    public metalSent: number = 0;
    public metalReceived: number = 0;
    public energyProduced: number = 0;
    public energyUsed: number = 0;
    public energyExcess: number = 0;
    public energySent: number = 0;
    public energyReceived: number = 0;
    public damageDealt: number = 0;
    public damageReceived: number = 0;
    public unitsReceived: number = 0;
    public unitsKilled: number = 0;
    public unitsProduced: number = 0;
    public unitsSent: number = 0;
    public unitsCaptures: number = 0;
    public unitsOutCaptured: number = 0;

    public static parse(elem: any): GameEventTeamsStats {
        return {
            ...elem
        };
    }

}