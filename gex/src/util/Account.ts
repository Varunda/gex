import { AppGroupPermission } from "model/account/AppGroupPermission";

export class AppCurrentAccount {
    public ID: number = 0;
    public name: string = "";
    public permissions: AppGroupPermission[] = [];
}

export default class AccountUtil {

    public static isLoggedIn(): boolean {
        return AccountUtil.get().name != "";
    }

    public static get(): AppCurrentAccount {
        return (window as any).appCurrentAccount;
    }

    public static getAccountName(): string {
        return AccountUtil.get().name;
    }

    public static hasPermission(name: string): boolean {
        const acc: AppCurrentAccount = AccountUtil.get();
        return acc.ID == 1 || acc.permissions.find(iter => iter.permission.toLowerCase() == name.toLowerCase()) != undefined;
    }

}
