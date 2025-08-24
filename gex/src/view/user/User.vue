
<template>
    <div>
        <div class="container">
            <div>
                <h2 class="wt-header bg-light text-dark">User info</h2>

                <div v-if="user.state == 'idle'"></div>

                <div v-else-if="user.state == 'loading'">
                    <busy class="busy busy-sm"></busy>
                    Loading...
                </div>

                <div v-else-if="user.state == 'loaded'">
                    <user-info :user="user.data"></user-info>
                </div>
            </div>

            <div>
                <h2 class="wt-header bg-light text-dark">Recent matches</h2>

                <div v-if="matches.state == 'idle'"></div>
                
                <div v-else-if="matches.state == 'loading'">
                    <busy class="busy busy-sm"></busy>
                    Loading...
                </div>

                <div v-else-if="matches.state == 'loaded'">
                    <user-matches :data="matches.data" :user-id="userID"></user-matches>
                </div>

                <div v-else-if="matches.state == 'error'">
                    <api-error :error="matches.problem"></api-error>
                </div>

                <div v-else>
                    unchecked state of matches: {{ matches.state }}
                </div>
            </div>

            <div v-if="user.state == 'loaded'">
                <user-units-made :user="user.data"></user-units-made>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading"

    import "filters/MomentFilter";
    import "filters/DurationFilter";

    import { GexMenu } from "components/AppMenu";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol, ARank, ATableType } from "components/ATable";
    import ApiError from "components/ApiError";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";
    import Busy from "components/Busy.vue";

    import UserMatches from "./components/UserMatches.vue";
    import UserInfo from "./components/UserInfo.vue";
    import UserUnitsMade from "./components/UserUnitsMade.vue";

    import { BarMatchApi } from "api/BarMatchApi";
    import { BarUserApi } from "api/BarUserApi";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarUser } from "model/BarUser";

    export const User = Vue.extend({
        props: {

        },

        data: function() {
            return {
                userID: 0 as number,
                matches: Loadable.idle() as Loading<BarMatch[]>,
                user: Loadable.idle() as Loading<BarUser>
            };
        },

        created: function(): void {
            this.userID = Number.parseInt(location.pathname.split("/")[2]);
        },

        beforeMount: function(): void {
            this.loadMatches();
            this.loadUser();
        },

        methods: {
            loadUser: async function(): Promise<void> {
                this.user = Loadable.loading();
                this.user = await BarUserApi.getByUserID(this.userID);

                if (this.user.state == "loaded") {
                    document.title = `Gex / User / ${this.user.data.username}`;
                    const url = new URL(location.href);
                    url.searchParams.set("name", this.user.data.username);

                    history.replaceState({ path: url.href }, "", `/user/${this.userID}/?${url.searchParams.toString()}`);
                }
            },

            loadMatches: async function(): Promise<void> {
                this.matches = Loadable.loading();
                this.matches = await BarMatchApi.getByUserID(this.userID);
            },

            isFFA: function(match: BarMatch): boolean {
                return match.allyTeams.length > 2 && Math.max(...match.allyTeams.map(iter => iter.playerCount)) == 1;
            },
        },

        computed: {

            mostRecentMatch: function(): BarMatch | null {
                if (this.matches.state != "loaded") {
                    return null;
                }

                if (this.matches.data.length == 0) {
                    return null;
                }

                return this.matches.data[0];
            },

            mostRecentMatchPlayer: function(): BarMatchPlayer | null {
                if (this.mostRecentMatch == null) {
                    return null;
                }

                return this.mostRecentMatch.players.find(iter => iter.userID == this.userID) ?? null;
            }

        },

        watch: {

        },

        components: {
            GexMenu, InfoHover, ApiError, ToggleButton, Busy,
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            UserMatches, UserInfo, UserUnitsMade
        }
    });
    export default User;
</script>