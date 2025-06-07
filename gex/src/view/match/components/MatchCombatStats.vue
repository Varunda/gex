
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
                        <div style="height: 200px; max-height: 200px">
                            <canvas id="combat-metal-efficiency" height="200"></canvas>
                        </div>

                        <div>
                            Metal efficiency
                        </div>
                    </div>

                    <div class="text-center">
                        <div style="height: 200px; max-height: 200px">
                            <canvas id="combat-damage" height="200"></canvas>
                        </div>

                        <div>
                            Damage dealt
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
                <a-table :entries="dynamicUnits" display-type="table" default-sort-field="rank" default-sort-order="desc" :hide-paginate="true">
                    <a-col sort-field="name">
                        <a-header>
                            <h5 class="mb-0 text-center" style="min-width: 12rem"><b>Units</b></h5>
                        </a-header>

                        <a-body v-slot="entry">
                            <img :src="'/image-proxy/UnitIcon?defName=' + entry.defName" height="24" width="24">
                            {{ entry.name }}
                            <info-hover :text="entry.definition.tooltip"></info-hover>
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
                            <b>Metal efficiency</b>
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
                            <b>Eco killed</b>
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
            </div>

            <a-table :entries="staticUnits" display-type="table" default-sort-field="rank" default-sort-order="desc" :hide-paginate="true">
                <a-col sort-field="name">
                    <a-header>
                        <h5 class="mb-0 text-center" style="min-width: 12rem">
                            <b>Structures</b>
                        </h5>
                    </a-header>

                    <a-body v-slot="entry">
                        <img :src="'/image-proxy/UnitIcon?defName=' + entry.defName" height="24" width="24">
                        {{ entry.name }}
                        <info-hover :text="entry.definition.tooltip"></info-hover>
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
                        <b>Metal efficiency</b>
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
                        <b>Eco killed</b>
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

    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";

    import { UnitStats } from "../compute/UnitStatData";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import "filters/LocaleFilter";
    import "filters/CompactFilter";

    export const MatchCombatStats = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            UnitStats: { type: Array as PropType<UnitStats[]>, required: true },
            SelectedTeam: { type: Number, required: true }
        },

        data: function() {
            return {
                chart: {
                    metalEff: null as Chart | null,
                    damage: null as Chart | null
                }
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeCharts();
            });
        },

        methods: {

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
                    type: "pie",
                    data: {
                        labels: [ "Metal killed", "Metal lost" ],
                        datasets: [{
                            data: [
                                this.playerStats.reduce((acc, iter) => acc += iter.metalKilled, 0),
                                this.playerStats.reduce((acc, iter) => acc += (iter.lost * (iter.definition?.metalCost ?? 1)), 0),
                            ],
                            backgroundColor: [
                                "#419d49",
                                "#ba3e33"
                            ]
                        }]
                    },
                    options: {
                        plugins: {
                            legend: {
                                display: false,
                            },
                            tooltip: {
                                enabled: false
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,

                        animation: {
                            onComplete: (ev) => {
                                const chart: Chart = ev.chart;
                                const ctx: CanvasRenderingContext2D = chart.ctx;

                                const centerX = chart.width / 2;
                                const centerY = chart.height / 2;

                                ctx.font = `18px "Atkinson Hyperlegible"`;
                                ctx.fillStyle = "#ffffff";
                                ctx.textAlign = "center";

                                chart.data.datasets.forEach((dataset: ChartDataset, i: number) => {
                                    const meta = chart.getDatasetMeta(i);

                                    meta.data.forEach((bar: Element, index: number) => {
                                        const data = dataset.data[index];
                                        const start: number = (bar as any).startAngle;
                                        const end: number = (bar as any).endAngle;
                                        const radius: number = (bar as any).outerRadius;
                                        const label: string = (chart.data.labels ?? [])[index] as unknown as string;

                                        const center = start + ((end - start) / 2);

                                        const x = Math.cos(center) * (radius / 2);
                                        const y = Math.sin(center) * (radius / 2);

                                        //console.log(`d=${data} sa=${start} ea=${end} x=${x}/${centerX + x} y=${y}/${centerY + y} r=${radius} name=${label} ca=${center}`);

                                        // debug dot that shows where the calculated (x,y) is
                                        //ctx.fillRect(centerX + x - 5, centerY + y - 5, 10, 10);
                                        ctx.fillText(label.replace(" ", "\n"), centerX + x, centerY + y);
                                    });
                                });

                            }
                        }
                    }
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
                    type: "pie",
                    data: {
                        labels: [ "Damage dealt", "Damage taken" ],
                        datasets: [
                            {
                                label: "Damage ratio",
                                data: [
                                    this.playerStats.reduce((acc, iter) => acc += iter.damageDealt, 0),
                                    this.playerStats.reduce((acc, iter) => acc += iter.damageTaken, 0),
                                ],
                                backgroundColor: [
                                    "#419d49",
                                    "#ba3e33"
                                ]
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
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,

                        animation: {
                            onComplete: (ev) => {
                                const chart: Chart = ev.chart;
                                const ctx: CanvasRenderingContext2D = chart.ctx;

                                const centerX = chart.width / 2;
                                const centerY = chart.height / 2;

                                ctx.font = `18px "Atkinson Hyperlegible"`;
                                ctx.fillStyle = "#ffffff";
                                ctx.textAlign = "center";

                                chart.data.datasets.forEach((dataset: ChartDataset, i: number) => {
                                    const meta = chart.getDatasetMeta(i);

                                    meta.data.forEach((bar: Element, index: number) => {
                                        const data = dataset.data[index];
                                        const start: number = (bar as any).startAngle;
                                        const end: number = (bar as any).endAngle;
                                        const radius: number = (bar as any).outerRadius;
                                        const label: string = (chart.data.labels ?? [])[index] as unknown as string;

                                        const center = start + ((end - start) / 2);

                                        const x = Math.cos(center) * (radius / 2);
                                        const y = Math.sin(center) * (radius / 2);

                                        //console.log(`d=${data} sa=${start} ea=${end} x=${x}/${centerX + x} y=${y}/${centerY + y} r=${radius} name=${label} ca=${center}`);

                                        // debug dot that shows where the calculated (x,y) is
                                        //ctx.fillRect(centerX + x - 5, centerY + y - 5, 10, 10);
                                        ctx.fillText(label.replace(" ", "\n"), centerX + x, centerY + y);
                                    });
                                });

                            }
                        }
                    }

                });
            }

        },

        computed: {
            data: function(): Loading<UnitStats[]> {
                return Loadable.loaded(this.playerStats);
            },

            playerStats: function(): UnitStats[] {
                return this.UnitStats.filter(iter => iter.teamID == this.SelectedTeam);
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
                    return iter.teamID == this.SelectedTeam
                        && (iter.definition?.speed ?? 0) > 0 && (iter.definition?.weaponCount ?? 0) > 0;
                }));
            },

            staticUnits: function(): Loading<UnitStats[]> {
                return Loadable.loaded(this.UnitStats.filter(iter => {
                    return iter.teamID == this.SelectedTeam
                        && (iter.definition?.speed ?? 1) == 0 && (iter.definition?.weaponCount ?? 0) > 0;
                }));
            },

            selectedPlayer: function(): BarMatchPlayer | null {
                return this.match.players.find(iter => iter.teamID == this.SelectedTeam) || null;
            }
        },

        watch: {
            SelectedTeam: function(): void {
                this.makeCharts();
            }

        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            Collapsible, InfoHover
        }

    });
    export default MatchCombatStats;

</script>
