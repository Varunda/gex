import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarUser } from "model/BarUser";
import { BarUserSkillChanges } from "model/BarUserSkillChanges";
import { UserSearchResult } from "model/UserSearchResult";

export class BarUserApi extends ApiWrapper<BarUser> {

    private static _instance: BarUserApi = new BarUserApi();
    public static get(): BarUserApi { return BarUserApi._instance; }

    public static getByUserID(userID: number): Promise<Loading<BarUser>> {
        return BarUserApi.get().readSingle(`/api/user/${userID}?includeSkill=true&includeMapStats=true&includeFactionStats=true&includePreviousNames=true&includeUnitsMade=true`, BarUser.parse);
    }

    public static getSkillChanges(userID: number): Promise<Loading<BarUserSkillChanges>> {
        return BarUserApi.get().readSingle(`/api/user/${userID}/skill-changes`, BarUserSkillChanges.parse);
    }

    public static search(text: string, searchPreviousNames: boolean = false, includeSkill: boolean = true): Promise<Loading<UserSearchResult[]>> {
        return BarUserApi.get().readList(`/api/user/search/${encodeURIComponent(text)}?includeSkill=${includeSkill}&searchPreviousNames=${searchPreviousNames}`, UserSearchResult.parse);
    }

}