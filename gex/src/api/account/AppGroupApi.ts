import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

import { AppGroup } from "model/account/AppGroup";

export class AppGroupApi extends ApiWrapper<AppGroup> {
    private static _instance: AppGroupApi = new AppGroupApi();
    public static get(): AppGroupApi { return AppGroupApi._instance; }

    public static getAll(): Promise<Loading<AppGroup[]>> {
        return AppGroupApi.get().readList(`/api/group`, AppGroup.parse);
    }

    public static create(name: string): Promise<Loading<void>> {
        return AppGroupApi.get().post(`/api/group?name=${name}&hex=ff0000`);
    }

}
