export class UserUnitsMaderLeaderboardEntry {
    public userID: number = 0;
    public username: string = "";
    public count: number = 0;

    public static parse(elem: any): UserUnitsMaderLeaderboardEntry {
        return {
            ...elem
        };
    }

}