import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { AppAccountGroupMembership } from "model/account/AppAccountGroupMembership";

export class AppAccountGroupMembershipApi extends ApiWrapper<AppAccountGroupMembership> {
    private static _instance: AppAccountGroupMembershipApi = new AppAccountGroupMembershipApi();
    public static get(): AppAccountGroupMembershipApi { return AppAccountGroupMembershipApi._instance; }

    public static getByAccountID(accountID: number): Promise<Loading<AppAccountGroupMembership[]>> {
        return AppAccountGroupMembershipApi.get().readList(`/api/group-membership/account/${accountID}`, AppAccountGroupMembership.parse);
    }

    public static getByGroupID(groupID: number): Promise<Loading<AppAccountGroupMembership[]>> {
        return AppAccountGroupMembershipApi.get().readList(`/api/group-membership/group/${groupID}`, AppAccountGroupMembership.parse);
    }

    public static addUserToGroup(groupID: number, accountID: number): Promise<Loading<void>> {
        return AppAccountGroupMembershipApi.get().post(`/api/group-membership/${groupID}/${accountID}`);
    }

    public static removeUserFromGroup(groupID: number, accountID: Number): Promise<Loading<void>> {
        return AppAccountGroupMembershipApi.get().delete(`/api/group-membership/${groupID}/${accountID}`);
    }

}
