import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class AppAccount {
    public id: number = 0;
    public name: string = "";
    public email: string = "";
    public discord: string = "";
    public discordID: string = "0";
    public deletedOn: Date | null = null;
    public deletedBy: number | null = null;
}

export class AppAccountApi extends ApiWrapper<AppAccount> {
    private static _instance: AppAccountApi = new AppAccountApi();
    public static get(): AppAccountApi { return AppAccountApi._instance; };

    public static parse(elem: any): AppAccount {
        return {
            ...elem,
            discordID: elem.discordID.toString(),
            deletedOn: (elem.deletedOn != null) ? new Date(elem.deletedOn) : null
        };
    }

    public static async getMe(): Promise<Loading<AppAccount>> {
        return AppAccountApi.get().readSingle(`/api/account/whoami`, AppAccountApi.parse);
    }

    public static async getAll(): Promise<Loading<AppAccount[]>> {
        return AppAccountApi.get().readList(`/api/account/`, AppAccountApi.parse);
    }

    public static create(account: AppAccount): Promise<Loading<number>> {
        const parms: URLSearchParams = new URLSearchParams();
        parms.set("name", account.name);
        parms.set("discordID", account.discordID + "");

        return AppAccountApi.get().postReply(`/api/account/create?${parms.toString()}`, (elem: any) => elem);
    }

    public static deactivate(accountID: number): Promise<Loading<void>> {
        return AppAccountApi.get().delete(`/api/account/${accountID}`);
    }

}
