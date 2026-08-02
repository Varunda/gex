import { BasicBarUser } from "model/BarUser";

export class MapPositionLeaderboardEntry {
    public userID: number = 0;
    public mapFilename: string = "";
    public positionLabel: string = "";
    public score: number = 0;
    public playCount: number = 0;
    public winCount: number = 0;
    public averageEnemySkill: number = 0;
    public timestamp: Date = new Date();
    public user: BasicBarUser | null = null;

    public static parse(elem: any): MapPositionLeaderboardEntry {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp),
            user: elem.user == null ? null : BasicBarUser.parse(elem.user)
        };
    }

}