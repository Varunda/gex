<template>
    <div class="container">
        <div v-if="barMap.state == 'idle'"></div>

        <div v-else-if="barMap.state == 'loading'">
            Loading...
        </div>

        <div v-else-if="barMap.state == 'nocontent'">
            No map with name <code>{{ mapFilename }}</code> exists
        </div>

        <div v-else-if="barMap.state == 'loaded'">

            <div class="text-center">
                <h1>
                    {{ barMap.data.name }}
                </h1>

                <h5>
                    by {{ barMap.data.author }}
                </h5>
            </div>

            <div class="d-flex mb-3">
                <div class="flex-grow-1"></div>

                <div class="flex-grow-1">
                    <img :src="mapUrl" height="500px">
                </div>

                <div class="flex-grow-1">
                    <table class="table table-borderless">
                        <tr>
                            <td>Size</td>
                            <td>{{ barMap.data.width }}x{{ barMap.data.height }}</td>
                        </tr>

                        <tr>
                            <td>Max metal</td>
                            <td>{{ barMap.data.maxMetal }}</td>
                        </tr>

                        <tr>
                            <td>Tidal</td>
                            <td>{{ barMap.data.tidalStrength }}</td>
                        </tr>

                        <tr>
                            <td>Wind</td>
                            <td>{{ barMap.data.minimumWind }}-{{ barMap.data.maximumWind }}</td>
                        </tr>
                    </table>

                </div>
            </div>

            <div class="mb-3">
                <div v-if="stats.state == 'loaded'">

                    <h2 class="wt-header">Play stats</h2>

                    <span v-if="stats.data.length == 0">
                        Gex does not have any games played on this map!
                    </span>

                    <table v-else class="table">
                        <thead>
                            <tr>
                                <th>Gamemode</th>
                                <th>Average duration</th>
                                <th>Median duration</th>
                                <th>Play count</th>
                                <th>Play count (month)</th>
                                <th>Play count (week)</th>
                                <th>Play count (day)</th>
                            </tr>
                        </thead>

                        <tbody>
                            <tr v-for="stat in stats.data" :key="stat.gamemode">

                                <td>{{ stat.gamemode | gamemode }}</td>
                                <td>{{ stat.durationAverageMs / 1000 | mduration }}</td>
                                <td>{{ stat.durationMedianMs / 1000 | mduration }}</td>
                                <td>{{ stat.playCountAllTime | locale(0) }}</td>
                                <td>{{ stat.playCountMonth | locale(0) }}</td>
                                <td>{{ stat.playCountWeek | locale(0) }}</td>
                                <td>{{ stat.playCountDay | locale(0) }}</td>
                            </tr>
                        </tbody>
                    </table>

                    <span class="text-muted text-small">
                        If a gamemode does not exist, it means Gex has not seen a match played of that gamemode
                    </span>
                </div>

                <div v-else-if="stats.state == 'error'">
                    Failed to load stats
                    <api-error :problem="stats.problem"></api-error>
                </div>
            </div>

            <h2 class="wt-header">Recent games</h2>
            <div v-if="recent.state == 'loaded'">

                <match-list :matches="recent.data"></match-list>
            </div>

        </div>

        <div v-else-if="barMap.state == 'error'">
            <api-error :problem="barMap.problem"></api-error>
        </div>
    </div>

</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import ApiError from "components/ApiError";
    import MatchList from "components/app/MatchList.vue";

    import "filters/BarGamemodeFilter";
    import "filters/MomentFilter";
    import "filters/LocaleFilter";

    import { BarMap } from "model/BarMap";
    import { MapStatsByGamemode } from "model/map_stats/MapStatsByGamemode";
    import { BarMatch } from "model/BarMatch";

    import { MapApi } from "api/MapApi";
    import { MapStatsApi } from "api/map_stats/MapStatsApi";
    import { BarMatchApi } from "api/BarMatchApi";

    export const MapView = Vue.extend({
        props: {

        },

        data: function() {
            return {
                mapFilename: "" as string,

                barMap: Loadable.idle() as Loading<BarMap>,
                stats: Loadable.idle() as Loading<MapStatsByGamemode[]>,
                recent: Loadable.idle() as Loading<BarMatch[]>
            }
        },

        mounted: function(): void {
            this.getMapFilenameFromUrl();
            this.bind();
        },

        methods: {

            getMapFilenameFromUrl: function(): void {
                this.mapFilename = location.pathname.split("/")[2];
            },

            bind: async function(): Promise<void> {
                this.barMap = Loadable.loading();
                this.barMap = await MapApi.getByFilename(this.mapFilename);

                this.stats = Loadable.loading();
                this.stats = await MapStatsApi.getByMapFilename(this.mapFilename);

                this.bindRecent();
            },

            bindRecent: async function(): Promise<void> {
                if (this.barMap.state != "loaded") {
                    console.warn(`Map> cannot load recent games, map is not loaded ('${this.barMap.state}')`);
                    return;
                }

                this.recent = Loadable.loading();
                this.recent = await BarMatchApi.search(0, 8, {
                    map: this.barMap.data.name
                });
            }

        },

        computed: {
            mapUrl: function(): string {
                return `/image-proxy/MapBackground?mapName=${this.mapFilename}&size=texture-mq`;
            },
        },

        components: {
            ApiError, MatchList
        }
    });
    export default MapView;

</script>