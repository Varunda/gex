
export class BarMatchAllyTeam {
    public gameID: string = "";
    public allyTeamID: number = 0;
    public playerCount: number = 0;
    public won: boolean = false;
    public startBox: { top: number, bottom: number, left: number, right: number } = { top: 0, bottom: 0, left: 0, right: 0};

    public static parse(elem: any): BarMatchAllyTeam {
        return {
            ...elem
        };
    }

}
