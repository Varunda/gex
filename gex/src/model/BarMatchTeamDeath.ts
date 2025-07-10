
export class BarMatchTeamDeath {
    public gameID: string = "";
    public teamID: number = 0;
    public reason: number = 0; // 2 = resign, 4 = killed
    public gameTime: number = 0;

    public static parse(elem: any): BarMatchTeamDeath {
        return {
            ...elem
        };
    }
}