import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { AppPermission } from "model/account/AppPermission";

export class AppPermissionApi extends ApiWrapper<AppPermission> {

    private static _instance: AppPermissionApi = new AppPermissionApi();
    public static get(): AppPermissionApi { return AppPermissionApi._instance; };

    public static async getAll(): Promise<Loading<AppPermission[]>> {
        return AppPermissionApi.get().readList(`/api/permission/`, AppPermission.parse);
    }
}
