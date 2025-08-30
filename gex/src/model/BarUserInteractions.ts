import { BasicBarUser } from "./BarUser";

export class BarUserInteractions {
    public userID: number = 0;
    public targetUserID: number = 0;

    public total: number = 0;

    public withCount: number = 0;
    public withWin: number = 0;
    public againstCount: number = 0;
    public againstWin: number = 0;

    public user: BasicBarUser | null = null;
    public targetUsername: string = "";

    public static parse(elem: any): BarUserInteractions {
        const user: BasicBarUser | null = elem.user == null ? null : BasicBarUser.parse(elem.user)

        return {
            userID: elem.interactions.userID,
            targetUserID: elem.interactions.targetUserID,

            total: elem.interactions.withCount + elem.interactions.againstCount,

            withCount: elem.interactions.withCount,
            withWin: elem.interactions.withWin,
            againstCount: elem.interactions.againstCount,
            againstWin: elem.interactions.againstWin,

            user: user,
            targetUsername: user?.username ?? `<missing ${elem.interactions.userID}>`
        };
    }
}