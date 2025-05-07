import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { AppAccount } from "model/account/AppAccount";

export class AppAccountApi extends ApiWrapper<AppAccount> {
    private static _instance: AppAccountApi = new AppAccountApi();
    public static get(): AppAccountApi { return AppAccountApi._instance; };

    public static async getMe(): Promise<Loading<AppAccount>> {
        return AppAccountApi.get().readSingle(`/api/account/whoami`, AppAccount.parse);
    }

    public static async getAll(): Promise<Loading<AppAccount[]>> {
        return AppAccountApi.get().readList(`/api/account/`, AppAccount.parse);
    }

    public static create(name: string, discordID: string): Promise<Loading<number>> {
        const parms: URLSearchParams = new URLSearchParams();
        parms.set("name", name);
        parms.set("discordID", discordID);

        return AppAccountApi.get().postReply(`/api/account/create?${parms.toString()}`, (elem: any) => elem);
    }

    public static deactivate(accountID: number): Promise<Loading<void>> {
        return AppAccountApi.get().delete(`/api/account/${accountID}`);
    }

}
