
<template>
    <div>
        <collapsible header-text="Economy" bg-color="bg-light" size-class="h1">
            <div class="d-flex flex-wrap align-items-center mb-5" style="gap: 1rem; justify-content: space-evenly;">

                <div>
                    <template v-for="interest in milestones">
                        <h2>{{ interest.frame / 30 | mduration }}</h2>
                        <h5>{{ interest.action }}</h5>
                    </template>
                </div>

                <div @click="debug = !debug">
                    <h2> {{ totalBuildPower | compact }}</h2>
                    <h5>Peak build power</h5>

                    <h2>{{ 100 -buildPowerUsedAverage | locale(0) }}%</h2>
                    <h5>
                        Idle build power
                        <info-hover text="Average percentage of build power idle"></info-hover>
                    </h5>
                </div>

                <div>
                    <h2>{{ playerResourceStats.reduce((acc, iter) => acc += iter.metalUsed, 0) | compact }}</h2>
                    <h5>Metal used</h5>

                    <h2>{{ playerResourceStats.reduce((acc, iter) => acc += iter.energyUsed, 0) | compact }}</h2>
                    <h5>Energy used</h5>
                </div>

                <div class="border text-center position-sticky" style="border-radius: 0.5rem;">
                    <template v-if="highestProductionFactory != undefined">
                        <div class="text-outline px-2 py-1" style="position: absolute; top: 0; background-color: #00000066; border-radius: 0.25rem 0 0.25rem 0;">
                            {{ highestProductionFactory.name }}
                        </div>

                        <img :src="'/image-proxy/UnitPic?defName=' + highestProductionFactory.factoryDefinitionName" height="128" width="128" style="border-radius: 0.5rem 0.5rem 0 0">

                        <div>{{ highestProductionFactory.totalMade }} units made</div>
                    </template>
                </div>

                <div class="border text-center position-sticky" style="border-radius: 0.5rem;">
                    <div class="text-outline px-2 py-1" style="position: absolute; top: 0; background-color: #00000066; border-radius: 0.25rem 0 0.25rem 0;">
                        {{ highestEnergySource.name }}
                    </div>
                    <img :src="'/image-proxy/UnitPic?defName=' + highestEnergySource.defName" height="128" width="128" style="border-radius: 0.5rem 0.5rem 0 0">

                    <div>
                        {{ highestEnergySource.energy | compact }} E
                        ({{ highestEnergySource.energy / totalEnergyMade * 100 | locale(0) }}%)
                    </div>
                </div>
            </div>

            <div class="mb-5">
                <a-table :entries="builders" :hide-paginate="true" default-sort-field="rank" default-sort-order="desc" :overflow-wrap="true">
                    <a-col>
                        <a-header>
                            <h4 class="mb-0">
                                <b>Builders</b>
                            </h4>
                        </a-header>

                        <a-body v-slot="entry">
                            <div class="d-flex">
                                <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                                <span class="ps-2">
                                    {{ entry.name }}
                                </span>
                            </div>
                        </a-body>
                    </a-col>

                    <a-col sort-field="rank">
                        <a-header>
                            <b>Produced</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.count }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="lost">
                        <a-header>
                            <b>Lost</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.lost == 0 }">
                                {{ entry.lost }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalUsed">
                        <a-header>
                            <b>Metal used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.metalUsed == 0 }">
                                {{ entry.metalUsed | compact }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalMade">
                        <a-header>
                            <b>Metal made</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.metalMade == 0 }">
                                {{ entry.metalMade | compact }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col>
                        <a-header>
                            <b>Energy used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.energyUsed == 0 }">
                                {{ entry.energyUsed | compact }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col>
                        <a-header>
                            <b>Energy made</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.energyMade == 0 }">
                                {{ entry.energyMade | compact }}
                            </span>
                        </a-body>
                    </a-col>
                </a-table>
            </div>

            <div class="d-flex flex-wrap mb-5" style="gap: 1rem;">
                <div class="flex-grow-1 w-100" style="flex-basis: 48%">
                    <a-table :entries="metalProduction" default-sort-field="count" default-sort-order="desc" :hide-paginate="true" :overflow-wrap="true">
                        <a-col sort-field="name">
                            <a-header>
                                <h4 class="mb-0" style="min-width: 12rem;">
                                    <b>Metal</b>
                                </h4>
                            </a-header>

                            <a-body v-slot="entry">
                                <div class="d-flex">
                                    <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                                    <span class="ps-2">
                                        {{ entry.name }}
                                    </span>
                                </div>
                            </a-body>
                        </a-col>

                        <a-col sort-field="count">
                            <a-header>
                                <b>Created</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.count }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="lost">
                            <a-header>
                                <b>Lost</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.lost }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="reclaimed">
                            <a-header>
                                <b>Reclaimed</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.reclaimed | locale(0) }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="metalMade">
                            <a-header>
                                <b>M made</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.metalMade | compact }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="energyUsed">
                            <a-header>
                                <b>E used</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.energyUsed | compact }}
                            </a-body>
                        </a-col>

                    </a-table>
                </div>

                <div class="flex-grow-1 w-100" style="flex-basis: 48%">
                    <a-table :entries="energyProduction" default-sort-field="count" default-sort-order="desc" :hide-paginate="true" :overflow-wrap="true">
                        <a-col sort-field="name">
                            <a-header>
                                <h4 class="mb-0" style="min-width: 12rem;">
                                    <b>Energy</b>
                                </h4>
                            </a-header>

                            <a-body v-slot="entry">
                                <div class="d-flex">
                                    <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                                    <span class="ps-2">
                                        {{ entry.name }}
                                    </span>
                                </div>
                            </a-body>
                        </a-col>

                        <a-col sort-field="count">
                            <a-header>
                                <b>Created</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.count }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="lost">
                            <a-header>
                                <b>Lost</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.lost }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="reclaimed">
                            <a-header>
                                <b>Reclaimed</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.reclaimed | locale(0) }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="energyMade">
                            <a-header>
                                <b>Energy made</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.energyMade | compact }}
                            </a-body>
                        </a-col>
                    </a-table>
                </div>
            </div>

            <div class="d-flex flex-wrap" style="gap: 1rem;">
                <div class="flex-grow-1 w-100" style="flex-basis: 48%">
                    <a-table :entries="otherBuildings" default-sort-field="count" default-sort-order="desc" :hide-paginate="true" :overflow-wrap="true">
                        <a-col sort-field="name">
                            <a-header>
                                <h4 class="mb-0" style="min-width: 12rem;">
                                    <b>Other buildings</b>
                                </h4>
                            </a-header>

                            <a-body v-slot="entry">
                                <div class="d-flex">
                                    <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                                    <span class="ps-2">
                                        {{ entry.name }}
                                    </span>
                                </div>
                            </a-body>
                        </a-col>

                        <a-col sort-field="count">
                            <a-header>
                                <b>Created</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.count }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="lost">
                            <a-header>
                                <b>Lost</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.lost }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="reclaimed">
                            <a-header>
                                <b>Reclaimed</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.reclaimed | locale(0) }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="metalUsed">
                            <a-header>
                                <b>M used</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.metalUsed | compact }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="energyUsed">
                            <a-header>
                                <b>E used</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.energyUsed | compact }}
                            </a-body>
                        </a-col>
                    </a-table>
                </div>

                <div class="flex-grow-1 w-100" style="flex-basis: 48%">
                    <a-table :entries="unitEffs" default-sort-field="count" default-sort-order="desc" :hide-paginate="true" :overflow-wrap="true">
                        <a-col sort-field="name">
                            <a-header>
                                <h4 class="mb-0" style="min-width: 12rem;">
                                    <b>Misc efficiencies</b>
                                </h4>
                            </a-header>

                            <a-body v-slot="entry">
                                <div class="d-flex">
                                    <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                                    <span class="ps-2">
                                        {{ entry.name }}
                                    </span>
                                </div>
                            </a-body>
                        </a-col>

                        <a-col sort-field="count">
                            <a-header>
                                <b>Created</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.count }}
                            </a-body>
                        </a-col>

                        <a-col>
                            <a-header>
                                <b>Relative cost</b>
                                <info-hover text="Relative cost of a unit in terms of metal (metal cost + energy cost / 70)"></info-hover>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.relativeCost * entry.count | compact }}
                                <span v-if="debug" class="text-muted">
                                    {{ entry.definition.metalCost }} + ({{ entry.definition.energyCost }} / 70)
                                </span>
                            </a-body>
                        </a-col>

                        <a-col sort-field="metalMade">
                            <a-header>
                                <b>M made</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.metalMade | compact }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="metalUsed">
                            <a-header>
                                <b>M eff%</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.metalMade / (entry.relativeCost * entry.count) * 100 | locale(0) }}%
                            </a-body>
                        </a-col>
                    </a-table>

                </div>

            </div>

            <div class="d-flex flex-wrap">
                <div class="flex-grow-1 w-100">
                    <match-wind-graph :updates="output.windUpdates" :map="match.mapData"></match-wind-graph>
                </div>
            </div>

        </collapsible>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol, ARank, ATableType } from "components/ATable";
    import Collapsible from "components/Collapsible.vue";
    import InfoHover from "components/InfoHover.vue";
    import UnitIcon from "components/app/UnitIcon.vue";

    import MatchWindGraph from "./MatchWindGraph.vue";

    import { BarMatch } from "model/BarMatch";
    import { GameOutput } from "model/GameOutput";

    import { UnitStats } from "../compute/UnitStatData";
    import MergedStats from "../compute/MergedStats";
    import { ResourceProductionData, ResourceProductionEntry } from "../compute/ResourceProductionData";
    import { FactoryData, TeamFactories } from "../compute/FactoryData";
    import { Milestone } from "../compute/Milestones";

    import "filters/LocaleFilter";
    import "filters/CompactFilter";

    type ResourcesByUnitDef = {
        defID: number,
        defName: string,
        name: string,
        energy: number,
        metal: number
    };

    export const MatchEcoStats = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: { type: Object as PropType<GameOutput>, required: true },
            UnitStats: { type: Array as PropType<UnitStats[]>, required: true },
            UnitResources: { type: Array as PropType<ResourceProductionData[]>, required: true },
            merged: { type: Array as PropType<MergedStats[]>, required: true },
            SelectedEntity: { type: String, required: true },
        },

        data: function() {
            return {
                milestones: [] as Milestone[],

                factories: [] as TeamFactories[],

                debug: false as boolean
            }
        },

        mounted: function(): void {
            this.makeFactoryData();
            this.makeInterstingActions();
        },

        methods: {

            makeFactoryData: function(): void {
                this.factories = [];
                this.factories = TeamFactories.compute(this.match, this.output);
            },

            makeInterstingActions: function(): void {
                this.milestones = [];
                this.milestones = Milestone.compute(this.match, this.output, this.SelectedEntity).slice(0, 2);
            },

            isBuilder: function(entry: ResourceProductionEntry): boolean {
                return !!entry.definition && entry.definition.buildPower > 0 && entry.definition.isFactory == false;
            },

            isMetalProduction: function(entry: ResourceProductionEntry): boolean {
                return !!entry.definition && entry.definition.speed == 0 && (entry.definition.energyConversionCapacity > 0 || entry.definition.metalMake > 0 || entry.definition.isMetalExtractor > 0);
            },

            isEnergyProduction: function(entry: ResourceProductionEntry): boolean {
                return !!entry.definition && entry.energyMade > 0 && entry.definition.speed == 0;
            },

            isUnitEff: function(entry: ResourceProductionEntry): boolean {
                return !!entry.definition
                    && entry.definition.isCommander == false
                    && entry.definition.energyProduction == 0 // ignores cons
                    && entry.definition.category != "nano"
                    && (
                        entry.definition.isReclaimer
                        || entry.definition.energyConversionCapacity > 0
                    )
            }
        },

        computed: {
            playerResourceStats: function(): ResourceProductionEntry[] {
                return this.UnitResources.find(iter => iter.id == this.SelectedEntity)?.units ?? [];
            },

            dataResources: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats);
            },

            builders: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats.filter(this.isBuilder));
            },

            metalProduction: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats.filter(this.isMetalProduction));
            },

            energyProduction: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats.filter(this.isEnergyProduction));
            },

            otherBuildings: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats.filter(iter => {
                    return iter.definition
                        && this.isMetalProduction(iter) == false
                        && this.isEnergyProduction(iter) == false
                        && this.isBuilder(iter) == false
                        && iter.definition.speed == 0
                }));
            },

            unitEffs: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats.filter(this.isUnitEff));
            },

            highestEnergySource: function(): ResourcesByUnitDef {
                const map: Map<number, ResourcesByUnitDef> = new Map();

                for (const iter of this.playerResourceStats) {
                    const entry = map.get(iter.definitionID) ?? {
                        defID: iter.definitionID,
                        defName: iter.defName,
                        name: iter.name,
                        energy: 0,
                        metal: 0
                    };

                    entry.energy += iter.energyMade;
                    entry.metal += iter.metalMade;

                    map.set(iter.definitionID, entry);
                }

                let arr: ResourcesByUnitDef[] = Array.from(map.values());
                arr = arr.sort((a, b) => {
                    return b.energy - a.energy;
                });

                if (arr.length == 0) {
                    return {
                        defID: -1,
                        defName: "none",
                        name: "none",
                        energy: 0,
                        metal: 0
                    };
                }
                return arr[0];
            },

            highestProductionFactory: function(): FactoryData | undefined {
                const fac: TeamFactories | undefined = this.factories.find(iter => iter.id == this.SelectedEntity);
                if (fac == undefined || fac.factories.length == 0) {
                    return undefined;
                }

                return [...fac.factories].sort((a, b) => {
                    return b.totalMade - a.totalMade;
                })[0];
            },

            buildPowerUsedAverage: function(): number {
                const team = this.merged.filter(iter => iter.id == this.SelectedEntity);
                const sum: number = team.reduce((acc, iter) => acc += (iter.buildPowerUsed / Math.max(1, iter.buildPowerAvailable)) * 100, 0);

                return sum / Math.max(1, team.length);
            },

            totalBuildPower: function(): number {
                return Math.max(...this.merged.filter(iter => iter.id == this.SelectedEntity).map(iter => iter.buildPowerAvailable));
            },

            totalEnergyMade: function(): number {
                return this.UnitResources.filter(iter => iter.id == this.SelectedEntity)
                    .map(iter => iter.units.reduce((acc, iter) => acc += iter.energyMade, 0))
                    .reduce((acc, iter) => acc += iter, 0);
            },

            playerStats: function(): UnitStats[] {
                return this.UnitStats.filter(iter => iter.id == this.SelectedEntity);
            },

            data: function(): Loading<UnitStats[]> {
                return Loadable.loaded(this.playerStats);
            },
        },

        watch: {
            SelectedEntity: function() {
                this.makeInterstingActions();
            }
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            Collapsible, InfoHover, MatchWindGraph, UnitIcon
        }

    });
    export default MatchEcoStats;

</script>