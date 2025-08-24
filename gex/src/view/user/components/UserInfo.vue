
<template>
    <div>
        <div class="mb-4">
            <h1 class="text-center border-bottom pb-2 mb-4">
                {{ user.username }}
            </h1>

            <div class="d-flex mb-3 flex-wrap" style="gap: 1rem;">
                <div class="flex-grow-1 text-center">
                    <h2 class="border-bottom d-inline-block px-3">
                        Favorite faction
                    </h2>

                    <h4 class="mb-1">
                        <faction-icon :faction="mostPlayedFaction.faction" :width="48"></faction-icon>
                        {{ mostPlayedFaction.faction | faction }}
                    </h4>

                    <h5>
                        {{ mostPlayedFaction.winCount / mostPlayedFaction.playCount * 100 | locale(0) }}% won of 
                        {{ mostPlayedFaction.playCount | locale(0) }} played
                    </h5>
                </div>

                <div class="flex-grow-1 text-center">
                    <h2 class="border-bottom d-inline-block px-3">
                        Favorite gamemode
                    </h2>

                    <h4 class="mb-1">
                        {{ mostPlayedGamemode.gamemode | gamemode }}
                    </h4>

                    <h5>
                        {{ mostPlayedGamemode.sum.winCount / mostPlayedGamemode.sum.playCount * 100 | locale(0) }}% won of 
                        {{ mostPlayedGamemode.sum.playCount | locale(0) }} played
                    </h5>
                </div>

                <div class="flex-grow-1 text-center">
                    <h2 class="border-bottom d-inline-block px-3">
                        Favorite maps
                    </h2>

                    <div class="d-lg-none">
                        <div v-for="map in mostPlayedMaps" :key="map.map" class="mb-2">
                            <b>{{ map.map }}</b>
                            <div>
                                {{ map.winCount / map.playCount * 100 | locale(0) }}% won of 
                                {{ map.playCount }} played
                            </div>
                        </div>
                    </div>

                    <div class="d-none d-lg-grid" style="grid-template-columns: 1fr 1fr;">
                        <template v-for="map in mostPlayedMaps">
                            <div class="text-end">
                                <b>{{ map.map }}</b>
                            </div>
                            <div class="ps-1 text-start">
                                -
                                {{ map.winCount / map.playCount * 100 | locale(0) }}% won of 
                                {{ map.playCount }} played
                            </div>
                        </template>
                    </div>
                </div>
            </div>

            <h6 class="ps-3">
                <a :href="'https://www.bar-stats.pro/playerstats?playerName=' + user.username" target="_blank" ref="nofollow">BarStats link</a>
            </h6>
        </div>

        <div class="mb-4">
            <h4 class="wt-header bg-light text-dark mb-3">
                <b>Gamemode ratings</b>
            </h4>

            <div class="d-flex flex-wrap justify-content-around" style="gap: 1rem;">
                <div v-for="skill in skills" :key="skill.gamemode" class="hoverable text-center mx-2 rounded p-3">
                    <h5 class="border-bottom py-1">
                        {{ skill.gamemode | gamemode }}
                    </h5>

                    <div>
                        {{ skill.skill | locale(2) }}
                        <span class="text-muted">
                            &plusmn;{{ skill.skillUncertainty | locale(2) }}
                        </span>
                    </div>
                </div>
            </div>
        </div>

        <div>
            <h4 class="wt-header bg-light text-dark">
                <b>Faction stats</b>
            </h4>

            <div v-for="faction in groupedFactionData" :key="faction.gamemode" class="mb-4">
                <div class="wt-header mb-2 text-white" style="white-space: nowrap; text-wrap: wrap;">
                    <h5 class="ms-2 d-inline-block mb-0">
                        <strong>
                            {{ faction.gamemode | gamemode }}
                        </strong>
                    </h5>

                    <wbr/>

                    <h6 class="d-inline-block mb-0">
                        - {{ faction.sum.winCount / faction.sum.playCount * 100 | locale(0) }}% win rate over {{ faction.sum.playCount }} games
                    </h6>
                </div>

                <table class="table table-sm">
                    <thead>
                        <tr class="table-active">
                            <th class="ps-2">Faction</th>
                            <th>Plays</th>
                            <th>Wins</th>
                            <th>Win %</th>
                        </tr>
                    </thead>
                    
                    <tbody>
                        <tr v-if="faction.armada" is="FactionStatsRow" :data="faction.armada" :faction="1"></tr>
                        <tr v-if="faction.cortex" is="FactionStatsRow" :data="faction.cortex" :faction="2"></tr>
                        <tr v-if="faction.legion" is="FactionStatsRow" :data="faction.legion" :faction="3"></tr>
                        <tr v-if="faction.random" is="FactionStatsRow" :data="faction.random" :faction="4"></tr>
                        <tr class="table-active" is="FactionStatsRow" :data="faction.sum" :faction="0"></tr>
                    </tbody>
                </table>
            </div>
        </div>

        <div v-if="user.previousNames.length > 1" class="mb-3">
            <h4 class="wt-header bg-light text-dark mb-2">Previous names</h4>

            <a-table :entries="previousNames" :show-filters="false" default-sort-field="" default-sort-order="asc" row-padding="compact" :hover="true" :default-page-size="10">
                <a-col>
                    <a-header>
                        <b>User name</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.userName }}
                    </a-body>
                </a-col>

                <a-col>
                    <a-header>
                        <b>Timestamp</b>
                        <info-hover text="When Gex first saw a game with this name"></info-hover>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.timestamp | moment }}
                    </a-body>
                </a-col>
            </a-table>
        </div>

        <div>
            <h4 class="wt-header bg-light text-dark mb-1">
                Maps
            </h4>
            <h6 class="text-muted mb-3">
                Map stats are seperated into gamemode, so it is possible to have 1 map listed multiple times, each for a different gamemode
            </h6>

            <a-table :entries="mapData" :show-filters="true" default-sort-field="playCount" default-sort-order="desc" :default-page-size="10" :overflow-wrap="true">
                <a-col>
                    <a-header>
                        <b>Map</b>
                    </a-header>

                    <a-filter field="map" type="string" method="input"
                        :conditions="[ 'contains', 'equals' ]">
                    </a-filter>

                    <a-body v-slot="entry">
                        <a :href="'/MapName/' + encodeURIComponent(entry.map)">
                            {{ entry.map }}
                        </a>
                    </a-body>
                </a-col>

                <a-col>
                    <a-header>
                        <b>Gamemode</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.gamemode | gamemode }}
                    </a-body>
                </a-col>

                <a-col sort-field="playCount">
                    <a-header>
                        <b>Play count</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.playCount }}
                    </a-body>
                </a-col>

                <a-col>
                    <a-header>
                        <b>Win/Loss</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <span style="color: var(--bs-green)">
                            {{ entry.winCount }}
                        </span>
                        /
                        <span style="color: var(--bs-red)">
                            {{ entry.lossCount }}
                        </span>
                        <span>
                            ({{ entry.winCount / Math.max(1, entry.playCount) * 100 | locale(0) }}%)
                        </span>
                    </a-body>
                </a-col>

                <a-col>
                    <a-header>
                        <b>Start spots</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <button class="btn btn-primary btn-sm px-1 py-0 border-0" @click="generateStartSpots(entry.map, entry.gamemode)">
                            Generate
                        </button>
                    </a-body>
                </a-col>
            </a-table>

            <div v-if="startSpot.loaded == true" class="mb-3">
                <h4 class="wt-header bg-light text-dark">
                    Starting position data

                    <button class="btn-close border border-light text-light" @click="closeStartSpots" aria-label="close">
                        &times;
                    </button>
                </h4>

                <span v-if="startSpot.map.state == 'loading' || startSpot.data.state == 'loading'">
                    <busy class="busy busy-sm"></busy>
                    Loading...
                </span>

                <div v-if="startSpot.map.state == 'loaded' && startSpot.data.state == 'loaded'">
                    <h4 class="text-center">
                        Viewing map stats for 
                        <b>{{ user.username }}</b>
                        on map 
                        <b>{{ startSpot.map.data.name }}</b>
                        for gamemode
                        <b>{{ startSpot.gamemode | gamemode }}</b>
                    </h4>

                    <start-spot-map :map-data="startSpot.map.data" :start-spots="startSpot.data.data">
                    </start-spot-map>
                </div>

                <api-error v-if="startSpot.map.state == 'error'" :error="startSpot.map.problem"></api-error>
                <api-error v-if="startSpot.data.state == 'error'" :error="startSpot.data.problem"></api-error>
            </div>

        </div>
    </div>
</template>

<style scoped>

    .hoverable {
        border: var(--bs-border-width) var(--bs-border-style) var(--bs-border-color);
        transition: border-color 0.1s ease-in, background-color 0.1s ease-in;
    }

    .hoverable:hover {
        background-color: var(--bs-secondary-bg);
        border-color: var(--bs-white);
    }

</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";
    import StartSpotMap from "components/app/StartSpotMap.vue";
    import Busy from "components/Busy.vue";
    import { FactionIcon } from "components/app/FactionIcon";

    import { BarUser } from "model/BarUser";
    import { BarUserMapStats } from "model/BarUserMapStats";
    import { BarUserSkill } from "model/BarUserSkill";
    import { MapStatsStartSpot } from "model/map_stats/MapStatsStartSpot";
    import { BarMap } from "model/BarMap";
    import { UserPreviousName } from "model/UserPreviousName";
    import { BarUserFactionStats } from "model/BarUserFactionStats";

    import { MapStatsApi } from "api/map_stats/MapStatsApi";
    import { MapApi } from "api/MapApi";

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";
    import "filters/BarFactionFilter";
    import "filters/BarGamemodeFilter";

    import { FactionUtil } from "util/Faction";

    const FactionStatsRow = Vue.extend({
        props: {
            faction: { type: Number, required: true },
            data: { type: Object as PropType<GroupedFaction>, required: false }
        },

        template: `
            <tr>
                <td>
                    <span v-if="faction == 0" class="ps-2"><b>Total</b></span>
                    <img v-else-if="faction == 1" src="/img/armada.png" width="24" title="icon for armada">
                    <img v-else-if="faction == 2" src="/img/cortex.png" width="24" title="icon for cortex">
                    <img v-else-if="faction == 3" src="/img/legion.png" width="24" title="icon for legion">
                    <img v-else-if="faction == 4" src="/img/random.png" width="24" title="icon for random">
                    <span v-else>
                        unchecked faction {{ faction }}
                    </span>
                    <span v-if="faction != 0">
                        {{ faction | faction }}
                    </span>
                </td>
                <template v-if="data == null">
                    <td class="text-muted">--</td>
                    <td class="text-muted">--</td>
                    <td class="text-muted">--</td>
                </template>
                <template v-else>
                    <td>{{ data.playCount | locale(0) }}</td>
                    <td>{{ data.winCount | locale(0) }}
                    <td>{{ data.winCount / data.playCount * 100 | locale(0) }}%</td>
                </template>
            </tr>
        `
    });

    type GroupedFaction = {
        faction: number;
        playCount: number;
        winCount: number;
    }

    type GroupedFactionGamemode = {
        gamemode: number;
        armada: GroupedFaction | null;
        cortex: GroupedFaction | null;
        legion: GroupedFaction | null;
        random: GroupedFaction | null;
        sum: GroupedFaction;
    }

    export const UserInfo = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true }
        },

        data: function() {
            return {
                startSpot: {
                    loaded: false as boolean,
                    gamemode: 0 as number,
                    map: Loadable.idle() as Loading<BarMap>,
                    data: Loadable.idle() as Loading<MapStatsStartSpot[]>,
                }
            }
        },

        methods: {
            generateStartSpots: async function(mapName: string, gamemode: number): Promise<void> {
                this.startSpot.loaded = false;

                this.startSpot.gamemode = gamemode;

                this.startSpot.data = Loadable.loading();
                this.startSpot.data = await MapStatsApi.getStartSpotsByMapAndUser(mapName, this.user.userID);
                this.startSpot.loaded = true;

                if (this.startSpot.data.state != "loaded") {
                    return;
                }

                if (this.startSpot.data.data.length == 0) {
                    this.startSpot.data = Loadable.error(`no start spot data found for user ${this.user.userID} and map ${mapName}`);
                    return;
                }

                const startSpots: MapStatsStartSpot[] = this.startSpot.data.data.filter(iter => iter.gamemode == gamemode);
                if (startSpots.length == 0) {
                    this.startSpot.data = Loadable.error(`no start spot data found for user ${this.user.userID} and map ${mapName} on gamemode ${gamemode}`);
                    return;
                }

                const mapFilename: string = startSpots[0].mapFilename;
                this.startSpot.data = Loadable.loaded(startSpots);
                this.startSpot.map = Loadable.loading();
                this.startSpot.map = await MapApi.getByFilename(mapFilename);

                this.startSpot.loaded = true;
                console.log(`UserInfo> showing start spot info for map ${mapName} on gamemode ${gamemode} for user ${this.user.username}`);
            },

            closeStartSpots: function(): void {
                this.startSpot.loaded = false;
                this.startSpot.data = Loadable.idle();
                this.startSpot.map = Loadable.idle();
            }
        },

        computed:  {

            mapData: function(): Loading<BarUserMapStats[] >{
                return Loadable.loaded(this.user.mapStats);
            },

            skills: function(): BarUserSkill[] {
                return [...this.user.skill].sort((a, b) => a.gamemode - b.gamemode);
            },

            totalPlays: function(): number {
                return this.user.factionStats.reduce((acc, iter) => acc += iter.playCount, 0);
            },

            mapTotalPlays: function(): number {
                return this.user.mapStats.reduce((acc, iter) => acc += iter.playCount, 0);
            },

            startSpotGamemodes: function(): number[] {
                if (this.startSpot.data.state != "loaded") {
                    return [];
                }

                return this.startSpot.data.data.map(iter => iter.gamemode).filter((val, index, arr) => arr.indexOf(val) == index);
            },

            groupedFactionData: function(): GroupedFactionGamemode[] {
                const map: Map<number, GroupedFaction[]> = new Map();

                for (const faction of this.user.factionStats) {
                    const factionData: GroupedFaction[] = (map.get(faction.gamemode) ?? [])
                    factionData.push({
                        faction: faction.faction,
                        playCount: faction.playCount,
                        winCount: faction.winCount
                    });

                    map.set(faction.gamemode, factionData);
                }

                return Array.from(map.entries()).map(iter => {
                    const sum: GroupedFaction = {
                        faction: 0,
                        playCount: iter[1].reduce((acc, iter) => acc += iter.playCount, 0),
                        winCount: iter[1].reduce((acc, iter) => acc += iter.winCount, 0),
                    }

                    return {
                        gamemode: iter[0],
                        armada: iter[1].find(iter => iter.faction == FactionUtil.ARMADA) ?? null,
                        cortex: iter[1].find(iter => iter.faction == FactionUtil.CORTEX) ?? null,
                        legion: iter[1].find(iter => iter.faction == FactionUtil.LEGION) ?? null,
                        random: iter[1].find(iter => iter.faction == FactionUtil.RANDOM) ?? null,
                        sum: sum
                    }
                }).sort((a, b) => {
                    return b.sum.playCount - a.sum.playCount;
                });
            },

            previousNames: function(): Loading<UserPreviousName[]> {
                return Loadable.loaded(this.user.previousNames);
            },

            mostPlayedFaction: function(): BarUserFactionStats {
                // vetur moment doesn't know es2024 stuff
                const map: Map<number, BarUserFactionStats[]> = Map.groupBy(this.user.factionStats, (elem: BarUserFactionStats) => {
                    return elem.faction;
                });

                // TODO 2025-08-24: this kinda fuckin sucks yo, is there a better way for this?
                return Array.from(map.entries()).sort((a, b) => {
                    const aCount: number = a[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    const bCount: number = b[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    return bCount - aCount;
                }).slice(0, 1).map(iter => {
                    const fac: BarUserFactionStats = {
                        faction: iter[0],
                        gamemode: 0,
                        playCount: iter[1].reduce((acc, iter) => acc += iter.playCount, 0),
                        lossCount: 0,
                        tieCount: 0,
                        lastUpdated: new Date(),
                        userID: 0,
                        winCount: iter[1].reduce((acc, iter) => acc += iter.winCount, 0),
                    };
                    return fac;
                })[0];
            },

            mostPlayedGamemode: function(): GroupedFactionGamemode {
                return [...this.groupedFactionData].sort((a, b) => {
                    return b.sum.playCount - a.sum.playCount;
                })[0];
            },

            mostPlayedMaps: function(): BarUserMapStats[] {
                const map: Map<string, BarUserMapStats[]> = Map.groupBy(this.user.mapStats, (iter: BarUserMapStats) => iter.map);

                return Array.from(map.entries()).sort((a, b) => {
                    const aCount: number = a[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    const bCount: number = b[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    return bCount - aCount;
                }).slice(0, 3).map(iter => {
                    const map: BarUserMapStats = {
                        map: iter[0],
                        gamemode: 0,
                        lastUpdated: new Date(),
                        lossCount: 0,
                        winCount: iter[1].reduce((acc, iter) => acc += iter.winCount, 0),
                        playCount: iter[1].reduce((acc, iter) => acc += iter.playCount, 0),
                        tieCount: 0,
                        userID: 0
                    };
                    return map;
                });
            }
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            InfoHover, Busy,
            StartSpotMap, FactionStatsRow,
            FactionIcon
        }
    });
    export default UserInfo;
</script>