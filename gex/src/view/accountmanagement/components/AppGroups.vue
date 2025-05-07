<template>
    <div>
        <div class="row">
            <div class="col-6">
                <h4 class="wt-header">
                    Groups
                </h4>

                <div v-if="groups.state == 'loaded'" class="list-group">
                    <div v-for="group in groups.data" :key="group.id" class="list-group-item" @click="selectGroup(group.id)">
                        <span :style="{ 'color': '#' + group.hexColor }">
                            {{group.name}}
                        </span>
                        <span class="text-muted">
                            ({{group.id}})
                        </span>
                    </div>

                    <div class="list-group-item list-group-item-dark">
                        <div class="input-group">
                            <input v-model="create.name" class="form-control" />
                            <button class="btn btn-primary" type="button" @click="createGroup">create</button>
                        </div>
                    </div>
                </div>
            </div>

            <div class="col-6">
                <div class="wt-header">
                    permissions for 
                    <span v-if="selected.group != null">
                        {{selected.group.name}} / {{selected.group.id}}
                    </span>
                    <span v-else class="text-muted">
                        no group selected
                    </span>
                </div>

                <div v-if="selected.permissions.state == 'loaded'">
                    <div class="list-group mb-3">
                        <div class="list-group-item list-group-item-primary">
                            granted permissions
                        </div>

                        <div v-for="perm in permissionsGranted" :key="perm.id" class="list-group-item d-flex" @click="removePermissionFromGroup(perm.id)">
                            <span class="flex-grow-1">
                                {{perm.description}}
                                <span class="text-muted">
                                    ({{perm.permission}})
                                </span>
                            </span>
                            <span class="bi-x"></span>
                        </div>

                        <div class="list-group-item list-group-item-dark">
                            {{selected.permissions.data.length}}
                            <span v-if="permissions.state == 'loaded'">
                                of {{permissions.data.length}}
                            </span>
                            permissions
                        </div>
                    </div>

                    <div class="list-group">
                        <div class="list-group-item list-group-item-success">
                            permission that can be granted
                        </div>

                        <div v-for="perm in permissionsNotGranted" :key="perm.id" class="list-group-item d-flex" @click="addPermissionToGroup(perm.id)">
                            <span class="flex-grow-1">
                                {{perm.description}}
                                <span class="text-muted">
                                    ({{perm.id}})
                                </span>
                            </span>
                            <span class="bi-plus"></span>
                        </div>

                        <div v-if="permissionsNotGranted.length == 0" class="list-group-item text-muted">
                            all permissions granted!
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

    import { AppGroupApi } from "api/account/AppGroupApi";
    import { AppGroupPermissionApi } from "api/account/AppGroupPermissionApi";
    import { AppPermissionApi } from "api/account/AppPermissionApi";

    import EventBus from "EventBus";

    import { AppGroup } from "model/account/AppGroup";
    import { AppPermission } from "model/account/AppPermission";
    import { AppGroupPermission } from "model/account/AppGroupPermission";

    interface NamedGroupPermission {
        id: number;
        permission: string;
        description: string;
    }

    export const AppGroups = Vue.extend({
        props: {

        },

        data: function() {
            return {
                groups: Loadable.idle() as Loading<AppGroup[]>,
                permissions: Loadable.idle() as Loading<AppPermission[]>,

                selected: {
                    group: null as AppGroup | null,
                    permissions: Loadable.idle() as Loading<AppGroupPermission[]>
                },

                create: {
                    name: "" as string
                }
            }
        },

        mounted: function() {
            this.loadPermissions();
            this.loadGroups();

            EventBus.$on("select-group", (groupID: number) => {
                console.log(`AppGroups> @select-group ${groupID}`);
                this.selectGroup(groupID);
            });
        },

        methods: {
            loadPermissions: async function(): Promise<void> {
                this.permissions = Loadable.loading();
                this.permissions = await AppPermissionApi.getAll();
            },

            loadGroups: async function(): Promise<void> {
                this.groups = Loadable.loading();
                this.selected.group = null;
                this.selected.permissions = Loadable.idle();

                this.groups = await AppGroupApi.getAll();
            },

            selectGroup: async function(groupID: number): Promise<void> {
                if (this.groups.state != "loaded") {
                    console.warn(`cannot select group ${groupID}: groups.state is "${this.groups.state}", not "loaded"`);
                    return;
                }

                console.log(`selecting group ${groupID}`);
                this.selected.group = this.groups.data.find(iter => iter.id == groupID) || null;
                this.selected.permissions = Loadable.loading();
                this.selected.permissions = await AppGroupPermissionApi.getByGroupID(groupID);
            },

            addPermissionToGroup: async function(perm: string): Promise<void> {
                if (this.selected.group == null) {
                    console.warn(`cannot add permission to group: no group is selected`);
                    return;
                }

                const l: Loading<number> = await AppGroupPermissionApi.insert(this.selected.group.id, perm);
                if (l.state == "loaded") {
                    this.selectGroup(this.selected.group.id);
                }
            },

            removePermissionFromGroup: async function(id: number): Promise<void> {
                await AppGroupPermissionApi.delete(id);
                if (this.selected.group != null) {
                    this.selectGroup(this.selected.group.id);
                }
            },

            createGroup: async function(): Promise<void> {
                if (this.create.name == "") {
                    return;
                }

                await AppGroupApi.create(this.create.name);
                this.loadGroups();
            }
        },

        computed: {
            permissionsGranted: function(): NamedGroupPermission[] {
                if (this.permissions.state != "loaded" || this.selected.permissions.state != "loaded") {
                    return [];
                }

                const perms: AppPermission[] = this.permissions.data;

                return this.selected.permissions.data.map(iter => {
                    return {
                        id: iter.id,
                        permission: iter.permission,
                        description: perms.find(i => i.id == iter.permission)?.description ?? ""
                    };
                });
            },

            permissionsNotGranted: function(): AppPermission[] {
                if (this.permissions.state != "loaded" || this.selected.permissions.state != "loaded") {
                    return [];
                }

                const permSet: Set<string> = new Set(this.selected.permissions.data.map(iter => iter.permission));

                return this.permissions.data.filter(iter => !permSet.has(iter.id));
            }

        },

        components: {

        }
    });
    export default AppGroups;

</script>
