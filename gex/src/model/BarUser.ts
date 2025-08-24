import { BarUserFactionStats } from "./BarUserFactionStats";
import { BarUserMapStats } from "./BarUserMapStats";
import { BarUserSkill } from "./BarUserSkill";
import { BarUserUnitsMade } from "./BarUserUnitsMade";
import { UserPreviousName } from "./UserPreviousName";


export class BarUser {
    public userID: number = 0;
    public username: string = "";
    public lastUpdated: Date = new Date();

    public skill: BarUserSkill[] = [];
    public mapStats: BarUserMapStats[] = [];
    public factionStats: BarUserFactionStats[] = [];
    public previousNames: UserPreviousName[] = [];
    public unitsMade: BarUserUnitsMade[] = [];

    public static parse(elem: any): BarUser {
        return {
            userID: elem.userID,
            username: elem.username,
            lastUpdated: new Date(elem.lastUpdated),

            skill: elem.skill.map((iter: any) => BarUserSkill.parse(iter)),
            mapStats: elem.mapStats.map((iter: any) => BarUserMapStats.parse(iter)),
            factionStats: elem.factionStats.map((iter: any) => BarUserFactionStats.parse(iter)),
            previousNames: elem.previousNames.map((iter: any) => UserPreviousName.parse(iter)),
            unitsMade: elem.unitsMade.map((iter: any) => BarUserUnitsMade.parse(iter))
        };
    }

}