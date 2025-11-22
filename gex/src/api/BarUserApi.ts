import { Loadable, Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarUser } from "model/BarUser";
import { BarUserInteractions } from "model/BarUserInteractions";
import { BarUserSkillChanges } from "model/BarUserSkillChanges";
import { UserSearchResult } from "model/UserSearchResult";

export class BarUserApi extends ApiWrapper<BarUser> {

    private static _instance: BarUserApi = new BarUserApi();
    public static get(): BarUserApi { return BarUserApi._instance; }

    public static getByUserID(userID: number): Promise<Loading<BarUser>> {
        return BarUserApi.get().readSingle(`/api/user/${userID}?includeSkill=true&includeMapStats=true&includeFactionStats=true&includePreviousNames=true&includeUnitsMade=false`, BarUser.parse);
    }

    public static async getByUserIDs(userIDs: number[]): Promise<Loading<BarUser[]>> {
        if (userIDs.length == 0) {
            return Loadable.loaded([]);
        }

        const ret: BarUser[] = [];
        for (let i = 0; i < userIDs.length; i += 100) {
            const slice: Loading<BarUser[]> = await BarUserApi.get().readList(`/api/user/users?${userIDs.slice(i, i + 100).map(iter => `userIDs=${iter}`).join("&")}`, BarUser.parse);
            if (slice.state != "loaded") {
                return slice;
            }

            ret.push(...slice.data);
        }

        return Loadable.loaded(ret);
    }

    public static getSimpleByUserID(userID: number): Promise<Loading<BarUser>> {
        return BarUserApi.get().readSingle(`/api/user/${userID}?includeSkill=false&includeMapStats=false&includeFactionStats=false&includePreviousNames=false&includeUnitsMade=false`, BarUser.parse);
    }

    public static getUnitsMadeByUserID(userID: number): Promise<Loading<BarUser>> {
        return BarUserApi.get().readSingle(`/api/user/${userID}?includeSkill=false&includeMapStats=false&includeFactionStats=false&includePreviousNames=false&includeUnitsMade=true`, BarUser.parse);
    }

    public static getSkillChanges(userID: number): Promise<Loading<BarUserSkillChanges>> {
        return BarUserApi.get().readSingle(`/api/user/${userID}/skill-changes`, BarUserSkillChanges.parse);
    }

    public static getInteractions(userID: number): Promise<Loading<BarUserInteractions[]>> {
        return BarUserApi.get().readList(`/api/user/${userID}/interactions`, BarUserInteractions.parse);
    }

    public static search(text: string, searchPreviousNames: boolean = false, includeSkill: boolean = true): Promise<Loading<UserSearchResult[]>> {
        return BarUserApi.get().readList(`/api/user/search/${encodeURIComponent(text)}?includeSkill=${includeSkill}&searchPreviousNames=${searchPreviousNames}`, UserSearchResult.parse);
    }

}