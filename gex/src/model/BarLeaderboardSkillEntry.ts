
export class BarLeaderboardSkillEntry {
    public gamemode: number = 0;
    public userID: number = 0;
    public username: string = "";
    public skill: number = 0;

    public static parse(elem: any): BarLeaderboardSkillEntry {
        return {
            ...elem
        };
    }

}