<template>
    <div class="container">
        <div class="mb-3">
            <toggle-button v-model="showMapStats">show map stats</toggle-button>

            <toggle-button v-model="showPlayerStats">show player stats</toggle-button>

            <toggle-button v-if="hasAddRemovePermission" v-model="showPoolEdit">show edit</toggle-button>

            <button v-if="hasUpdatePriorityPermission" class="btn btn-secondary" @click="setPriorityToOne">
                set prio to 1
            </button>
        </div>

        <div v-if="showMapStats == true" class="d-flex flex-wrap mb-4 justify-content-center" style="gap: 1rem;">
            <div v-for="map in mapStats" :key="map.mapFilename" class="mb-1 text-start" style="height: 114px; width: 434px;">
                <div class="img-overlay max-width"></div>

                <div class="position-absolute max-width img-map-parent" style="z-index: 0; font-size: 0">
                    <div :style="mapBackground(map)" class="img-map-side img-map-left"></div>
                    <div :style="mapBackground(map)" class="img-map-center"></div>
                    <div :style="mapBackground(map)" class="img-map-side img-map-right"></div>
                </div>

                <div style="z-index: 10; position: relative; top: 50%; transform: translateY(-50%); left: 20px;">
                    <img :src="'/image-proxy/MapNameBackground?map=' + map.mapName + '&size=texture-thumb'" width="80" height="80" class="d-inline corner-img me-2"/>

                    <div class="d-inline-flex flex-column align-items-start text-outline" style="vertical-align: top;">
                        <h5 class="mb-0 text-outline2">
                            {{ mapNameWithoutVersion(map.mapName) }} - {{ map.plays }} play{{ map.plays == 1 ? '' : 's' }}
                        </h5>

                        <div>
                            <img src="/img/armada.png" width="24" height="24" title="Armada"/>
                            {{ map.armadaPlays }} pick{{ map.armadaPlays == 1 ? '' : 's' }}, {{ map.armadaWins }} win{{ map.armadaWins == 1 ? '' : 's' }} ({{ map.armadaWins / Math.max(1, map.armadaPlays) * 100 | locale(0) }}%)
                        </div>

                        <div>
                            <img src="/img/cortex.png" width="24" height="24" title="Cortex"/>
                            {{ map.cortexPlays }} pick{{ map.cortexPlays == 1 ? '' : 's' }}, {{ map.cortexWins }} win{{ map.cortexWins == 1 ? '' : 's' }} ({{ map.cortexWins / Math.max(1, map.cortexPlays) * 100 | locale(0) }}%)
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div v-if="showPlayerStats == true" class="d-flex flex-wrap mb-4 justify-content-center" style="gap: 1rem;">
            <div v-for="player in playerStats" :key="player.userID" class="mb-1 text-start" style="height: 114px; width: 360px;">
                <div class="img-overlay-player max-width border" :class="'img-overlay-' + player.factionPref"></div>

                <div class="position-absolute img-map-parent" style="z-index: 0; font-size: 0; max-width: 360px; background-color: #0004">
                    <img :src="'/img/banner/' + player.factionPref + '_large.jpg'" height="114px" style="transform: scaleX(-1) translateX(15%);">
                </div>

                <div class="" style="z-index: 10; position: relative; top: 50%; transform: translateY(-50%); left: 20px;">
                    <div class="d-flex flex-column align-items-start text-outline2" style="vertical-align: top;">
                        <h4 class="d-inline mb-2 text-outline2">
                            <img v-if="player.flag == undefined || player.flag == '??' || player.flag == 'ARM' || player.flag == 'COR'"
                                :src="'/img/' + player.factionPref + '.png'" width="24" height="24" class="d-inline"/>
                            <img v-else :src="'/img/flags/' + player.flag.toLowerCase() + '.png'" width="32" height="24" class="d-inline" :title="'country flag for ' + player.flag"/>
                            {{ player.username}} ({{ player.wins }} - {{ player.plays - player.wins }})
                        </h4>

                        <div class="d-grid" style="grid-template-columns: 1fr 1fr; row-gap: 0.5rem; column-gap: 1rem;">
                            <div>
                                <img src="/img/armada.png" width="24" height="24" title="Armada"/>
                                {{ player.armadaWins }} - {{ player.armadaPlays - player.armadaWins }}
                                ({{ player.armadaWins / Math.max(1, player.armadaPlays) * 100 | locale(0) }}%)
                            </div>
                            <div>
                                <img src="/img/random.png" width="24" height="24" title="Random"/>
                                {{ player.randomWins }} - {{ player.randomPlays - player.randomWins }}
                                ({{ player.randomWins / Math.max(1, player.randomPlays) * 100 | locale(0) }}%)
                            </div>
                            <div>
                                <img src="/img/cortex.png" width="24" height="24" title="Cortex"/>
                                {{ player.cortexWins }} - {{ player.cortexPlays - player.cortexWins }}
                                ({{ player.cortexWins / Math.max(1, player.cortexPlays) * 100 | locale(0) }}%)
                            </div>
                            <div v-if="player.legionPlays > 0">
                                <img src="/img/legion.png" width="24" height="24" title="Legion"/>
                                {{ player.legionWins }} - {{ player.legionPlays - player.legionWins }}
                                ({{ player.legionWins / Math.max(1, player.legionPlays) * 100 | locale(0) }}%)
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class="mb-3">
            <h2 class="wt-header bg-light text-dark">
                Pool: 
                <span v-if="pool.state == 'loaded'">
                    {{ pool.data.name }}
                </span>
            </h2>

            <div v-if="matches.state == 'idle'"></div>

            <div v-else-if="matches.state == 'loading'" class="text-center">
                <busy class="busy busy-sm"></busy>
                Loading...
            </div>

            <div v-else-if="matches.state == 'loaded'">
                <match-list :matches="matches.data"></match-list>

                <div v-if="matches.data.length == 0">
                    No matches found!
                </div>
            </div>

            <div v-else-if="matches.state == 'error'">
                <api-error :error="matches.problem"></api-error>
            </div>
        </div>

        <div v-if="showPoolEdit">
            <h2 class="wt-header bg-light text-dark">edit pool</h2>

            <label class="d-block">match IDs to add, newline seperate</label>
            <textarea v-model="addMatch" class="form-control"></textarea>
            <button class="btn btn-primary mt-2" @click="addMatchesToPool">add</button>

            <label class="d-block">remove matches</label>

            <div v-if="matches.state == 'loaded'">
                <table class="table table-sm table-border">
                    <tr v-for="match in matches.data" :key="match.id">
                        <td class="font-monospace">
                            {{ match.id }} 
                        </td>
                        <td>
                            <button class="btn btn-danger px-2 btn-sm" @click="removeMatchFromPool(match.id)">
                                &times;
                            </button>
                        </td>
                    </tr>
                </table>
            </div>

        </div>

    </div>
</template>

<style scoped>
    /* from: https://stackoverflow.com/a/61913549 by Temani Afif */
    .corner-img {
        --s: 8px; /* size on corner */
        --t: 1px; /* thickness of border */
        --g: 0px; /* gap between border//image */
        
        padding: calc(var(--g) + var(--t));
        outline: var(--t) solid var(--bs-white); /* color here */
        outline-offset: calc(-1*var(--t));
        mask:
            conic-gradient(at var(--s) var(--s),#0000 75%,#000 0)
            0 0/calc(100% - var(--s)) calc(100% - var(--s)),
            conic-gradient(#000 0 0) content-box;
    }

    .max-width {
        max-width: calc(100vw - (var(--bs-gutter-x) * 0.5)) !important;
    }

    .img-map-parent {
        max-width: 100vw;
        white-space: nowrap;
        overflow: hidden;
    }

    .img-overlay {
        width: 434px;
        height: 114px;
        position: absolute;
        background: #000a;
        /*
        background: linear-gradient(to right, #0008, var(--bs-body-bg));
        */
        z-index: 1;
    }

    .img-overlay-player {
        width: 360px;
        height: 114px;
        position: absolute;
        background: #000a;
        background: linear-gradient(to right, var(--bs-body-bg), #0008);
        z-index: 1;
    }

    .img-overlay-armada {
        background-color: #487edb44;
    }

    .img-overlay-cortex {
        background-color: #b93d3244;
    }

    .img-map-side {
        display: inline-block;
        width: 124px;
        height: 114px;
        transform: scaleX(-1);
        background-repeat: no-repeat !important;
        background-size: 150% !important;
    }

    .img-map-left {
        background-position: left -36px !important;
    }

    .img-map-center {
        display: inline-block;
        width: 186px;
        height: 114px;
        background-position: center -36px !important;
        background-size: cover !important;
        background-repeat: no-repeat !important;
    }

    .img-map-right {
        background-position: right -36px !important;
    }
</style>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import InfoHover from "components/InfoHover.vue";
    import MatchList from "components/app/MatchList.vue";
    import ToggleButton from "components/ToggleButton";
    import ApiError from "components/ApiError";
    import Busy from "components/Busy.vue";

    import { MatchPool } from "model/MatchPool";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarUser } from "model/BarUser";

    import { MatchPoolApi } from "api/MatchPoolApi";
    import { BarMatchApi } from "api/BarMatchApi";
    import { BarMatchProcessingApi } from "api/BarMatchProcessingApi";
    import { BarUserApi } from "api/BarUserApi";

    import AccountUtil from "util/Account";
    import { MapUtil } from "util/MapUtil";
    import ColorUtils from "util/Color";

    import "filters/LocaleFilter";

    type MapStats = {
        mapName: string;
        mapFilename: string;
        plays: number;
        cortexPlays: number;
        cortexWins: number;
        armadaPlays: number;
        armadaWins: number;
        legionPlays: number;
        legionWins: number;
        durations: number[];
        averageDurationMs: number;
    }

    type PlayerStats = {
        userID: number;
        username: string;
        plays: number;
        wins: number;
        factionPref: string;
        flag: string | undefined;

        cortexPlays: number;
        cortexWins: number;
        armadaPlays: number;
        armadaWins: number;
        legionPlays: number;
        legionWins: number;
        randomPlays: number;
        randomWins: number;
        durations: number[];
        averageDurationMs: number;
    }

    export const MatchPoolView = Vue.extend({
        props: {

        },

        data: function() {
            return {
                poolID: 0 as number,

                pool: Loadable.idle() as Loading<MatchPool>,
                matches: Loadable.idle() as Loading<BarMatch[]>,

                showPoolEdit: false as boolean,
                addMatch: "" as string,

                showMapStats: false as boolean,
                mapStats: [] as MapStats[],

                showPlayerStats: false as boolean,
                playerStats: [] as PlayerStats[]
            }
        },

        mounted: function(): void {
            this.poolID = Number.parseInt(location.pathname.split("/")[2]);
            console.log(`MatchPoolView> parsed poolID [poolID=${this.poolID}]`);
            this.loadPool();
        },

        methods: {
            loadPool: async function(): Promise<void> {
                this.pool = Loadable.loading();
                this.pool = await MatchPoolApi.getByID(this.poolID);

                if (this.pool.state != "loaded") {
                    return;
                }

                document.title = `Gex / Pool / ${this.pool.data.name}`;

                this.matches = Loadable.loading();

                const loadedMatches: BarMatch[] = [];
                let offset: number = 0;
                while (true) {
                    const res = await BarMatchApi.search(offset, 100, "start_time", "desc", {
                        poolID: this.poolID
                    });

                    if (res.state != "loaded") {
                        this.matches = Loadable.rewrap(res);
                        break;
                    }

                    loadedMatches.push(...res.data);
                    if (res.data.length == 100) {
                        offset += 100;
                        console.log(`MatchPoolView> got 100/100 entries, checking for next page`);
                    } else {
                        this.matches = Loadable.loaded(loadedMatches);
                        break;
                    }
                }

                if (this.matches.state == "loaded") {
                    this.loadMapStats();
                    this.loadPlayerStats();
                }
            },

            loadMapStats: function(): void {
                if (this.matches.state != "loaded") {
                    console.warn(`MatchPoolView> cannot make map stats, matches is not loaded (is ${this.matches.state})`);
                    return;
                }

                this.mapStats = [];

                const map: Map<string, MapStats> = new Map();
                for (const match of this.matches.data) {
                    
                    if (match.allyTeams.find(iter => iter.won == true) == undefined) {
                        console.log(`MatchPoolView> skipping match that had no winner [gameID=${match.id}]`);
                        continue;
                    }

                    const stat: MapStats = map.get(match.mapName) ?? {
                        mapFilename: match.mapName,
                        mapName: match.map,
                        plays: 0,
                        cortexPlays: 0,
                        cortexWins: 0,
                        armadaPlays: 0,
                        armadaWins: 0,
                        legionPlays: 0,
                        legionWins: 0,
                        durations: [],
                        averageDurationMs: 0
                    };

                    stat.durations.push(match.durationMs);
                    ++stat.plays;

                    for (const allyTeam of match.allyTeams) {
                        const players: BarMatchPlayer[] = match.players.filter(iter => iter.allyTeamID == allyTeam.allyTeamID);

                        for (const p of players) {
                            const addWin: number = allyTeam.won ? 1 : 0;
                            if (p.faction == "Cortex") {
                                ++stat.cortexPlays;
                                stat.cortexWins += addWin;
                            } else if (p.faction == "Armada") {
                                ++stat.armadaPlays;
                                stat.armadaWins += addWin;
                            } else if (p.faction == "Legion") {
                                ++stat.legionPlays;
                                stat.legionWins += addWin;
                            }
                        }
                    }

                    map.set(stat.mapFilename, stat);
                }

                this.mapStats = Array.from(map.values()).sort((a, b) => {
                    return a.mapName.localeCompare(b.mapName);
                }).map(iter => {
                    iter.averageDurationMs = iter.durations.reduce((acc, iter) => acc += iter, 0) / Math.max(1, iter.durations.length);
                    return iter;
                });
            },

            loadPlayerStats: async function(): Promise<void> {
                if (this.matches.state != "loaded") {
                    console.warn(`MatchPoolView> cannot make map stats, matches is not loaded (is ${this.matches.state})`);
                    return;
                }

                this.playerStats = [];

                const playerIDs: number[] = this.matches.data.map(iter => iter.players.map(i2 => i2.userID)).reduce((acc, iter) => {
                    acc.push(...iter);
                    return acc;
                }, []);

                let apiUsers: Loading<BarUser[]> = await BarUserApi.getByUserIDs(playerIDs);
                if (apiUsers.state != "loaded") {
                    Toaster.add("Failed to load users", "failed to load users, see console for more", "danger");
                    console.error(`MatchPoolView> failed to load user stats: ${JSON.stringify(apiUsers)}`);
                    return;
                }

                const userDict: Map<number, BarUser> = new Map();
                for (const user of apiUsers.data) {
                    userDict.set(user.userID, user);
                }
                console.log(`MatchPoolView> loaded user data for ${userDict.size} users`);

                const map: Map<number, PlayerStats> = new Map();
                for (const match of this.matches.data) {

                    for (const allyTeam of match.allyTeams) {
                        const players: BarMatchPlayer[] = match.players.filter(iter => iter.allyTeamID == allyTeam.allyTeamID);

                        for (const p of players) {
                            const stat: PlayerStats = map.get(p.userID) ?? {
                                userID: p.userID,
                                username: p.username,
                                plays: 0,
                                wins: 0,
                                factionPref: "",
                                flag: undefined,
                                cortexPlays: 0,
                                cortexWins: 0,
                                armadaPlays: 0,
                                armadaWins: 0,
                                randomPlays: 0,
                                randomWins: 0,
                                legionPlays: 0,
                                legionWins: 0,
                                durations: [],
                                averageDurationMs: 0
                            };

                            if (stat.plays == 0) {
                                const u: BarUser | undefined = userDict.get(p.userID);
                                if (u == undefined) {
                                    console.warn(`MatchPoolView> missing userDict entry for user [userID=${p.userID}]`);
                                } else {
                                    stat.flag = u.countryCode ?? undefined;
                                }
                            }

                            stat.durations.push(match.durationMs);
                            ++stat.plays;
                            const addWin: number = (allyTeam.won == true) ? 1 : 0;
                            if (p.faction == "Cortex") {
                                ++stat.cortexPlays;
                                stat.cortexWins += addWin;
                            } else if (p.faction == "Armada") {
                                ++stat.armadaPlays;
                                stat.armadaWins += addWin;
                            } else if (p.faction == "Legion") {
                                ++stat.legionPlays;
                                stat.legionWins += addWin;
                            } else if (p.faction == "Random") {
                                ++stat.randomPlays;
                                stat.randomWins += addWin;
                            } else {
                                console.warn(`MatchPoolView> unhandled faction '${p.faction}' from player ${p.username} in game ${match.id}`);
                            }

                            map.set(p.userID, stat);
                        }
                    }
                }

                this.playerStats = Array.from(map.values()).sort((a, b) => {
                    return a.username.localeCompare(b.username);
                }).map(iter => {
                    iter.averageDurationMs = iter.durations.reduce((acc, iter) => acc += iter, 0) / Math.max(1, iter.durations.length);
                    iter.wins = iter.armadaWins + iter.cortexWins + iter.legionWins + iter.randomWins;
                    if (iter.armadaPlays > iter.cortexPlays) {
                        iter.factionPref = "armada";
                    } else {
                        iter.factionPref = "cortex";
                    }
                    return iter;
                });
            },

            addMatchesToPool: async function(): Promise<void> {
                const matchIds: string [] = this.addMatch.split("\n");

                let added: number = 0;
                let total: number = matchIds.length;

                for (const matchId of matchIds) {
                    if (matchId == "") {
                        continue;
                    }
                    let mID: string = matchId.trim();
                    console.log(`adding match ${mID} to pool ${this.poolID}`);
                    const res: Loading<void> = await MatchPoolApi.addMatchToPool(this.poolID, mID);
                    if (res.state != "loaded") {
                        console.error(`failed to add match ${mID} to pool ${this.poolID}`);
                    } else {
                        ++added;
                    }
                }

                Toaster.add(`Matches added`, `Added ${added}/${total} matches to pool ${this.poolID}`, "success");

                await this.loadPool();
            },

            removeMatchFromPool: async function(matchId: string): Promise<void> {
                await MatchPoolApi.removeMatchFromPool(this.poolID, matchId);
                await this.loadPool();
            },

            setPriorityToOne: async function(): Promise<void> {
                if (this.matches.state != "loaded") {
                    return;
                }

                for (const match of this.matches.data) {
                    const res = await BarMatchProcessingApi.updatePriority(match.id, 1);
                    if (res.state == "error") {
                        console.error(`failed to update prio of ${match.id}: ${JSON.stringify(res.problem)}`);
                        Toaster.add(`Failed to update prio`, `Failed to update prio of ${match.id} to 1: ${res.problem.detail}`, "danger");
                    } else if (res.state == "notfound") {
                        Toaster.add(`Missing match`, `Missing match ${match.id}`, "danger");
                    } else if (res.state != "loaded") {
                        Toaster.add(`unchecked response state`, `response state: ${res.state}`, "danger");
                    }
                }
            },

            mapBackground: function(map: MapStats): any {
                return {
                    "background": `url("/image-proxy/MapNameBackground?map=${map.mapName}&size=texture-thumb")`
                };
            },

            mapNameWithoutVersion: function(name: string): string {
                return MapUtil.getNameNameWithoutVersion(name);
            },

        },

        computed: {
            hasAddRemovePermission: function(): boolean {
                return AccountUtil.hasPermission("Gex.MatchPoolEntry.AddRemove");
            },

            hasUpdatePriorityPermission: function(): boolean {
                return AccountUtil.hasPermission("Gex.Match.ForceReplay");
            },

            colors: function() {
                return {
                    "Armada": ColorUtils.Armada + "11",
                    "Cortex": ColorUtils.Cortex + "11",
                    "Legion": ColorUtils.Legion + "11",
                };
            }
        },

        components: {
            InfoHover, MatchList, ToggleButton, ApiError, Busy
        }
    });
    export default MatchPoolView;
</script>