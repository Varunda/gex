import { BarUserSkill } from "./BarUserSkill";

export class UserSearchResult {
    public userID: number = 0;
    public username: string = "";
    public lastUpdated: Date = new Date();
    public previousName: string = "";
    public skill: BarUserSkill[] = [];

    public static parse(elem: any): UserSearchResult {
        return {
            ...elem,
            lastUpdated: new Date(elem.lastUpdated),
            skill: elem.skill.map((iter: any) => BarUserSkill.parse(iter))
        };
    }
}