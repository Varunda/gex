
export class ApmStats {

    public gameID: string = "";
    public playerID: number = 0;
    public actionCount: number = 0;
    public periods: ApmPeriod[] = [];

    public static parse(elem: any): ApmStats {
        return {
            gameID: elem.gameID,
            playerID: elem.playerID,
            actionCount: elem.actionCount,
            periods: elem.periods.map((iter: any) => ApmPeriod.parse(iter)) 
        };
    }
}

export class ApmPeriod {
    public actionCount: number = 0;
    public timeStart: number = 0;
    public timeDuration: number = 0;

    public static parse(elem: any): ApmPeriod {
        return {
            ...elem
        };
    }
}