
export class BarMatchOutcome {
    public allyTeamID: number = 0;
    public mu: number = 0;
    public sigma: number = 0;
    public winProbability: number = 0;

    public static parse(elem: any): BarMatchOutcome {
        return {
            ...elem
        };
    }

}