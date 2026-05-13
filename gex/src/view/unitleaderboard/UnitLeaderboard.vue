
<template>
    <div>

        <h2 class="wt-header">
            Unit leaderboard
        </h2>

        <div class="row mb-3">
            <div class="col-12">
                <toggle-button v-model="showAllOptions">
                    Show all options
                </toggle-button>
            </div>
        </div>

        <div class="row mb-3">
            <div class="col-12">
                <label>Units</label>

                <div v-if="unitNames.state == 'loaded'">
                    <input v-model="addUnitInput" @keyup.enter="addUnitWrapper" class="form-control" list="unit-names" autocomplete="on" @change="addUnitWrapper" @input="addUnitInputWrapper"/>
                    <datalist id="unit-names" @select="addUnitWrapper">
                        <option v-for="unitName in unitNames.data" :key="unitName.definitionName" :value="unitName.definitionName">
                            {{ unitName.disambiguatedName }}
                        </option>
                    </datalist>
                </div>
            </div>

            <div class="col-12">
                <label>Selected units:</label>

                <span v-if="unitDefs.length == 0">
                    No units selected!
                </span>

                <div>
                    <button v-for="unitDef in unitDefs" :key="unitDef.definitionName" class="btn btn-primary me-3" @click="removeUnitDef(unitDef.definitionName)">
                        &times; {{ unitDef.disambiguatedName }}
                    </button>
                </div>
            </div>
        </div>

        <div v-if="showAllOptions">
            <div class="row mb-3">
                <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                    <label><b>Range start</b></label>
                    <date-time-input v-model="periodStart" :allow-null="true"></date-time-input>
                </div>

                <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                    <label><b>Range end</b></label>
                    <date-time-input v-model="periodEnd" :allow-null="true"></date-time-input>
                </div>

                <div class="col-lg-6 col-md-4 col-sm-12 col-12 align-content-end">
                    <button class="btn btn-primary" @click="setRange('7d')">
                        Last week
                    </button>

                    <button class="btn btn-primary" @click="setRange('30d')">
                        Last month
                    </button>

                    <button class="btn btn-primary" @click="setRange('90d')">
                        Last 3 months
                    </button>

                    <button class="btn btn-primary" @click="setRange('all_time')">
                        All time
                    </button>
                </div>
            </div>

            <div class="row mb-3">
                <div class="col-12">
                    <label class="w-100"><b>Gamemodes</b></label>

                    <toggle-button v-model="gamemode.duel">
                        Duel
                    </toggle-button>

                    <toggle-button v-model="gamemode.smallTeam">
                        Small team
                    </toggle-button>

                    <toggle-button v-model="gamemode.largeTeam">
                        Large team
                    </toggle-button>

                    <toggle-button v-model="gamemode.ffa">
                        FFA 
                    </toggle-button>

                    <toggle-button v-model="gamemode.teamFfa">
                        Team FFA
                    </toggle-button>
                </div>

                <div class="col-12">
                    <span v-if="gamemodes.length == 0">
                        Including matches from all gamemodes
                    </span>
                    <span v-else>
                        Including matches from: 
                        <span v-for="gm in gamemodes" :key="gm">
                            {{ gm | gamemode }}
                        </span>
                    </span>
                </div>
            </div>
        </div>

        <div v-if="unitDefs.length > 0">
            <hr class="border">

            <button class="btn btn-primary w-100" @click="searchWrapper" :disabled="!canSearch">
                Load
            </button>

            <div class="text-center">
                <div v-for="(err, index) in validationErrors" :key="index">
                    {{ err }}
                </div>
            </div>
        </div>

        <hr class="border">

        <div v-if="leaderboard.state != 'idle'" class="mb-3">
            Showing top <code>{{ previousSearch.limit }}</code> users
            with matches between <code>{{ previousSearch.periodStart | moment }}</code> and <code>{{ previousSearch.periodEnd | moment }}</code>
            (a duration of {{ (previousSearch.periodEnd.getTime() - previousSearch.periodStart.getTime()) / 1000 | mduration }}),

            <span v-if="previousSearch.gamemodes && previousSearch.gamemodes.length > 0">
                that were a {{ previousSearchGamemodes }}
            </span>

            <span v-if="previousSearch.mapsFilenames && previousSearch.mapsFilenames.length > 0">
                maps
            </span>

            who created any of: <code v-for="ud in previousSearch.defNames" :key="ud">{{ ud }} </code>
        </div>

        <div v-if="leaderboard.state != 'idle'">
            <a-table :entries="leaderboard"
                default-sort-order="desc" default-sort-field="count">

                <a-col>
                    <a-header>
                        <b>User</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <a :href="'/user/' + entry.userID" target="_blank" ref="nofollow">
                            {{ entry.username }}
                        </a>
                    </a-body>
                </a-col>

                <a-col sort-field="count">
                    <a-header>
                        <b>Count</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.count }}
                    </a-body>
                </a-col>

            </a-table>

            <hr class="border">
        </div>

        <span>
            This data is updated every 4 hours, and only includes games that are fully simulated. <a href="/faq#which-games">More info.</a>
        </span>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import * as luxon from "luxon";

    import DateTimeInput from "components/DateTimeInput.vue";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import ApiError from "components/ApiError";
    import ToggleButton from "components/ToggleButton";

    import { BarUnitApi, UserUnitsMadeLeaderboardOptions } from "api/BarUnitApi";

    import { UserUnitsMaderLeaderboardEntry } from "model/UserUnitsMadeLeaderboardEntry";
    import { BarUnitName } from "model/BarUnitName";

    import { GamemodeUtil } from "util/Gamemode";

    import "filters/MomentFilter";
    import "filters/BarGamemodeFilter";

    export const UnitLeaderboard = Vue.extend({
        props: {

        },

        data: function() {
            return {
                leaderboard: Loadable.idle() as Loading<UserUnitsMaderLeaderboardEntry[]>,

                unitNames: Loadable.idle() as Loading<BarUnitName[]>,
                addUnitInput: "" as string,

                showAllOptions: false as boolean,

                unitDefs: [] as BarUnitName[],
                periodStart: new Date() as Date,
                periodEnd: new Date() as Date,

                mapFilenames: [] as string[],

                gamemode: {
                    all: true as boolean,
                    duel: false as boolean,
                    smallTeam: false as boolean,
                    largeTeam: false as boolean,
                    ffa: false as boolean,
                    teamFfa: false as boolean
                },

                previousSearch: {
                    defNames: [] as string[]
                } as UserUnitsMadeLeaderboardOptions
            }
        },

        mounted: function(): void {
            document.title = "Gex / Unit leaderboard";

            this.loadUnitNames();

            const lx: luxon.DateTime = luxon.DateTime.utc().startOf("day").plus(luxon.Duration.fromDurationLike({ days: 1 }));
            this.periodEnd = lx.toJSDate();
            this.periodStart = lx.minus(luxon.Duration.fromDurationLike({ month: 1 })).toJSDate();

            const search: URLSearchParams = new URLSearchParams(location.search);
            if (search.has("search") && search.get("search") != "") {
                const b64: string = search.get("search")!;
                const blob = JSON.parse(atob(b64));
                if (blob.periodStart) {
                    blob.periodStart = new Date(blob.periodStart);
                }
                if (blob.periodEnd) {
                    blob.periodEnd = new Date(blob.periodEnd);
                }
                console.log(`UserLeaderboard> loading search from query parameters: ${JSON.stringify(blob)}`);
                this.previousSearch = blob;
                this.search(blob);
            }
        },

        methods: {

            loadUnitNames: async function(): Promise<void> {
                this.unitNames = Loadable.loading();
                this.unitNames = await BarUnitApi.getAll();

                if (this.unitNames.state == "loaded" && this.previousSearch.defNames.length > 0 && this.unitDefs.length == 0) {
                    const prevNameSet: Set<string> = new Set(this.previousSearch.defNames);
                    this.unitDefs = this.unitNames.data.filter(iter => prevNameSet.has(iter.definitionName));
                }
            },

            removeUnitDef: function(defName: string): void {
                this.unitDefs = this.unitDefs.filter(iter => iter.definitionName != defName);
            },

            searchWrapper: async function(): Promise<void> {
                this.previousSearch = {
                    defNames: this.unitDefs.map(iter => iter.definitionName),
                    periodEnd: this.periodEnd,
                    periodStart: this.periodStart,
                    limit: 50,
                    offset: 0,
                    gamemodes: this.gamemodes.length == 0 ? undefined : this.gamemodes,
                    mapsFilenames: this.mapFilenames.length == 0 ? undefined : this.mapFilenames,
                };

                this.leaderboard = Loadable.loading();
                this.leaderboard = await BarUnitApi.getLeaderboard(this.previousSearch);

                const url = new URL(location.href);
                history.pushState({ path: url.href }, "", `/unitleaderboard?${this.searchParam}`);
            },

            search: async function(options: UserUnitsMadeLeaderboardOptions): Promise<void> {
                this.leaderboard = Loadable.loading();
                this.leaderboard = await BarUnitApi.getLeaderboard(options);
            },

            addUnitWrapper: function(): void {
                if (this.unitNames.state != "loaded") {
                    console.warn(`UnitLeaderboard> cannot add ${this.addUnitInput} to unit defs, unitNames is not loaded`);
                    return;
                }

                const unitDef: BarUnitName | undefined = this.unitNames.data.find(iter => iter.definitionName == this.addUnitInput);
                if (unitDef == undefined) {
                    console.warn(`UnitLeaderboard> cannot add ${this.addUnitInput} to unit defs, failed to find a unitName with that def name`);
                    return;
                }

                this.unitDefs.push(unitDef);
                this.addUnitInput = "";
            },

            addUnitInputWrapper: function(ev: InputEvent): void {
                if (ev.inputType == "insertText") {
                    return;
                }

                const input = ev.target as HTMLInputElement;
                const val = input.value;

                console.log(`UnitLeaderboard> input wrapper ${val} ${ev.inputType}`);

                if (ev.inputType != "insertReplacementText") {
                    return;
                }
                this.addUnitWrapper();
            },

            setRange: function(duration: string): void {
                const now: Date = new Date(Date.now());
                const lx: luxon.DateTime = luxon.DateTime.fromJSDate(now).startOf("day").plus(luxon.Duration.fromDurationLike({ days: 1 }));

                this.periodEnd = new Date();
                if (duration == "all_time") {
                    this.periodStart = new Date(2010, 0, 0);
                } else if (duration == "7d") {
                    this.periodStart = lx.minus(luxon.Duration.fromDurationLike({ days: 7 })).toJSDate();
                } else if (duration == "30d") {
                    this.periodStart = lx.minus(luxon.Duration.fromDurationLike({ months: 1 })).toJSDate();
                } else if (duration == "90d") {
                    this.periodStart = lx.minus(luxon.Duration.fromDurationLike({ months: 3 })).toJSDate();
                } else {
                    throw `UnitLeaderboard> unchecked duration ${duration}`;
                }
            }

        },

        computed: {

            gamemodes: function(): number[] {
                let ret: number[] = [];

                if (this.gamemode.duel == true) {
                    ret.push(GamemodeUtil.DUEL);
                }
                if (this.gamemode.smallTeam == true) {
                    ret.push(GamemodeUtil.SMALL_TEAM);
                }
                if (this.gamemode.largeTeam == true) {
                    ret.push(GamemodeUtil.LARGE_TEAM);
                }
                if (this.gamemode.ffa == true) {
                    ret.push(GamemodeUtil.FFA);
                }
                if (this.gamemode.teamFfa == true) {
                    ret.push(GamemodeUtil.TEAM_FFA);
                }

                return ret;
            },

            previousSearchGamemodes: function(): string {
                if ((this.previousSearch.gamemodes?.length ?? 0) <= 0) {
                    return "";
                }

                return this.previousSearch.gamemodes!.map(iter => GamemodeUtil.getName(iter)).join(", ");
            },

            searchParam: function(): string {
                return `search=${btoa(JSON.stringify(this.previousSearch))}`;
            },

            canSearch: function(): boolean {
                return this.validationErrors.length == 0;
            },

            validationErrors: function(): string[] {
                let ret: string[] = [];

                if (this.unitDefs.length == 0) {
                    ret.push(`Include at least 1 unit`);
                }

                return ret;
            }
        },

        components: {
            DateTimeInput, ApiError, ToggleButton,
            ATable, AHeader, ABody, AFooter, AFilter, ACol
        }
    });

    export default UnitLeaderboard;

</script>