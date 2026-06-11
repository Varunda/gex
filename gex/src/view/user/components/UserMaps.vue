<template>
    <div>
        <h2 class="wt-header border-0">
            Maps
        </h2>
        <h6 class="text-muted mb-3 text-center">
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
                    <span style="color: var(--bs-success-text-emphasis)">
                        {{ entry.winCount }}
                    </span>
                    /
                    <span style="color: var(--bs-danger-text-emphasis)">
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
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import { BarMatch } from "model/BarMatch";
    import { BarUser } from "model/BarUser";
    import { BarMap } from "model/BarMap";
    import { MapStatsStartSpot } from "model/map_stats/MapStatsStartSpot";
    import { BarUserMapStats } from "model/BarUserMapStats";

    import { MapApi } from "api/MapApi";
    import { MapStatsApi } from "api/map_stats/MapStatsApi";

    import Busy from "components/Busy.vue";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";
    import StartSpotMap from "components/app/StartSpotMap.vue";

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";
    import "filters/BarFactionFilter";
    import "filters/BarGamemodeFilter";

    export const UserMaps = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            matches: { type: Array as PropType<BarMatch[]>, required: true }
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

        computed: {
            mapData: function(): Loading<BarUserMapStats[] >{
                return Loadable.loaded(this.user.mapStats);
            },

            startSpotGamemodes: function(): number[] {
                if (this.startSpot.data.state != "loaded") {
                    return [];
                }

                return this.startSpot.data.data.map(iter => iter.gamemode).filter((val, index, arr) => arr.indexOf(val) == index);
            },
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            InfoHover, Busy,
            StartSpotMap, 
        }
    });
    export default UserMaps;
</script>