
<template>
    <div class="container remove-container-padding">

        <div v-if="user.state == 'idle'"></div>

        <div v-else-if="user.state == 'loading' || matches.state == 'loading'" class="text-center">
            Loading...
            <busy class="busy busy-sm"></busy>

            <table class="table table-sm">
                <tbody>
                    <tr>
                        <td>User</td>
                        <td>{{ user.state }}</td>
                    </tr>
                    <tr>
                        <td>Matches</td>
                        <td>{{ matches.state }}</td>
                    </tr>
                </tbody>
            </table>
        </div>

        <div v-else-if="user.state == 'nocontent'">
            <div class="alert alert-danger">
                No user with ID {{ userID }} exists
            </div>
        </div>

        <div v-else-if="user.state == 'loaded' && matches.state == 'loaded'">

            <user-header :user="user.data"></user-header>

            <ul class="nav nav-tabs border-bottom-0 justify-content-center">
                <li class="nav-item" @click="selectTab('overview')">
                    <a class="nav-link border" :class=" [ selectedTab == 'overview' ? 'bg-light text-dark fw-bold border-bottom-0' : 'text-light' ]">
                        Overview
                    </a>
                </li>
                <li class="nav-item" @click="selectTab('faction')">
                    <a class="nav-link border" :class=" [ selectedTab == 'faction' ? 'bg-light text-dark fw-bold border-bottom-0' : 'text-light' ]">
                        Faction stats
                    </a>
                </li>
                <li class="nav-item" @click="selectTab('charts')">
                    <a class="nav-link border" :class=" [ selectedTab == 'charts' ? 'bg-light text-dark fw-bold border-bottom-0' : 'text-light' ]">
                        Charts
                    </a>
                </li>
                <li class="nav-item" @click="selectTab('names')">
                    <a class="nav-link border" :class=" [ selectedTab == 'names' ? 'bg-light text-dark fw-bold border-bottom-0' : 'text-light' ]">
                        Previous names
                    </a>
                </li>
                <li class="nav-item" @click="selectTab('map')">
                    <a class="nav-link border" :class=" [ selectedTab == 'map' ? 'bg-light text-dark fw-bold border-bottom-0' : 'text-light' ]">
                        Map stats
                    </a>
                </li>
                <li class="nav-item" @click="selectTab('matches')">
                    <a class="nav-link border" :class=" [ selectedTab == 'matches' ? 'bg-light text-dark fw-bold border-bottom-0' : 'text-light' ]">
                        Match history
                    </a>
                </li>
                <li class="nav-item" @click="selectTab('encounters')">
                    <a class="nav-link border" :class=" [ selectedTab == 'encounters' ? 'bg-light text-dark fw-bold border-bottom-0' : 'text-light' ]">
                        Player encounters
                    </a>
                </li>
                <li class="nav-item" @click="selectTab('units')">
                    <a class="nav-link border" :class=" [ selectedTab == 'units' ? 'bg-light text-dark fw-bold border-bottom-0' : 'text-light' ]">
                        Units created
                    </a>
                </li>
            </ul>

            <keep-alive>
                <component :is="selectedComponent" :user="user.data" :matches="matches.data"></component>
            </keep-alive>
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

    import UserHeader from "./components/UserHeader.vue";
    import UserMatches from "./components/UserMatches.vue";
    import UserInfo from "./components/UserInfo.vue";
    import UserUnitsMade from "./components/UserUnitsMade.vue";
    import UserInteractions from "./components/UserInteractions.vue";
    import UserCharts from "./components/UserCharts.vue";
    import UserOverview from "./components/UserOverview.vue";
    import UserFactionStats from "./components/UserFactionStats.vue";
    import UserMaps from "./components/UserMaps.vue";
    import UserPreviousNames from "./components/UserPreviousNames.vue";

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
                user: Loadable.idle() as Loading<BarUser>,

                selectedTab: "overview" as string,
                selectedComponent: "UserOverview" as string,
            };
        },

        created: function(): void {
        },

        beforeMount: function(): void {
            this.loadUser();
        },

        methods: {
            loadUser: async function(): Promise<void> {
                const parts: string[] = location.pathname.split("/");
                if (parts.length < 3) {
                    throw `User> invalid URL passed '${location.pathname}': expected at least 3 parts after split on '/'`;
                }

                if (parts[1] != "user") {
                    throw `User> expected 'user' in parts[1], got '${parts[1]}' instead`;
                }

                this.userID = Number.parseInt(parts[2]);
                this.loadMatches();

                this.user = Loadable.loading();
                this.user = await BarUserApi.getByUserID(this.userID);

                if (this.user.state == "loaded") {
                    document.title = `Gex / User / ${this.user.data.username}`;
                }

                if (parts.length >= 4 && parts[3] != "") {
                    console.log(`User> viewing tab '${parts[3]}'`);
                    this.selectTab(parts[3]);
                }
            },

            selectTab: function(tab: string): void {
                console.log(`User> selecting tab '${tab}'`);
                this.selectedTab = tab.toLowerCase();

                if (this.selectedTab == "overview") {
                    this.selectedComponent = "UserOverview";
                } else if (this.selectedTab == "faction") {
                    this.selectedComponent = "UserFactionStats";
                } else if (this.selectedTab == "matches") {
                    this.selectedComponent = "UserMatches";
                } else if (this.selectedTab == "encounters") {
                    this.selectedComponent = "UserInteractions";
                } else if (this.selectedTab == "map") {
                    this.selectedComponent = "UserMaps";
                } else if (this.selectedTab == "names") {
                    this.selectedComponent = "UserPreviousNames";
                } else if (this.selectedTab == "charts"){
                    this.selectedComponent = "UserCharts";
                } else if (this.selectedTab == "units"){
                    this.selectedComponent = "UserUnitsMade";
                } else {
                    throw `User> unhandled tab select '${this.selectedTab}'`;
                }

                const url = new URL(location.href);
                if (this.user.state == "loaded") {
                    url.searchParams.set("name", encodeURIComponent(this.user.data.username));
                }
                history.pushState({ path: url.href }, "", `/user/${this.userID}/${this.selectedTab}?${url.searchParams.toString()}`);
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
            UserHeader, UserMatches, UserInfo, UserUnitsMade, UserInteractions,
            UserCharts, UserOverview, UserFactionStats, UserMaps, UserPreviousNames,
        }
    });
    export default User;
</script>