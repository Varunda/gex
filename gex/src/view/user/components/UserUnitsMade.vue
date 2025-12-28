<template>
    <div>
        <h2 class="wt-header bg-light text-dark">
            Units created
        </h2>

        <div class="alert alert-info text-center">
            <div>
                <b>This data is only an estimate, and undercounts the correct value</b>
            </div>

            <div>
                This data only includes public PvP matches after Gex started, and only when a match is fully processed. 
                <br/>
                See what conditions Gex processes a match <a href="/faq">here</a>
            </div>
        </div>

        <div class="mb-2 d-grid" style="grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); column-gap: 1rem;">
            <div>
                <label>Map filter</label>
                <select v-model="mapFilter" class="form-control" style="max-width: 240px;">
                    <option :value="null">all</option>
                    <option v-for="map in uniqueMaps" :value="map" :key="map">
                        {{ map }}
                    </option>
                </select>
            </div>

            <div>
                <label>Gamemode filter</label>
                <select v-model="gamemodeFilter" class="form-control" style="max-width: 240px;">
                    <option :value="null">all</option>
                    <option :value="1">Duel</option>
                    <option :value="2">Small team</option>
                    <option :value="3">Large team</option>
                    <option :value="4">FFA</option>
                    <option :value="5">Team FFA</option>
                    <option :value="0">Unknown</option>
                </select>
            </div>

            <div>
                <label class="d-block">Time range</label>

                <div class="btn-group w-100">
                    <button class="btn" :class="[ range == 'daily' ? 'btn-primary' : 'btn-outline-secondary' ]" @click="range = 'daily'">
                        24h
                    </button>
                    <button class="btn" :class="[ range == 'weekly' ? 'btn-primary' : 'btn-outline-secondary' ]" @click="range = 'weekly'">
                        7d
                    </button>
                    <button class="btn" :class="[ range == 'monthly' ? 'btn-primary' : 'btn-outline-secondary' ]" @click="range = 'monthly'">
                        30d
                    </button>
                    <button class="btn" :class="[ range == 'all_time' ? 'btn-primary' : 'btn-outline-secondary' ]" @click="range = 'all_time'">
                        All
                    </button>
                </div>
            </div>

            <div>
                <label class="d-block">Split by map</label>

                <button class="btn w-100" :class="[ mapSplit == true ? 'btn-primary' : 'btn-outline-light' ]" @click="toggleMapSplit()">
                    Toggle map split
                </button>
            </div>

            <div>
                <label class="d-block">Show debug info</label>

                <toggle-button v-model="showDebug">
                    debug
                </toggle-button>
            </div>
        </div>

        <a-table v-if="showTable == true" :entries="unitsMade" :show-filters="true" default-sort-field="count" default-sort-order="desc" :paginate="true" :default-page-size="10" :overflow-wrap="true">
            <a-col sort-field="unitName">
                <a-header>
                    <b>Unit name</b>
                </a-header>

                <a-filter field="unitName" type="string" method="input"
                    :conditions="[ 'contains', 'equals' ]">
                </a-filter>

                <a-body v-slot="entry">
                    <unit-icon v-if="entry.definitionName != ''" :name="entry.definitionName" :color="getUnitColor(entry.definitionName)"></unit-icon>
                    {{ entry.unitName }} 
                    <span v-if="showDebug">
                        / {{ entry.definitionName }}
                    </span>
                </a-body>
            </a-col>

            <a-col v-if="mapSplit">
                <a-header>
                    <b>Map</b>
                </a-header>

                <a-filter field="map" type="string" method="input"
                    :conditions="[ 'contains', 'equals' ]">
                </a-filter>

                <a-body v-slot="entry">
                    {{ entry.map }}
                </a-body>
            </a-col>

            <a-col sort-field="count">
                <a-header>
                    <b>Created</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.count | locale(0) }}
                </a-body>
            </a-col>
        </a-table>

        <div class="mb-2">
            Last updated: {{ lastUpdated | moment }}. Show stats from {{ filteredMatches.size }} matches
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import { BarUser } from "model/BarUser";
    import { BarMatch } from "model/BarMatch";
    import { BarUserUnitsMade } from "model/BarUserUnitsMade";

    import UnitIcon from "components/app/UnitIcon.vue";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import ToggleButton from "components/ToggleButton";

    import "filters/LocaleFilter";
    import "filters/MomentFilter";

    import ColorUtils from "util/Color";

    type UnitsMade = {
        definitionName: string;
        unitName: string;
        map: string;
        count: number;
    }

    export const UserUnitsMade = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            matches: { type: Array as PropType<BarMatch[]>, required: true }
        },

        data: function() {
            return {
                range: "all_time" as "daily" | "weekly" | "monthly" | "all_time",
                mapFilter: null as string | null,
                gamemodeFilter: null as number | null,

                mapSplit: false as boolean,

                showDebug: false as boolean,
                showTable: true as boolean,
            }
        },

        methods: {
            getUnitColor: function(defName: string): string | null {
                if (defName.startsWith("arm")) {
                    return ColorUtils.Armada;
                } else if (defName.startsWith("cor")) {
                    return ColorUtils.Cortex;
                } else if (defName.startsWith("leg")) {
                    return ColorUtils.Legion;
                } else {
                    return null;
                }
            },

            toggleMapSplit: function(): void {
                this.mapSplit = !this.mapSplit;

                this.showTable = false;
                this.$nextTick(() => {
                    this.showTable = true;
                });
            },
        },

        computed: {
            unitsMade: function(): Loading<UnitsMade[]> {
                const map: Map<string, UnitsMade> = new Map();
                for (const iter of this.user.unitsMade) {
                    let match: BarMatch | undefined = this.filteredMatches.get(iter.gameID);

                    if (match == undefined) {
                        continue;
                    }

                    const key: string = (this.mapSplit == true)
                        ? `${match!.mapName}.${iter.definitionName}`
                        : iter.definitionName;

                    let unit: UnitsMade | undefined = map.get(key);
                    if (unit == undefined) {
                        unit = {
                            definitionName: iter.definitionName,
                            unitName: iter.unitName ?? `<missing ${iter.definitionName}>`,
                            map: match?.map ?? "",
                            count: iter.count
                        };
                    } else {
                        unit.count += iter.count;
                    }

                    map.set(key, unit);
                }

                return Loadable.loaded(Array.from(map.values()));
            },

            filteredMatches: function(): Map<string, BarMatch> {
                const map: Map<string, BarMatch> = new Map();

                for (const iter of this.matchDict) {
                    const match = iter[1];

                    if (this.mapFilter != null && match.map != this.mapFilter) {
                        continue;
                    }

                    if (this.gamemodeFilter != null && match.gamemode != this.gamemodeFilter) {
                        continue;
                    }

                    if (this.cutoffTime != 0 && match.startTime.getTime() < this.cutoffTime) {
                        continue;
                    }

                    map.set(iter[0], iter[1]);
                }
                return map;
            },

            uniqueMaps: function(): string[] {
                const set: Set<string> = new Set();
                const ignore: Set<string> = new Set();
                const maps: Set<string> = new Set();

                for (const iter of this.user.unitsMade) {
                    if (set.has(iter.gameID) || ignore.has(iter.gameID)) {
                        continue;
                    }

                    const match: BarMatch | undefined = this.matchDict.get(iter.gameID);
                    if (match == undefined) {
                        console.warn(`UserUnitsMade> missing match for getting unique maps [gameID=${iter.gameID}]`);
                        ignore.add(iter.gameID);
                        continue;
                    }

                    set.add(iter.gameID);
                    maps.add(match.map);
                }

                return Array.from(maps).sort((a, b) => a.localeCompare(b));
            },

            lastUpdated: function(): Date {
                if (this.user.unitsMade.length == 0) {
                    return new Date();
                }

                return new Date(Math.max(...this.user.unitsMade.map(iter => iter.timestamp.getTime())));
            },

            dayStart: function(): Date {
                const now: Date = new Date();
                const dayStart: Date = new Date(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
                return dayStart;
            },

            cutoffTime: function(): number {
                if (this.range == "all_time") {
                    return 0;
                } else if (this.range == "daily") {
                    return this.dayStart.getTime() - (1000 * 60 * 60 * 24);
                } else if (this.range == "weekly") {
                    return this.dayStart.getTime() - (1000 * 60 * 60 * 24 * 7);
                } else if (this.range == "monthly") {
                    return this.dayStart.getTime() - (1000 * 60 * 60 * 24 * 30);
                } else {
                    throw `unchecked range '${this.range}'`;
                }
            },

            cutoffDate: function(): Date {
                return new Date(this.cutoffTime);
            },

            matchDict: function(): Map<string, BarMatch> {
                return new Map(this.matches.map(iter => {
                    return [iter.id, iter];
                }));
            }
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            UnitIcon, ToggleButton
        }
    });
    export default UserUnitsMade;
</script>