
export class GameEventTeamDied {
    public gameID: string = "";
    public frame: number = 0;
    public teamID: number = 0;

    public static parse(elem: any): GameEventTeamDied {
        return {
            ...elem
        };
    }
}