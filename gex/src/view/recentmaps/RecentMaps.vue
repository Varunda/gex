<template>
    <div class="container">
        <h2 class="wt-header bg-light text-dark d-flex">
            <div class="flex-grow-1">
                Recent maps played
                <span v-if="timespan == '1d'">
                    (24h)
                </span>
                <span v-else-if="timespan == '7d'">
                    (7d)
                </span>
                <span v-else-if="timespan == '14d'">
                    (14d)
                </span>
                <span v-else-if="timespan == '30d'">
                    (30d)
                </span>
                <span v-else-if="timespan == 'alltime'">
                    (all time)
                </span>
            </div>

            <div class="btn-group flex-grow-0">
                <button class="btn btn-sm" :class="[ timespan == '1d' ? 'btn-primary' : 'btn-secondary' ]" @click="timespan = '1d'">
                    24h
                </button>
                <button class="btn btn-sm" :class="[ timespan == '7d' ? 'btn-primary' : 'btn-secondary' ]" @click="timespan = '7d'">
                    7 day
                </button>
                <button class="btn btn-sm" :class="[ timespan == '14d' ? 'btn-primary' : 'btn-secondary' ]" @click="timespan = '14d'">
                    14 day
                </button>
                <button class="btn btn-sm" :class="[ timespan == '30d' ? 'btn-primary' : 'btn-secondary' ]" @click="timespan = '30d'">
                    30 day
                </button>
                <button class="btn btn-sm" :class="[ timespan == 'alltime' ? 'btn-primary' : 'btn-secondary' ]" @click="timespan = 'alltime'">
                    all time
                </button>
            </div>
        </h2>

        <div v-if="recent.state == 'loading'" class="text-center">
            <busy class="busy busy-sm"></busy>
            Loading...
        </div>

        <div v-else-if="recent.state == 'loaded'" class="d-flex flex-wrap justify-content-around" style="gap: 0.75rem;">
            <div v-if="mapPlayRecentGroups.length == 0">
                no matches exist in this timespan
            </div>

            <div v-else v-for="group in mapPlayRecentGroups" :key="group.gamemode">
                <h3 class="text-center mb-0">
                    {{ group.gamemode | gamemode }}
                    <h5>
                        {{ group.entries.reduce((acc, iter) => acc += iter.count, 0) | locale(0) }} plays
                    </h5>
                    <h5>
                        {{ group.entries.length | locale(0) }} unique maps
                    </h5>
                </h3>

                <table class="table table-sm table-hover">
                    <thead class="table-ligh text-center">
                        <tr>
                            <th>Map</th>
                            <th>Plays</th>
                        </tr>
                    </thead>

                    <tbody>
                        <tr v-for="entry in group.entries" :key="group.gamemode + '-' + entry.map">
                            <td class="px-2">
                                <a :href="'/MapName/' + encodeURIComponent(entry.map)" :title="entry.map">
                                    <span v-if="entry.map.length > 27">
                                        {{ entry.map.slice(0, 24) }}...
                                    </span>
                                    <span v-else>
                                        {{ entry.map }}
                                    </span>
                                </a>
                            </td>
                            <td class="px-2">
                                {{ entry.count | locale(0) }}
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import Busy from "components/Busy.vue";

    import { MapPlayCountEntry } from "model/MapPlayCountEntry";

    import { MapPlayCountApi } from "api/MapPlayCountApi";

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";
    import * as lx from "luxon";

    type GroupedMapPlayCount = {
        gamemode: number;
        entries: MapPlayCountEntry[];
    };

    export const RecentMaps = Vue.extend({
        props: {

        },

        data: function() {
            return {
                recent: Loadable.idle() as Loading<MapPlayCountEntry[]>,
                map: new Map() as  Map<string, Map<number, MapPlayCountEntry>>,
                timespan: "1d" as "1d" | "7d" | "14d" | "30d" | "alltime",
            }
        },

        mounted: function(): void {
            document.title = "Gex / Recent maps";
            this.$nextTick(() => {
                this.loadData();
            });
        },

        methods: {
            loadData: async function(): Promise<void> {
                this.recent = Loadable.loading();
                this.recent = await MapPlayCountApi.getRecent();
            },
        },

        computed: {

            dateCutoff: function(): Date {
                if (this.timespan == "1d") {
                    return lx.DateTime.utc().minus(lx.Duration.fromObject({ days: 1 })).toJSDate();
                } else if (this.timespan == "7d") {
                    return lx.DateTime.utc().minus(lx.Duration.fromObject({ days: 7 })).toJSDate();
                } else if (this.timespan == "14d") {
                    return lx.DateTime.utc().minus(lx.Duration.fromObject({ days: 14 })).toJSDate();
                } else if (this.timespan == "30d") {
                    return lx.DateTime.utc().minus(lx.Duration.fromObject({ days: 30 })).toJSDate();
                } else if (this.timespan == "alltime") {
                    return new Date(0);
                } else {
                    console.warn(`RecentMaps> unchecked timespan value '${this.timespan}'`);
                    return new Date();
                }
            },

            mapPlayRecentGroups: function(): GroupedMapPlayCount[] {
                if (this.recent.state != "loaded") {
                    return [];
                }

                return this.recent.data.reduce((acc: GroupedMapPlayCount[], iter: MapPlayCountEntry) => {
                    if (iter.timestamp == null) {
                        return acc;
                    }

                    if (iter.timestamp.getTime() <= this.dateCutoff.getTime()) {
                        return acc;
                    }

                    const g = acc.find(i => i.gamemode == iter.gamemode);

                    if (g == undefined) {
                        acc.push({
                            gamemode: iter.gamemode,
                            entries: [iter]
                        });
                    } else {
                        const m = g.entries.find(i => i.map == iter.map);
                        if (m == undefined) {
                            g.entries.push(iter);
                        } else {
                            m.count += iter.count;
                        }
                    }

                    return acc;
                }, []).sort((a, b) => a.gamemode - b.gamemode).map(iter => {
                    iter.entries.sort((a, b) => b.count - a.count || a.map.localeCompare(b.map));
                    return iter;
                });
            }

        },

        components: {
            Busy
        }
    });
    export default RecentMaps;
</script>