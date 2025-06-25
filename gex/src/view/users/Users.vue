
<template>
    <div>
        <div class="container">
            <h1>User search</h1>

            <input class="form-control mb-2" type="text" v-model="searchTerm" @keyup.enter="searchWrapper" placeholder="Press enter to search (minimum 3 characters)">

            <div class="mb-2 form-check">
                <input type="checkbox" class="form-check-input" id="search-previous-names-toggle" v-model="searchPreviousNames">
                <label class="form-check-label" for="search-previous-names-toggle">Include name changes</label>
            </div>

            <button class="btn btn-primary" @click="searchWrapper">
                Search
            </button>

            <hr class="border">

            <h2 v-show="searchedValue.length > 0">
                Search results for: 
                <strong>
                    {{ searchedValue }}
                </strong>
            </h2>

            <div v-if="users.state == 'idle'"></div>

            <div v-else-if="users.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="users.state == 'loaded'">

                <a-table :entries="users"
                    default-sort-field="searchDiff" default-sort-order="asc">

                    <a-col sort-field="username">
                        <a-header>
                            <b>Username</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <a :href="'/user/' + entry.userID">
                                <span v-if="entry.username != entry.previousName">
                                    {{ entry.previousName }}
                                    &rarr;
                                </span>

                                {{ entry.username }}
                            </a>
                        </a-body>
                    </a-col>

                    <a-col sort-field="highestEloValue">
                        <a-header>
                            <b>Highest OS</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span v-if="entry.highestElo == null">
                                --
                            </span>

                            <span v-else>
                                <span class="font-monospace">
                                    {{ entry.highestElo.skill | locale(2, 2) }}&plusmn;{{ entry.highestElo.skillUncertainty | locale(2) }}
                                </span>

                                <span class="text-muted">
                                    (in {{ entry.highestElo.gamemode | gamemode }})
                                </span>
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="avgEloValue">
                        <a-header>
                            <b>Avg OS</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span class="font-monospace">
                                {{ entry.avgEloStr }}
                            </span>
                            <span class="text-muted">
                                (over {{ entry.skill.length }} gamemodes)
                            </span>
                        </a-body>
                    </a-col>

                    <a-col>
                        <a-header>
                            <b>View</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <a :href="'/user/' + entry.userID">View</a>
                        </a-body>
                    </a-col>

                </a-table>

            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading"

    import "filters/MomentFilter";
    import "filters/DurationFilter";
    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";

    import { GexMenu } from "components/AppMenu";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import ApiError from "components/ApiError";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";

    import { BarUserApi } from "api/BarUserApi";

    import { BarUser } from "model/BarUser";
    import { BarUserSkill } from "model/BarUserSkill";
    import { UserSearchResult } from "model/UserSearchResult";

    import { GamemodeUtil } from "util/Gamemode";
    import LocaleUtil from "util/Locale";
    import StringDistance from "util/StringDistance";

    type ExpandedUser = {
        userID: number;
        username: string;
        lastUpdated: Date;
        previousName: string;

        searchDiff: number;

        skill: BarUserSkill[];

        highestElo: BarUserSkill | null;
        highestEloValue: number;
        avgEloStr: string;
        avgEloValue: number;
    }

    export const Users = Vue.extend({
        props: {

        },

        data: function() {
            return {
                searchTerm: "" as string,
                searchedValue: "" as string,
                searchPreviousNames: false as boolean,
                users: Loadable.idle() as Loading<ExpandedUser[]>
            };
        },

        created: function(): void {
            document.title = "Gex / User search";
        },

        beforeMount: function(): void {

        },

        methods: {

            searchWrapper: function(): void {
                if (this.searchTerm.length < 3) {
                    return;
                }

                this.search(this.searchTerm);
            },

            search: async function(term: string): Promise<void> {
                this.users = Loadable.loading();

                const ret: Loading<UserSearchResult[]> = await BarUserApi.search(this.searchTerm, this.searchPreviousNames, true);

                if (ret.state != "loaded") {
                    this.users = Loadable.rewrap(ret);
                    return;
                }

                this.users = Loadable.loaded(ret.data.map(iter => {
                    const searchDiff: number = StringDistance.calculate(iter.username, term);

                    if (iter.skill.length == 0) {
                        return {
                            ...iter,
                            searchDiff: searchDiff,
                            highestElo: null,
                            highestEloValue: 0,
                            avgEloStr: "--",
                            avgEloValue: 0
                        };
                    }

                    const highest: BarUserSkill | null = [...iter.skill.sort((a, b) => b.skill - a.skill)][0];

                    const count: number = iter.skill.length;

                    const skill: number = iter.skill.reduce((acc, iter) => acc += iter.skill, 0) / count;
                    const uncertain: number = iter.skill.reduce((acc, iter) => acc += iter.skillUncertainty, 0) / count;

                    return {
                        ...iter,
                        searchDiff: searchDiff,
                        highestElo: highest,
                        highestEloValue: highest.skill,
                        avgEloStr: `${LocaleUtil.locale(skill, 2, 2)}±${LocaleUtil.locale(uncertain, 2)}`,
                        avgEloValue: skill
                    };
                }));

                this.searchedValue = term;
            },
        },

        computed: {

        },

        watch: {

        },

        components: {
            GexMenu, InfoHover, ApiError, ToggleButton,
            ATable, AHeader, ABody, AFooter, AFilter, ACol
        }
    });
    export default Users;
</script>