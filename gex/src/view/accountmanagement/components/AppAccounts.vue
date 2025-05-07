<template>
    <div>
        <div class="row">
            <div class="col-6">
                <h4 class="wt-header">
                    Accounts

                    <span class="btn-group">
                        <button class="btn btn-primary btn-sm btn-outline-dark" @click="sort = 'name'">
                            sort name
                        </button>
                        <button class="btn btn-primary btn-sm btn-outline-dark" @click="sort = 'id'">
                            sort id
                        </button>
                    </span>
                </h4>

                <div v-if="accounts.state == 'loaded'" class="list-group">
                    <div v-for="account in sortedAccounts" :key="account.id" class="list-group-item" @click="selectAccount(account.id)"
                         :class="{ 'list-group-item-primary': selected.account != null && selected.account.id == account.id }">

                        {{account.name}}
                        <span class="text-muted">
                            ({{account.id}})
                        </span>
                    </div>
                </div>

                <div class="border-top pt-3 mt-3">
                    <div>
                        <div class="form-floating">
                            <input v-model="create.name" type="text" class="form-control" />
                            <label>name</label>
                        </div>

                        <div class="form-floating">
                            <input v-model="create.discordID" type="number" class="form-control" />
                            <label>discord ID</label>
                        </div>

                        <button class="btn btn-primary" type="button" @click="createAccount">create</button>
                    </div>
                </div>
            </div>

            <div class="col-6">
                <div class="wt-header">
                    groups for 
                    <span v-if="selected.account != null">
                        {{selected.account.name}} / {{selected.account.id}}
                    </span>
                    <span v-else class="text-muted">
                        no account selected
                    </span>
                </div>

                <div v-if="selected.groups.state == 'loaded'">
                    <div class="list-group mb-3">
                        <div class="list-group-item list-group-item-primary">
                            groups
                        </div>

                        <div v-for="group in groupsOfAccount" :key="group.groupID" class="list-group-item d-flex" @click="removeUserFromGroup(group.groupID)">
                            <span class="flex-grow-1">
                                {{group.name}}
                            </span>
                            <span class="bi-x"></span>
                        </div>

                        <div v-if="groupsOfAccount.length == 0" class="list-group-item text-muted">
                            account is in no groups!
                        </div>
                    </div>

                    <div class="list-group">
                        <div class="list-group-item list-group-item-success">
                            groups that can be added
                        </div>

                        <div v-for="group in groupsNotHadByUser" :key="group.groupID" class="list-group-item d-flex" @click="addUserToGroup(group.groupID)">
                            <span class="flex-grow-1">
                                {{group.name}}
                                <span class="text-muted">
                                    ({{group.groupID}})
                                </span>
                            </span>
                            <span class="bi-plus"></span>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppAccountApi } from "api/account/AppAccountApi";
    import { AppAccountGroupMembershipApi } from "api/account/AppAccountGroupMembershipApi";
    import { AppGroupApi } from "api/account/AppGroupApi";

    import EventBus from "EventBus";
    import Toaster from "Toaster";

    import { AppAccount } from "model/account/AppAccount";
    import { AppGroup } from "model/account/AppGroup";
    import { AppAccountGroupMembership } from "model/account/AppAccountGroupMembership";

    interface NamedGroup {
        groupID: number;
        name: string;
    }

    export const AppAccounts = Vue.extend({
        props: {

        },

        data: function() {
            return {
                accounts: Loadable.idle() as Loading<AppAccount[]>,
                groups: Loadable.idle() as Loading<AppGroup[]>,

                selected: {
                    account: null as AppAccount | null,
                    groups: Loadable.idle() as Loading<AppAccountGroupMembership[]>
                },

                sort: "name" as "name" | "id",

                create: {
                    name: "" as string,
                    discordID: "" as string
                }
            }
        },

        mounted: function() {
            this.loadAccounts();
            this.loadGroups();
        },

        methods: {
            loadAccounts: async function(): Promise<void> {
                this.selected.account = null;
                this.selected.groups = Loadable.idle();

                this.accounts = Loadable.loading();
                this.accounts = await AppAccountApi.getAll();
            },

            loadGroups: async function(): Promise<void> {
                this.groups = Loadable.loading();
                this.groups = await AppGroupApi.getAll();
            },

            selectAccount: async function(accountID: number): Promise<void> {
                if (this.accounts.state != "loaded") {
                    console.warn(`cannot select account ${accountID}: this.accounts.state is ${this.accounts.state}, not "loaded"`);
                    return;
                }

                this.selected.account = this.accounts.data.find(iter => iter.id == accountID) ?? null;

                this.selected.groups = Loadable.loading();
                this.selected.groups = await AppAccountGroupMembershipApi.getByAccountID(accountID);
            },

            selectGroup: async function(groupID: number): Promise<void> {
                EventBus.$emit("select-group", groupID);
            },

            createAccount: async function(): Promise<void> {
                const l: Loading<number> = await AppAccountApi.create(this.create.name, this.create.discordID);
                if (l.state == "loaded") {
                    Toaster.add("user created", `successfully created account for ${this.create.name}`, "success");
                    this.loadAccounts();
                    this.loadGroups();
                } else if (l.state == "error") {
                    Loadable.toastError(l, "failed to create account");
                } else {
                    console.error(`unchecked state of response: ${l.state}`);
                }
            },

            addUserToGroup: async function(groupID: number): Promise<void> {
                if (this.selected.account == null) {
                    console.warn(`cannot add user to group ${groupID}: selected.account is null`);
                    return;
                }
                const accountID: number = this.selected.account.id;

                const l: Loading<void> = await AppAccountGroupMembershipApi.addUserToGroup(groupID, accountID);
                if (l.state == "loaded") {
                    Toaster.add(`user added`, `user added to group!`, "success");
                    if (this.selected.account != null) {
                        this.selectAccount(this.selected.account.id);
                    }
                } else if (l.state == "error") {
                    Loadable.toastError(l, "failed to add user");
                } else {
                    console.error(`unchecked state of response: ${l.state}`);
                }
            },

            removeUserFromGroup: async function(groupID: number): Promise<void> {
                if (this.selected.account == null) {
                    console.warn(`cannot remove user from group ${groupID}: selected.account is null`);
                    return;
                }
                const accountID: number = this.selected.account.id;

                const l: Loading<void> = await AppAccountGroupMembershipApi.removeUserFromGroup(groupID, accountID);
                if (l.state == "loaded") {
                    Toaster.add(`user removed`, `user removed from group!`, "success");
                    if (this.selected.account != null) {
                        this.selectAccount(this.selected.account.id);
                    }
                } else if (l.state == "error") {
                    Loadable.toastError(l, "failed to remove user");
                } else {
                    console.error(`unchecked state of response: ${l.state}`);
                }
            }
        },

        computed: {

            sortedAccounts: function(): AppAccount[] {
                if (this.accounts.state != "loaded") {
                    return [];
                }

                return [...this.accounts.data].sort((a, b) => {
                    if (this.sort == "id") {
                        return a.id - b.id;
                    } else if (this.sort == "name") {
                        return a.name.localeCompare(b.name);
                    } else {
                        throw `unchecked sort value '${this.sort}'`;
                    }
                });
            },

            groupsOfAccount: function(): NamedGroup[] {
                if (this.selected.groups.state != "loaded" || this.groups.state != "loaded") {
                    return [];
                }

                const map: Map<number, AppGroup> = this.groupMap;
                return this.selected.groups.data.map((iter: AppAccountGroupMembership) => {
                    return {
                        groupID: iter.groupID,
                        name: map.get(iter.groupID)?.name ?? `<missing ${iter.groupID}>`
                    }
                });
            },

            groupsNotHadByUser: function(): NamedGroup[] {
                if (this.selected.groups.state != "loaded" || this.groups.state != "loaded") {
                    return [];
                }

                let groups: Set<number> = new Set(this.groups.data.map(iter => iter.id));
                for (const group of this.selected.groups.data) {
                    groups.delete(group.groupID);
                }

                const map: Map<number, AppGroup> = this.groupMap;
                return Array.from(groups).map((iter: number) => {
                    return {
                        groupID: iter,
                        name: map.get(iter)?.name ?? `<missing ${iter}>`
                    }
                });
            },

            groupMap: function(): Map<number, AppGroup> {
                if (this.groups.state != "loaded") {
                    return new Map();
                }

                const map: Map<number, AppGroup> = new Map();

                for (const group of this.groups.data) {
                    map.set(group.id, group);
                }

                return map;
            }
        },

        components: {

        }
    });
    export default AppAccounts;

</script>