<template>
    <div>
        <collapsible header-text="Economy" bg-color="bg-light" size-class="h1">
            <div class="d-flex flex-wrap align-items-center mb-5" style="gap: 1rem; justify-content: space-evenly">
                <div>
                    <template v-for="interest in interestingActions">
                        <h2>{{ (interest.frame / 30) | mduration }}</h2>
                        <h5>{{ interest.action }}</h5>
                    </template>
                </div>

                <div>
                    <h2>{{ totalBuildPower | compact }}</h2>
                    <h5>Peak build power</h5>

                    <h2>{{ (100 - buildPowerUsedAverage) | locale(0) }}%</h2>
                    <h5>
                        Idle build power
                        <info-hover text="Average percentage of build power idle"></info-hover>
                    </h5>
                </div>

                <div>
                    <h2>{{ playerResourceStats.reduce((acc, iter) => (acc += iter.metalUsed), 0) | compact }}</h2>
                    <h5>Metal used</h5>

                    <h2>{{ playerResourceStats.reduce((acc, iter) => (acc += iter.energyUsed), 0) | compact }}</h2>
                    <h5>Energy used</h5>
                </div>

                <div>
                    <div>Most used factory</div>

                    <template v-if="highestProductionFactory != undefined">
                        <img :src="'/image-proxy/UnitPic?defName=' + highestProductionFactory.factoryDefinitionName" height="128" width="128" />

                        <div>{{ highestProductionFactory.totalMade }} units made</div>
                    </template>
                </div>

                <div>
                    <div>Main energy source</div>

                    <img :src="'/image-proxy/UnitPic?defName=' + highestEnergySource.defName" height="128" width="128" />

                    <div>
                        {{ highestEnergySource.energy | compact }}
                        <img src="/img/energy.png" width="24" height="24" />
                    </div>
                </div>
            </div>

            <div class="mb-5">
                <a-table :entries="builders" :hide-paginate="true" default-sort-field="rank" default-sort-order="desc">
                    <a-col>
                        <a-header>
                            <h4 class="mb-0">
                                <b>Builders</b>
                            </h4>
                        </a-header>

                        <a-body v-slot="entry">
                            <img :src="'/image-proxy/UnitIcon?defName=' + entry.defName" height="24" width="24" />
                            {{ entry.name }}
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

            <div class="d-flex mb-5" style="gap: 1rem">
                <div class="flex-grow-1" style="flex-basis: 50%">
                    <a-table :entries="metalProduction" default-sort-field="count" default-sort-order="desc" :hide-paginate="true">
                        <a-col sort-field="name">
                            <a-header>
                                <h4 class="mb-0" style="min-width: 12rem">
                                    <b>Metal</b>
                                </h4>
                            </a-header>

                            <a-body v-slot="entry">
                                <img :src="'/image-proxy/UnitIcon?defName=' + entry.defName" height="24" width="24" />
                                {{ entry.name }}
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

                        <a-col sort-field="metalMade">
                            <a-header>
                                <b>Metal made</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.metalMade | compact }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="energyUsed">
                            <a-header>
                                <b>Energy used</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.energyUsed | compact }}
                            </a-body>
                        </a-col>
                    </a-table>
                </div>

                <div class="flex-grow-1" style="flex-basis: 50%">
                    <a-table :entries="energyProduction" default-sort-field="count" default-sort-order="desc" :hide-paginate="true">
                        <a-col sort-field="name">
                            <a-header>
                                <h4 class="mb-0" style="min-width: 12rem">
                                    <b>Energy</b>
                                </h4>
                            </a-header>

                            <a-body v-slot="entry">
                                <img :src="'/image-proxy/UnitIcon?defName=' + entry.defName" height="24" width="24" />
                                {{ entry.name }}
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

            <div class="d-flex" style="gap: 1rem">
                <div class="flex-grow-1" style="flex-basis: 50%">
                    <a-table :entries="otherBuildings" default-sort-field="count" default-sort-order="desc" :hide-paginate="true">
                        <a-col sort-field="name">
                            <a-header>
                                <h4 class="mb-0" style="min-width: 12rem">
                                    <b>Other buildings</b>
                                </h4>
                            </a-header>

                            <a-body v-slot="entry">
                                <img :src="'/image-proxy/UnitIcon?defName=' + entry.defName" height="24" width="24" />
                                {{ entry.name }}
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

                        <a-col sort-field="metalUsed">
                            <a-header>
                                <b>Metal used</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.metalUsed | compact }}
                            </a-body>
                        </a-col>

                        <a-col sort-field="energyUsed">
                            <a-header>
                                <b>Energy used</b>
                            </a-header>

                            <a-body v-slot="entry">
                                {{ entry.energyUsed | compact }}
                            </a-body>
                        </a-col>
                    </a-table>
                </div>

                <div class="flex-grow-1" style="flex-basis: 50%">
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

    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";
    import MatchWindGraph from "./MatchWindGraph.vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { GameOutput } from "model/GameOutput";
    import { GameEventUnitDef } from "model/GameEventUnitDef";

    import { UnitStats } from "../compute/UnitStatData";
    import MergedStats from "../compute/MergedStats";
    import { ResourceProductionData, ResourceProductionEntry } from "../compute/ResourceProductionData";

    import "filters/LocaleFilter";
    import "filters/CompactFilter";
    import { FactoryData, PlayerFactories } from "../compute/FactoryData";

    type ResourcesByUnitDef = {
        defID: number;
        defName: string;
        name: string;
        energy: number;
        metal: number;
    };

    class InterestingEvent {
        public frame: number = 0;
        public interest: number = 0;
        public action: string = "";
    }

    export const MatchEcoStats = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: { type: Object as PropType<GameOutput>, required: true },
            UnitStats: { type: Array as PropType<UnitStats[]>, required: true },
            UnitResources: { type: Array as PropType<ResourceProductionData[]>, required: true },
            merged: { type: Array as PropType<MergedStats[]>, required: true },
            SelectedTeam: { type: Number, required: true },
        },

        data: function () {
            return {
                interestingActions: [] as InterestingEvent[],

                factories: [] as PlayerFactories[],
            };
        },

        mounted: function (): void {
            this.makeFactoryData();
            this.makeInterstingActions();
        },

        methods: {
            makeFactoryData: function (): void {
                this.factories = [];

                this.factories = PlayerFactories.compute(this.match, this.output);
            },

            makeInterstingActions: function (): void {
                this.interestingActions = [];

                const interest: InterestingEvent[] = [];

                let t1made: boolean = false;
                let t2made: boolean = false;
                let t3made: boolean = false;
                let firstAfus: boolean = false;
                let vehicleSwap: boolean = !(this.match.players.length == 2 && this.match.allyTeams.length == 2); // only interesting for duels

                let botStart: boolean = false;

                for (const ev of this.output.unitsCreated) {
                    if (ev.teamID != this.SelectedTeam) {
                        continue;
                    }

                    const def: GameEventUnitDef | undefined = this.output.unitDefinitions.get(ev.definitionID);
                    if (def == undefined) {
                        continue;
                    }

                    if (vehicleSwap == false && botStart == false) {
                        if (def.isFactory == true && def.name == "Bot Lab" && def.unitGroup == "builder") {
                            console.log(`MatchEcoStats> team ${ev.teamID} started bots`);
                            botStart = true;
                        }
                    } else if (vehicleSwap == false && botStart == true) {
                        if (def.isFactory == true && def.name == "Vehicle Plant" && def.unitGroup == "builder") {
                            interest.push({
                                frame: ev.frame,
                                action: "Bot -> Vehicle swap",
                                interest: 10,
                            });
                            vehicleSwap = true;
                        }
                    }

                    if (t1made == false) {
                        if (def.isFactory == true && def.isFactory == true && def.unitGroup == "builder") {
                            interest.push({
                                frame: ev.frame,
                                action: "T1 made",
                                interest: 1,
                            });
                            t1made = true;
                        }
                    }

                    if (t2made == false) {
                        if (def.isFactory == true && def.isFactory == true && def.unitGroup == "buildert2" && def.speed == 0) {
                            interest.push({
                                frame: ev.frame,
                                action: "T2 made",
                                interest: 2,
                            });
                            t2made = true;
                        }
                    }

                    if (t3made == false) {
                        if (def.isFactory == true && def.isFactory == true && def.unitGroup == "buildert3" && def.speed == 0) {
                            interest.push({
                                frame: ev.frame,
                                action: "Gantry made",
                                interest: 5,
                            });
                            t3made = true;
                        }
                    }

                    if (firstAfus == false) {
                        if (def.energyProduction > 2000 && def.buildTime > 100000) {
                            interest.push({
                                frame: ev.frame,
                                action: "First AFUS",
                                interest: 5,
                            });
                            firstAfus = true;
                        }
                    }
                }

                this.interestingActions = interest.sort((a, b) => b.interest - a.interest).slice(0, 2);
            },

            isBuilder: function (entry: ResourceProductionEntry): boolean {
                return !!entry.definition && entry.definition.buildPower > 0 && entry.definition.isFactory == false;
            },

            isMetalProduction: function (entry: ResourceProductionEntry): boolean {
                return (
                    !!entry.definition &&
                    entry.definition.speed == 0 &&
                    (entry.definition.energyConversionCapacity > 0 || entry.definition.metalMake > 0 || entry.definition.isMetalExtractor > 0)
                );
            },

            isEnergyProduction: function (entry: ResourceProductionEntry): boolean {
                return !!entry.definition && entry.energyMade > 0 && entry.definition.speed == 0;
            },
        },

        computed: {
            playerResourceStats: function (): ResourceProductionEntry[] {
                return this.UnitResources.find((iter) => iter.teamID == this.SelectedTeam)?.units ?? [];
            },

            dataResources: function (): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats);
            },

            builders: function (): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats.filter(this.isBuilder));
            },

            metalProduction: function (): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats.filter(this.isMetalProduction));
            },

            energyProduction: function (): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.playerResourceStats.filter(this.isEnergyProduction));
            },

            otherBuildings: function (): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(
                    this.playerResourceStats.filter((iter) => {
                        return (
                            iter.definition &&
                            this.isMetalProduction(iter) == false &&
                            this.isEnergyProduction(iter) == false &&
                            this.isBuilder(iter) == false &&
                            iter.definition.speed == 0
                        );
                    })
                );
            },

            highestEnergySource: function (): ResourcesByUnitDef {
                const map: Map<number, ResourcesByUnitDef> = new Map();

                for (const iter of this.playerResourceStats) {
                    const entry = map.get(iter.definitionID) ?? {
                        defID: iter.definitionID,
                        defName: iter.defName,
                        name: iter.name,
                        energy: 0,
                        metal: 0,
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
                        metal: 0,
                    };
                }
                return arr[0];
            },

            highestProductionFactory: function (): FactoryData | undefined {
                const fac: PlayerFactories | undefined = this.factories.find((iter) => iter.teamID == this.SelectedTeam);
                if (fac == undefined || fac.factories.length == 0) {
                    return undefined;
                }

                return [...fac.factories].sort((a, b) => {
                    return b.totalMade - a.totalMade;
                })[0];
            },

            buildPowerUsedAverage: function (): number {
                const team = this.merged.filter((iter) => iter.teamID == this.SelectedTeam);
                const sum: number = team.reduce((acc, iter) => (acc += (iter.buildPowerUsed / Math.max(1, iter.buildPowerAvailable)) * 100), 0);

                return sum / Math.max(1, team.length);
            },

            totalBuildPower: function (): number {
                return Math.max(...this.merged.filter((iter) => iter.teamID == this.SelectedTeam).map((iter) => iter.buildPowerAvailable));
            },

            playerStats: function (): UnitStats[] {
                return this.UnitStats.filter((iter) => iter.teamID == this.SelectedTeam);
            },

            data: function (): Loading<UnitStats[]> {
                return Loadable.loaded(this.playerStats);
            },
        },

        watch: {
            SelectedTeam: function () {
                this.makeInterstingActions();
            },
        },

        components: {
            ATable,
            AHeader,
            ABody,
            AFooter,
            AFilter,
            ACol,
            Collapsible,
            InfoHover,
            MatchWindGraph,
        },
    });
    export default MatchEcoStats;
</script>
