import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarUser } from "model/BarUser";

export class BarUserApi extends ApiWrapper<BarUser> {

    private static _instance: BarUserApi = new BarUserApi();
    public static get(): BarUserApi { return BarUserApi._instance; }

    public static getByUserID(userID: number): Promise<Loading<BarUser>> {
        return BarUserApi.get().readSingle(`/api/user/${userID}?includeSkill=true&includeMapStats=true&includeFactionStats=true`, BarUser.parse);
    }


}