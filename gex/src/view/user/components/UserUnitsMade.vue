<template>
    <div>
        <h2 class="wt-header border-0">
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

        <div class="mb-3 d-grid" style="grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); column-gap: 1rem;">
            <div>
                <label>Map filter</label>
                <select v-model="filter.map" class="form-control" style="max-width: 240px;">
                    <option :value="null">all</option>
                    <option v-for="map in uniqueMaps" :value="map" :key="map">
                        {{ map }}
                    </option>
                </select>
            </div>

            <div>
                <label>Gamemode filter</label>
                <select v-model="filter.gamemode" class="form-control" style="max-width: 240px;">
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
                <label class="d-block">&nbsp;</label>

                <button class="btn w-100" :class="[ filterChanged ? 'btn-primary' : 'btn-secondary' ]" @click="loadFiltered()">
                    Update filter
                </button>
            </div>
        </div>

        <div class="mb-2 d-grid" style="grid-template-columns: repeat(auto-fit, minmax(240px, 1fr)); column-gap: 1rem;">
            <div>
                <label class="d-block">Split by map</label>

                <button class="btn" :class="[ mapSplit == true ? 'btn-primary' : 'btn-outline-light' ]" @click="toggleMapSplit()">
                    Toggle map split
                </button>
            </div>

            <div>
                <label class="d-block">Show debug info</label>

                <toggle-button v-model="showDebug">
                    debug
                </toggle-button>
            </div>

            <div>
                <label class="d-block">Show units unmade</label>

                <toggle-button v-model="showUnmadeUnits">
                    Unmade units
                </toggle-button>

                <toggle-button v-model="showUnmadeExtraUnits">
                    Include extra units
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

        <div v-if="showUnmadeUnits == true">
            <h4>Units that have not been made</h4>

            <div v-for="unit in missingUnits" :key="unit.definitionName" class="d-inline-block btn btn-outline-light p-2 m-2">
                <unit-icon :name="unit.definitionName" :size="24"></unit-icon>
                {{ unit.displayName }} ({{ unit.definitionName }})
            </div>
        </div>

        <div class="mb-2">
            Last updated: {{ lastUpdated | moment }}. Show stats from {{ full.state != "idle" ? filtered.size : "all" }} matches
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import { BarUser } from "model/BarUser";
    import { BarMatch } from "model/BarMatch";
    import { GameUnitsCreated } from "model/GameUnitsCreated";
    import { BarUnitName } from "model/BarUnitName";
    import { ApiBarUnit } from "model/BarUnit";

    import { BarUserApi } from "api/BarUserApi";
    import { BarUnitApi } from "api/BarUnitApi";

    import UnitIcon from "components/app/UnitIcon.vue";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import ToggleButton from "components/ToggleButton";

    import "filters/LocaleFilter";
    import "filters/MomentFilter";

    import ColorUtils from "util/Color";
import { FactionUtil } from "util/Faction";

    type UnitsMade = {
        definitionName: string;
        unitName: string;
        map: string;
        count: number;
    }

    class UnitCategory {
        public name: string = "";
        public order: number = 0;
        public units: ApiBarUnit[] = [];

        public constructor(name: string, order: number) {
            this.name = name;
            this.order = order;
        }
    }

    type CategoryType = "commander" | "factory" | "con" | "eco" | "bot" | "vehicle" | "air" | "sea" | "hover" | "defense" | "unknown";

    class GroupedUnits {
        public faction: string = "";
        public factionID: number = 0;
        public categoryMap: Map<string, ApiBarUnit[]> = new Map();
        public categories: UnitCategory[] = [];
        public units: ApiBarUnit[] = [];

        public constructor(name: string, factionID: number) {
            this.factionID = factionID;
            this.faction = name;
        }
    }

    // get all units that can be built from another unit, and any children of that unit (recursively)
    // this is used to only show the units that with the default settings can be built
    const generateBuildTree:(root: string, units: ApiBarUnit[]) => ApiBarUnit[] = (root, units) => {
        const map: Map<string, ApiBarUnit> = new Map();
        for (const unit of units) {
            map.set(unit.definitionName, unit);
        }

        if (map.get(root) == undefined) {
            throw `Units> missing root unit '${root}'`;
        }

        const queue: string[] = [root];
        const found: Map<string, ApiBarUnit> = new Map();

        found.set(root, map.get(root)!);

        let iter: string | undefined = queue.shift();
        while (iter != undefined) {
            const unit: ApiBarUnit | undefined = map.get(iter);
            if (unit == undefined) {
                throw `Units> missing unit ${iter} from map`;
            }

            for (const opt of unit.unit.buildOptions) {
                const optDef: ApiBarUnit | undefined = map.get(opt);
                if (optDef == undefined) {
                    console.warn(`Units> missing unit from build option [unit=${iter}] [opt=${opt}]`);
                    continue;
                }

                if (found.has(opt) == false) {
                    found.set(opt, optDef);
                    queue.push(opt);
                }
            }

            iter = queue.shift();
        }

        return Array.from(found.values());
    }

    export const UserUnitsMade = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            matches: { type: Array as PropType<BarMatch[]>, required: true }
        },

        data: function() {
            return {

                agg: Loadable.idle() as Loading<BarUser>,

                range: "all_time" as "daily" | "weekly" | "monthly" | "all_time",

                allUnits: Loadable.idle() as Loading<ApiBarUnit[]>,

                filter: {
                    map: null as string | null,
                    gamemode: null as number | null,

                    oldMap: null as string | null,
                    oldGamemode: null as number | null
                },

                full: Loadable.idle() as Loading<GameUnitsCreated[]>,

                filtered: new Map() as Map<string, BarMatch>,

                mapSplit: false as boolean,

                showDebug: false as boolean,
                showTable: true as boolean,
                showUnmadeUnits: false as boolean,
                showUnmadeExtraUnits: false as boolean,
            }
        },

        mounted: function(): void {
            BarUserApi.getUnitsMadeByUserID(this.user.userID).then((value: Loading<BarUser>) => {
                this.agg = value;
            });

            this.loadAllUnits();
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
                this.$nextTick(async () => {
                    await this.loadFiltered();
                    this.showTable = true;
                });
            },

            loadFiltered: async function(): Promise<void> {

                this.filter.oldMap = this.filter.map;
                this.filter.oldGamemode = this.filter.gamemode;

                if (this.full.state == "idle") {
                    this.full = Loadable.loading();
                    this.full = await BarUserApi.getAggregateUnitsMadeByUserID(this.user.userID);
                }

                const map: Map<string, BarMatch> = new Map();

                for (const iter of this.matchDict) {
                    const match = iter[1];

                    if (this.filter.map != null && match.map != this.filter.map) {
                        continue;
                    }

                    if (this.filter.gamemode != null && match.gamemode != this.filter.gamemode) {
                        continue;
                    }

                    if (this.cutoffTime != 0 && match.startTime.getTime() < this.cutoffTime) {
                        continue;
                    }

                    map.set(iter[0], iter[1]);
                }

                this.filtered = map;
            },

            loadAllUnits: async function(): Promise<void> {
                this.allUnits = Loadable.loading();
                this.allUnits = await BarUnitApi.getAllDefinitions();
            }

        },

        computed: {

            missingUnits: function(): ApiBarUnit[] {
                if (this.allUnits.state != "loaded") {
                    return [];
                }

                if (this.agg.state != "loaded") {
                    return [];
                }

                const map: Map<string, ApiBarUnit> = new Map();

                const includedUnits: ApiBarUnit[] = [];

                if (this.showUnmadeExtraUnits == false) {
                    includedUnits.push(...generateBuildTree("armcom", this.allUnits.data));
                    includedUnits.push(...generateBuildTree("corcom", this.allUnits.data));
                    includedUnits.push(...generateBuildTree("legcom", this.allUnits.data));
                } else {
                    includedUnits.push(...this.allUnits.data);
                }

                for (const unit of includedUnits) {
                    map.set(unit.definitionName, unit);
                }

                for (const made of this.agg.data.unitsMade) {
                    map.delete(made.definitionName);
                }

                const arr = Array.from(map.values()).sort((a, b) => {
                    return a.displayName.localeCompare(b.displayName);
                });

                return arr;
            },

            filterChanged: function(): boolean {
                return this.filter.map != this.filter.oldMap || this.filter.gamemode != this.filter.oldGamemode;
            },

            unitsMade: function(): Loading<UnitsMade[]> {
                if (this.full.state == "idle") {
                    if (this.agg.state != "loaded") {
                        return Loadable.rewrap(this.agg);
                    }

                    return Loadable.loaded(this.agg.data.unitsMade.map(iter => {
                        return {
                            definitionName: iter.definitionName,
                            unitName: iter.unitName ?? `<missing ${iter.definitionName}>`,
                            map: "",
                            count: iter.count
                        };
                    }));
                }

                if (this.full.state != "loaded") {
                    return Loadable.rewrap(this.full);
                }

                const map: Map<string, UnitsMade> = new Map();
                for (const iter of this.full.data) {
                    let match: BarMatch | undefined = this.filtered.get(iter.gameID);

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

            uniqueMaps: function(): string[] {
                const set: Set<string> = new Set();
                const ignore: Set<string> = new Set();
                const maps: Set<string> = new Set();

                for (const iter of this.matches) {
                    if (set.has(iter.id) || ignore.has(iter.id) || iter.processing?.actionsParsed == null) {
                        continue;
                    }

                    const match: BarMatch | undefined = this.matchDict.get(iter.id);
                    if (match == undefined) {
                        console.warn(`UserUnitsMade> missing match for getting unique maps [gameID=${iter.id}]`);
                        ignore.add(iter.id);
                        continue;
                    }

                    set.add(iter.id);
                    maps.add(match.map);
                }

                return Array.from(maps).sort((a, b) => a.localeCompare(b));
            },

            lastUpdated: function(): Date {
                if (this.agg.state != "loaded") {
                    return new Date();
                }

                if (this.agg.data.unitsMade.length == 0) {
                    return new Date();
                }

                return new Date(Math.max(...this.agg.data.unitsMade.map(iter => iter.timestamp.getTime())));
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