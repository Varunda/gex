
<template>
    <div>
        <collapsible header-text="Combat" bg-color="bg-light" size-class="h1">

            <div class="mb-5">
                <h4>Most used</h4>

                <div class="d-flex flex-wrap align-items-center" style="gap: 1rem; justify-content: space-evenly;">
                    <div v-for="mostUsed in playerMostUsed" class="text-center border position-sticky" :key="mostUsed.defID" style="border-radius: 0.5rem;">
                        <div class="text-outline px-2 py-1" style="position: absolute; top: 0; background-color: #00000066; border-radius: 0.25rem 0 0.25rem 0;">
                            {{ mostUsed.name }}
                        </div>

                        <img :src="'/image-proxy/UnitPic?defName=' + mostUsed.defName" height="128" width="128" :title="mostUsed.name" style="border-radius: 0.5rem 0.5rem 0 0;">
                        <div>
                            <div>
                                {{ mostUsed.produced }} made
                            </div>

                            <div>
                                {{ mostUsed.kills }} kills
                            </div>
                        </div>
                    </div>

                    <div class="text-center">
                        <h4>
                            Metal ratio - {{ totalMetalKilled / totalMetalLost * 100 | locale(0) }}%
                            <info-hover text="Total metal killed over total metal lost"></info-hover>
                        </h4>

                        <div style="height: 200px; max-height: 200px">
                            <canvas id="combat-metal-efficiency" height="200"></canvas>
                        </div>
                    </div>

                    <div class="text-center">
                        <h4>Damage efficiency - {{ totalDamageDealt / totalDamageTaken * 100 | locale(0) }}%</h4>

                        <div style="height: 200px; max-height: 200px">
                            <canvas id="combat-damage" height="200"></canvas>
                        </div>
                    </div>

                    <div class="text-center">
                        <h2>{{ playerStats.reduce((acc, iter) => acc += iter.mobileKills, 0) | locale(0) }}</h2>
                        <h4>Units killed</h4>

                        <h2>{{ playerStats.reduce((acc, iter) => acc += iter.staticKills, 0) | compact }}</h2>
                        <h4>Buildings<br>destroyed</h4>
                    </div>

                </div>
            </div>

            <div class="mb-5">
                <a-table :entries="dynamicUnits" display-type="table" default-sort-field="rank" default-sort-order="desc"
                    :hide-paginate="true" :paginate="true" :overflow-wrap="true" :default-page-size="25">

                    <a-col sort-field="name">
                        <a-header>
                            <h5 class="mb-0 text-center" style="min-width: 12rem"><b>Mobile units</b></h5>
                        </a-header>

                        <a-body v-slot="entry">
                            <div class="d-flex align-content-center">
                                <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                                <span class="ps-2">
                                    {{ entry.name }}
                                </span>
                                <info-hover :text="entry.definition.tooltip"></info-hover>
                            </div>
                        </a-body>
                    </a-col>

                    <a-col sort-field="rank">
                        <a-header>
                            <b>Made</b>
                            <info-hover text="How many of this unit were produced and shared"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <div class="text-end pe-3">
                                {{ entry.produced }}
                            </div>
                        </a-body>
                    </a-col>

                    <a-col sort-field="kills">
                        <a-header>
                            <b>Kills</b>
                            <info-hover text="How many kills these units got"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <div :class="{ 'text-muted': entry.kills == 0 }" class="text-end">
                                {{ entry.kills }}
                            </div>
                        </a-body>
                    </a-col>

                    <a-col sort-field="lost" :border-end="true">
                        <a-header>
                            <b>Lost</b>
                            <info-hover text="How many of this unit were lost"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <div :class="{ 'text-muted': entry.lost == 0 }" class="text-end">
                                {{ entry.lost }}
                            </div>
                        </a-body>
                    </a-col>

                    <a-col sort-field="damageDealt">
                        <a-header>
                            <div class="text-center">
                                <b>Dmg dealt</b>
                            </div>
                        </a-header>

                        <a-body v-slot="entry">
                            <div :class="{ 'text-muted': entry.damageDealt == 0 }" class="text-end w-100">
                                {{ entry.damageDealt | compact }}
                            </div>
                        </a-body>
                    </a-col>

                    <a-col sort-field="damageRatio">
                        <a-header>
                            <div class="text-center">
                                <b>Dmg eff</b>
                            </div>
                        </a-header>

                        <a-body v-slot="entry" :border-end="true">
                            <div :class="{ 'text-muted': entry.damageRatio == 0 }" class="text-end w-100">
                                {{ entry.damageRatio * 100 | locale(0) }}%
                            </div>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalKilled" :col-span="2">
                        <a-header :col-span="2">
                            <div class="text-center">
                                <b>M+E cost killed</b>
                                <info-hover text="The total metal and energy cost of units killed by this type of unit"></info-hover>
                            </div>
                        </a-header>

                        <a-body v-slot:col0="entry" :border-start="true">
                            <div class="d-flex align-items-center">
                                <img src="/img/metal.png" width="20px" height="20px" title="Metal"/>

                                <span :class="{ 'text-muted': entry.metalKilled == 0 }" class="text-end w-100">
                                    {{ entry.metalKilled | compact }}
                                </span>
                            </div>
                        </a-body>

                        <a-body v-slot:col1="entry" :border-end="true">
                            <div class="d-flex align-items-center">
                                <img src="/img/energy.png" width="20px" height="20px" title="Energy"/>

                                <span :class="{ 'text-muted': entry.energyKilled == 0 }" class="text-end w-100">
                                    {{ entry.energyKilled | compact }}
                                </span>
                            </div>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalRatio">
                        <a-header>
                            <div class="text-center">
                                <b>Metal eff</b>
                                <info-hover text="Total metal killed over metal spent to make this unit"></info-hover>
                            </div>
                        </a-header>

                        <a-body v-slot="entry">
                            <div :class="{ 'text-muted': entry.metalKilled == 0 }" class="text-end w-100">
                                {{ entry.metalRatio * 100 | locale(0) }}%
                            </div>
                        </a-body>
                    </a-col>

                </a-table>
            </div>

            <div class="mb-5">
                <a-table :entries="playerEcoKills" display-type="table" default-sort-field="killed" default-sort-order="desc"
                    :hide-paginate="true" :paginate="true" :overflow-wrap="true" :default-page-size="25">

                    <a-col sort-field="name">
                        <a-header>
                            <h5 class="mb-0 text-center" style="min-width: 12rem"><b>Economy damage</b></h5>
                        </a-header>

                        <a-body v-slot="entry">
                            <div class="d-flex align-content-center">
                                <unit-icon v-if="entry.unitStats.definition != undefined" :name="entry.unitStats.defName" :color="entry.unitStats.definition.color" :size="24"></unit-icon>
                                <span class="ps-2">
                                    {{ entry.unitStats.name }}
                                </span>
                                <info-hover v-if="entry.unitStats.definition != undefined" :text="entry.unitStats.definition.tooltip"></info-hover>
                            </div>
                        </a-body>
                    </a-col>

                    <a-col sort-field="produced" :border-end="true">
                        <a-header>
                            <b>Made</b>
                            <info-hover text="How many of this unit were produced or shared"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <div v-if="entry.unitStats.name != 'Total'" class="text-end w-100 pe-3">
                                {{ entry.produced }}
                            </div>
                            <span v-else>
                            </span>
                        </a-body>
                    </a-col>

                    <!-- builder value killed -->
                    <a-col>
                        <a-header :col-span="colSpanBuilder">
                            <div class="text-center">
                                <img src="/img/bp_black.png" width="20px" height="20px"/>
                                <b>Builders killed</b>
                            </div>
                        </a-header>

                        <a-body v-slot="entry">
                            <cell def-name="armck" :value="entry.connT1" :title="'Number of T1 constructors killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveConnT2">
                        <a-body v-slot="entry">
                            <cell def-name="armack" :value="entry.connT2" :title="'Number of T2 constructors killed by a' + entry.unitStats.name "></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveNano">
                        <a-body v-slot="entry">
                            <cell def-name="armnanotc" :value="entry.nano" :title="'Number of nano towers killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveRezbot">
                        <a-body v-slot="entry">
                            <cell def-name="cornecro" :value="entry.rezbot" :title="'Number of rezbots killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <!-- metal eco value killed -->
                    <a-col :border-start="true">
                        <a-header :col-span="colSpanMetal">
                            <div class="text-center">
                                <img src="/img/metal_black.png" width="20px" height="20px"/>
                                <b>Metal eco killed</b>
                            </div>
                        </a-header>

                        <a-body v-slot="entry">
                            <cell def-name="armmex" :value="entry.metalMexT1" :title="'Number of T1 mexes killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveMexT2">
                        <a-body v-slot="entry">
                            <cell def-name="armmoho" :value="entry.metalMexT2" :title="'Number of T2 mexes killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveEconvT1">
                        <a-body v-slot="entry">
                            <cell def-name="armmakr" :value="entry.econvT1" :title="'Number of T1 E convs killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveEconvT2">
                        <a-body v-slot="entry">
                            <cell def-name="armmmkr" :value="entry.econvT2" :title="'Number of T2 E convs killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <!-- energy eco value killed -->
                    <a-col :border-start="true">
                        <a-header :col-span="colSpanEnergy">
                            <div class="text-center">
                                <img src="/img/energy_black.png" width="20px" height="20px"/>
                                <b>Energy eco killed</b>
                            </div>
                        </a-header>

                        <a-body v-slot="entry">
                            <div v-if="anyHaveWind == true">
                                <cell def-name="armwin" :value="entry.wind" :title="'Number of winds killed by a ' + entry.unitStats.name"></cell>
                            </div>

                            <div v-else>
                                <cell def-name="armsolar" :value="entry.solar" :title="'Number of solars killed by a ' + entry.unitStats.name"></cell>
                            </div>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveSolar && anyHaveWind == true">
                        <a-body v-slot="entry">
                            <cell def-name="armsolar" :value="entry.solar" :title="'Number of solars killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveTidal">
                        <a-body v-slot="entry">
                            <cell def-name="armtide" :value="entry.tidal" :title="'Number of tidals killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveAdvSolar">
                        <a-body v-slot="entry">
                            <cell def-name="armadvsol" :value="entry.advSolar" :title="'Number of advanced solars killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveGeo">
                        <a-body v-slot="entry">
                            <cell def-name="armgeo" :value="entry.geoT1" :title="'Number of T1 geos killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveGeoT2">
                        <a-body v-slot="entry">
                            <cell def-name="armageo" :value="entry.geoT2" :title="'Number of T2 geos killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveFusion">
                        <a-body v-slot="entry">
                            <cell def-name="armfus" :value="entry.fusion" :title="'Number of fusions killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>

                    <a-col v-if="anyHaveAdvFusion">
                        <a-body v-slot="entry">
                            <cell def-name="armafus" :value="entry.advFusion" :title="'Number of advanced fusions killed by a ' + entry.unitStats.name"></cell>
                        </a-body>
                    </a-col>
                </a-table>

            </div>

            <a-table :entries="staticUnits" display-type="table" default-sort-field="rank" default-sort-order="desc" :hide-paginate="true" :overflow-wrap="true">
                <a-col sort-field="name">
                    <a-header>
                        <h5 class="mb-0 text-center" style="min-width: 12rem">
                            <b>Structures</b>
                        </h5>
                    </a-header>

                    <a-body v-slot="entry">
                        <div class="d-flex">
                            <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                            <span class="ps-2">
                                {{ entry.name }}
                            </span>
                            <info-hover :text="entry.definition.tooltip"></info-hover>
                        </div>
                    </a-body>
                </a-col>

                <a-col sort-field="rank">
                    <a-header>
                        <b>Produced</b>
                        <info-hover text="How many of this unit were produced"></info-hover>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.produced }}
                    </a-body>
                </a-col>

                <a-col sort-field="kills">
                    <a-header>
                        <b>Kills</b>
                        <info-hover text="How many kills these units got"></info-hover>
                    </a-header>

                    <a-body v-slot="entry">
                        <span :class="{ 'text-muted': entry.kills == 0 }">
                            {{ entry.kills }}
                        </span>
                    </a-body>
                </a-col>

                <a-col sort-field="lost">
                    <a-header>
                        <b>Lost</b>
                        <info-hover text="How many of this unit were lost"></info-hover>
                    </a-header>

                    <a-body v-slot="entry">
                        <span :class="{ 'text-muted': entry.lost == 0 }">
                            {{ entry.lost }}
                        </span>
                    </a-body>
                </a-col>

                <a-col sort-field="metalRatio">
                    <a-header>
                        <b>Metal eff</b>
                        <info-hover text="Total metal worth of units killed by this type of unit"></info-hover>
                    </a-header>

                    <a-body v-slot="entry">
                        <span :class="{ 'text-muted': entry.metalKilled == 0 }">
                            {{ entry.metalRatio * 100 | locale(0) }}%
                        </span>
                    </a-body>
                </a-col>

                <a-col sort-field="damageDealt">
                    <a-header>
                        <b>Dmg dealt</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <span :class="{ 'text-muted': entry.damageDealt == 0 }">
                            {{ entry.damageDealt | compact }}
                        </span>
                    </a-body>
                </a-col>

                <a-col sort-field="damageRatio">
                    <a-header>
                        <b>Dmg eff</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <span :class="{ 'text-muted': entry.damageRatio == 0 }">
                            {{ entry.damageRatio * 100 | locale(0) }}%
                        </span>
                    </a-body>
                </a-col>

                <a-col sort-field="metalKilled">
                    <a-header>
                        <b>M+E cost killed</b>
                        <info-hover text="The total metal and energy cost of units killed by this type of unit"></info-hover>
                    </a-header>

                    <a-body v-slot="entry">
                        <span :class="{ 'text-muted': entry.metalKilled == 0 && entry.energyKilled == 0 }">
                            {{ entry.metalKilled | compact }}&nbsp;M
                            /
                            {{ entry.energyKilled | compact }}&nbsp;E
                        </span>
                    </a-body>
                </a-col>
            </a-table>

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

    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";
    import ChartDataLabels from "chartjs-plugin-datalabels";
    Chart.defaults.color = "white";

    import { UnitStats } from "../compute/UnitStatData";
    import { StatEntity } from "../compute/common";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { GameOutput } from "model/GameOutput";
    import { GameEventUnitDef } from "model/GameEventUnitDef";

    import CompactUtils from "util/Compact";
    import UnitUtil, { UnitEcoType } from "util/Unit";

    import "filters/LocaleFilter";
    import "filters/CompactFilter";

    class EcoKillEntry {
        public unitStats: UnitStats = new UnitStats();
        public entityId: string = "";
        public produced: number = 0;
        public rank: number = 0;
        public killed: number = 0;

        // builder eco
        public connT1: number = 0;
        public connT2: number = 0;
        public nano: number = 0;
        public rezbot: number = 0;

        // metal eco
        public metalMexT1: number = 0;
        public metalMexT2: number = 0;
        public econvT1: number = 0;
        public econvT2: number = 0;

        // energy eco
        public wind: number = 0;
        public solar: number = 0;
        public advSolar: number = 0;
        public tidal: number = 0;
        public geoT1: number = 0;
        public geoT2: number = 0;
        public fusion: number = 0;
        public advFusion: number = 0;
    };

    const Cell = Vue.extend({
        props: {
            DefName: { type: String, required: true },
            value: { type: Number, required: true }
        },

        components: {
            UnitIcon
        },

        computed: {
            picColor: function(): string {
                if (this.value == 0) {
                    return "#464c53";
                }
                return "0";
            }
        },

        template: `
            <div :style="{ 'color': value == 0 ? '#717c86' : 'var(--bs-body-color)', 'gap': '4px' }" class="d-flex align-items-center justify-content-center">
                <unit-icon :name="DefName" :color="picColor" :size="18" :hide-title="true"></unit-icon>
                <span style="padding-left: 1px">
                    {{ value | locale(0) }}
                </span>
            </div>
        `
    });

    export const MatchCombatStats = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: { type: Object as PropType<GameOutput>, required: true },
            UnitStats: { type: Array as PropType<UnitStats[]>, required: true },
            entities: { type: Array as PropType<StatEntity[]>, required: true },
            SelectedEntity: { type: String, required: true }
        },

        data: function() {
            return {
                ecoData: [] as EcoKillEntry[],
                chart: {
                    metalEff: null as Chart | null,
                    damage: null as Chart | null
                }
            }
        },

        created: function(): void {
            this.makeEcoData();
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeCharts();
            });
        },

        methods: {

            makeEcoData: function(): void {
                const ud: Map<string, GameEventUnitDef> = new Map();
                for (const unitDef of this.output.unitDefinitions) {
                    ud.set(unitDef[1].definitionName, unitDef[1]);
                }

                const labeledSets: Set<string> = new Set();

                const entries: EcoKillEntry[] = [];

                for (const unit of this.UnitStats) {
                    const ecoData: EcoKillEntry = {
                        unitStats: unit,
                        entityId: unit.id,
                        killed: 0,
                        produced: unit.produced,
                        rank: unit.rank,
                        
                        connT1: 0,
                        connT2: 0,
                        nano: 0,
                        rezbot: 0,

                        metalMexT1: 0,
                        metalMexT2: 0,
                        econvT1: 0,
                        econvT2: 0,

                        wind: 0,
                        solar: 0,
                        advSolar: 0,
                        tidal: 0,
                        geoT1: 0,
                        geoT2: 0,
                        fusion: 0,
                        advFusion: 0
                    };

                    for (const [defName, count] of unit.unitsKilled) {
                        const def: GameEventUnitDef | undefined = ud.get(defName);
                        if (def == undefined) {
                            console.error(`MatchCombatStats> missing unit definition for killed unit [unit=${unit.defName}] [killed=${defName}]`);
                            continue;
                        }

                        const ecoType: UnitEcoType | null = UnitUtil.getEcoType(def);

                        if (labeledSets.has(defName) == false) {
                            console.log(`MatchCombatStats> eco type of unit found [defName=${defName}] [ecoType=${ecoType}]`);
                            labeledSets.add(defName);
                        }

                        if (ecoType == null) {
                            continue;
                        }

                        ecoData.killed += count;

                        // builder
                        if (ecoType == "rezbot") {
                            ecoData.rezbot += count;
                        } else if (ecoType == "connt1") {
                            ecoData.connT1 += count;
                        } else if (ecoType == "connt2") {
                            ecoData.connT2 += count;
                        } else if (ecoType == "nano") {
                            ecoData.nano += count;
                        }
                        
                        // metal
                        else if (ecoType == "mext1") {
                            ecoData.metalMexT1 += count;
                        } else if (ecoType == "mext2") {
                            ecoData.metalMexT2 += count;
                        } else if (ecoType == "econvt1") {
                            ecoData.econvT1 += count;
                        } else if (ecoType == "econvt2") {
                            ecoData.econvT2 += count;
                        }

                        // energy
                        else if (ecoType == "wind") {
                            ecoData.wind += count;
                        } else if (ecoType == "solar") {
                            ecoData.solar += count;
                        } else if (ecoType == "advsolar") {
                            ecoData.advSolar += count;
                        } else if (ecoType == "tidal") {
                            ecoData.tidal += count;
                        } else if (ecoType == "geot1") {
                            ecoData.geoT1 += count;
                        } else if (ecoType == "geot2") {
                            ecoData.geoT2 += count;
                        } else if (ecoType == "fusion") {
                            ecoData.fusion += count;
                        } else if (ecoType == "advfusion") {
                            ecoData.advFusion += count;
                        }

                        // error
                        else {
                            throw `unchecked ecoType '${ecoType}' from killed unit ${defName}`;
                        }
                    }

                    if (ecoData.killed > 0) {
                        entries.push(ecoData);
                    }
                }

                this.ecoData = entries;

                const map: Map<string, EcoKillEntry> = new Map();
                for (const entry of entries) {
                    const entityEcoEntry: EcoKillEntry = map.get(entry.entityId) ?? new EcoKillEntry();
                    entityEcoEntry.unitStats.name = "Total";
                    entityEcoEntry.entityId = entry.entityId;
                    entityEcoEntry.unitStats.produced = -1;
                    entityEcoEntry.rank = Number.MAX_SAFE_INTEGER;

                    entityEcoEntry.connT1 += entry.connT1;
                    entityEcoEntry.connT2 += entry.connT2;
                    entityEcoEntry.nano += entry.nano;
                    entityEcoEntry.rezbot += entry.rezbot;
                    entityEcoEntry.metalMexT1 += entry.metalMexT1;
                    entityEcoEntry.metalMexT2 += entry.metalMexT2;
                    entityEcoEntry.econvT1 += entry.econvT1;
                    entityEcoEntry.econvT2 += entry.econvT2;
                    entityEcoEntry.wind += entry.wind;
                    entityEcoEntry.solar += entry.solar;
                    entityEcoEntry.tidal += entry.tidal;
                    entityEcoEntry.advSolar += entry.advSolar;
                    entityEcoEntry.geoT1 += entry.geoT1;
                    entityEcoEntry.geoT2 += entry.geoT2;
                    entityEcoEntry.fusion += entry.fusion;
                    entityEcoEntry.advFusion += entry.advFusion;
                    entityEcoEntry.killed += entry.killed;

                    map.set(entry.entityId, entityEcoEntry);
                }

                this.ecoData.push(...Array.from(map.values()));
            },

            makeCharts: function(): void {
                this.makeMetalEffChart();
                this.makeDamageChart();
            },

            makeMetalEffChart: function(): void {
                if (this.chart.metalEff != null) {
                    this.chart.metalEff.destroy();
                    this.chart.metalEff = null;
                }

                const canvas = document.getElementById("combat-metal-efficiency") as HTMLCanvasElement | null; 
                if (canvas == null) {
                    throw `missing #combat-metal-efficiency`;
                }

                this.chart.metalEff = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        labels: [ "Killed", "Lost" ],
                        datasets: [{
                            data: [
                                this.totalMetalKilled,
                                this.totalMetalLost,
                            ],
                            backgroundColor: [
                                "#419d49",
                                "#ba3e33"
                            ],
                            datalabels: {
                                align: "top",
                                anchor: "center"
                            }
                        }]
                    },
                    options: {
                        plugins: {
                            legend: {
                                display: false,
                            },
                            tooltip: {
                                enabled: false
                            },
                            datalabels: {
                                display: true,
                                color: "white",
                                font: {
                                    family: "Atkinson Hyperlegible",
                                    size: 18,
                                },
                                formatter: CompactUtils.compact
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,
                        scales: {
                            y: {
                                ticks: {
                                    display: false,
                                }
                            }
                        }
                    },
                    plugins: [ ChartDataLabels ]
                });
            },

            makeDamageChart: function(): void {
                if (this.chart.damage != null) {
                    this.chart.damage.destroy();
                    this.chart.damage = null;
                }

                const canvas = document.getElementById("combat-damage") as HTMLCanvasElement | null; 
                if (canvas == null) {
                    throw `missing #combat-damage`;
                }

                this.chart.damage = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        labels: [ "Dealt", "Taken" ],
                        datasets: [
                            {
                                label: "Damage ratio",
                                data: [
                                    this.totalDamageDealt,
                                    this.totalDamageTaken,
                                ],
                                backgroundColor: [
                                    "#419d49",
                                    "#ba3e33"
                                ],
                                datalabels: {
                                    align: "top",
                                    anchor: "center"
                                }
                            }
                        ]
                    },
                    options: {
                        plugins: {
                            legend: {
                                display: false,
                            },
                            tooltip: {
                                enabled: false
                            },
                            datalabels: {
                                display: true,
                                color: "white",
                                font: {
                                    family: "Atkinson Hyperlegible",
                                    size: 18,
                                },
                                formatter: CompactUtils.compact
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,
                        scales: {
                            y: {
                                ticks: {
                                    display: false,
                                }
                            }
                        }
                    },
                    plugins: [ ChartDataLabels ]
                });
            },

            getColName: function(index: number): string {
                return `col${index}`;
            }
        },

        computed: {
            data: function(): Loading<UnitStats[]> {
                return Loadable.loaded(this.playerStats);
            },

            playerStats: function(): UnitStats[] {
                return this.UnitStats.filter(iter => iter.id == this.SelectedEntity);
            },

            playerEcoKills: function(): Loading<EcoKillEntry[]> {
                return Loadable.loaded(this.ecoData.filter(iter => iter.entityId == this.SelectedEntity && iter.killed > 0));
            },

            playerMostUsed: function(): UnitStats[] {
                return [...this.playerStats].filter(iter => {
                    return iter.definition && iter.definition?.weaponCount > 0;
                }).sort((a, b) => {
                    return b.metalKilled - a.metalKilled;
                }).slice(0, 3);
            },

            dynamicUnits: function(): Loading<UnitStats[]> {
                return Loadable.loaded(this.UnitStats.filter(iter => {
                    return iter.id == this.SelectedEntity
                        && (iter.definition?.speed ?? 0) > 0 && (iter.definition?.weaponCount ?? 0) > 0;
                }));
            },

            staticUnits: function(): Loading<UnitStats[]> {
                return Loadable.loaded(this.UnitStats.filter(iter => {
                    return iter.id == this.SelectedEntity
                        && (iter.definition?.speed ?? 1) == 0 && (iter.definition?.weaponCount ?? 0) > 0;
                }));
            },

            selectedPlayer: function(): StatEntity | null {
                return this.entities.find(iter => iter.id == this.SelectedEntity) || null;
            },

            totalMetalKilled: function(): number {
                return this.playerStats.reduce((acc, iter) => acc += iter.metalKilled, 0);
            },

            totalMetalLost: function(): number {
                return this.playerStats.reduce((acc, iter) => acc += (iter.lost * (iter.definition?.metalCost ?? 1)), 0);
            },

            totalDamageDealt: function(): number {
                return this.playerStats.reduce((acc, iter) => acc += iter.damageDealt, 0);
            },

            totalDamageTaken: function(): number {
                return this.playerStats.reduce((acc, iter) => acc += iter.damageTaken, 0);
            },

            anyHaveConnT1: function(): boolean {
                return this.ecoData.find(iter => iter.connT1 > 0) != undefined;
            },
            anyHaveConnT2: function(): boolean {
                return this.ecoData.find(iter => iter.connT2 > 0) != undefined;
            },
            anyHaveNano: function(): boolean {
                return this.ecoData.find(iter => iter.nano > 0) != undefined;
            },
            anyHaveRezbot: function(): boolean {
                return this.ecoData.find(iter => iter.rezbot > 0) != undefined;
            },

            anyHaveMexT1: function(): boolean {
                return this.ecoData.find(iter => iter.metalMexT1 > 0) != undefined;
            },
            anyHaveMexT2: function(): boolean {
                return this.ecoData.find(iter => iter.metalMexT2 > 0) != undefined;
            },
            anyHaveEconvT1: function(): boolean {
                return this.ecoData.find(iter => iter.econvT1 > 0) != undefined;
            },
            anyHaveEconvT2: function(): boolean {
                return this.ecoData.find(iter => iter.econvT2 > 0) != undefined;
            },

            anyHaveWind: function(): boolean {
                return this.ecoData.find(iter => iter.wind > 0) != undefined;
            },
            anyHaveSolar: function(): boolean {
                return this.ecoData.find(iter => iter.solar > 0) != undefined;
            },
            anyHaveTidal: function(): boolean {
                return this.ecoData.find(iter => iter.tidal > 0) != undefined;
            },
            anyHaveAdvSolar: function(): boolean {
                return this.ecoData.find(iter => iter.advSolar > 0) != undefined;
            },
            anyHaveGeo: function(): boolean {
                return this.ecoData.find(iter => iter.geoT1 > 0) != undefined;
            },
            anyHaveGeoT2: function(): boolean {
                return this.ecoData.find(iter => iter.geoT2 > 0) != undefined;
            },
            anyHaveFusion: function(): boolean {
                return this.ecoData.find(iter => iter.fusion > 0) != undefined;
            },
            anyHaveAdvFusion: function(): boolean {
                return this.ecoData.find(iter => iter.advFusion > 0) != undefined;
            },

            colSpanBuilder: function(): number {
                return Math.max(1,
                    [
                        this.anyHaveConnT1, this.anyHaveConnT2, this.anyHaveNano, this.anyHaveRezbot
                    ].reduce((acc, iter) => acc += (iter == true ? 1 : 0), 0)
                );
            },

            colSpanMetal: function(): number {
                return Math.max(1,
                    [
                        this.anyHaveMexT1, this.anyHaveMexT2, this.anyHaveEconvT1, this.anyHaveEconvT2
                    ].reduce((acc, iter) => acc += (iter == true ? 1 : 0), 0)
                );
            },

            colSpanEnergy: function(): number {
                return Math.max(1,
                    [
                        this.anyHaveWind, this.anyHaveSolar, this.anyHaveTidal, this.anyHaveAdvSolar, this.anyHaveGeo, this.anyHaveGeoT2, this.anyHaveFusion, this.anyHaveAdvFusion
                    ].reduce((acc, iter) => acc += (iter == true ? 1 : 0), 0)
                );
            },
        },

        watch: {
            SelectedEntity: function(): void {
                this.makeCharts();
            }
        },

        components: {
            Cell,
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            Collapsible, InfoHover, UnitIcon
        }
    });
    export default MatchCombatStats;

</script>
