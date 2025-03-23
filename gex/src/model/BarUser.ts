import { BarUserFactionStats } from "./BarUserFactionStats";
import { BarUserMapStats } from "./BarUserMapStats";
import { BarUserSkill } from "./BarUserSkill";


export class BarUser {
    public userID: number = 0;
    public username: string = "";

    public skill: BarUserSkill[] = [];
    public mapStats: BarUserMapStats[] = [];
    public factionStats: BarUserFactionStats[] = [];

    public static parse(elem: any): BarUser {
        return {
            userID: elem.userID,
            username: elem.username,

            skill: elem.skill.map((iter: any) => BarUserSkill.parse(iter)),
            mapStats: elem.mapStats.map((iter: any) => BarUserMapStats.parse(iter)),
            factionStats: elem.factionStats.map((iter: any) => BarUserFactionStats.parse(iter))
        };
    }

}