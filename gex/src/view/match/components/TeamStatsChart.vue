
<template>
    <div>
        <h2 class="wt-header bg-info">
            Player stats
        </h2>

        <div class="d-flex flex-row">
            <div class="btn-group btn-group-vertical flex-grow-0" style="text-wrap: nowrap;">
                <button v-for="stat in statNames" :key="stat[0]" @click="showDataset(stat[0])" class="btn" :class="[ showedStat == stat[0] ? 'btn-primary' : 'btn-secondary' ]">
                    {{ stat[1] }}
                </button>
            </div>

            <div style="height: 600px" class="flex-grow-1">
                <canvas id="team-stats-chart" height="600"></canvas>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { GameEventTeamsStats } from "model/GameEventTeamStats";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import Chart, { ChartDataset } from "chart.js/auto/auto.esm";

    import TimeUtils from "util/Time";
    import TableUtils from "util/Table";
    import CompactUtils from "util/Compact";

    const STATS: [(keyof GameEventTeamsStats), string][] = [
        ["damageDealt", "Damage dealt"],
        ["damageReceived", "Damage receieved"],

        ["energyExcess", "Energy excess"],
        ["energyProduced", "Energy produced"],
        ["energyReceived", "Energy receieved"],
        ["energySent", "Energy sent"],
        ["energyUsed", "Energy used"],

        ["metalExcess", "Metal excess"],
        ["metalProduced", "Metal produced"],
        ["metalReceived", "Metal receieved"],
        ["metalSent", "Metal sent"],
        ["metalUsed", "Metal used"],

        ["unitsReceived", "Units received"],
        ["unitsCaptures", "Units captured"],
        ["unitsKilled", "Units killed"],
        ["unitsOutCaptured", "Units out captured"],
        ["unitsProduced", "Units produced"],
        ["unitsSent", "Units sent"]
    ];

    export const TeamStatsChart = Vue.extend({
        props: {
            stats: { type: Array as PropType<GameEventTeamsStats[]>, required: true },
            match: { type: Object as PropType<BarMatch>, required: true }
        },

        data: function() {
            return {
                chart: null as Chart | null,

                datasets: new Map() as Map<string, any>,

                showedStat: "unitsProduced" as keyof GameEventTeamsStats
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeChart();
                this.makeDatasets();
                this.showDataset("unitsProduced");
            });
        },

        methods: {
            makeChart: function(): void {
                if (this.chart != null) {
                    this.chart.destroy();
                    this.chart = null;
                }

                const canvas: HTMLElement | null = document.getElementById("team-stats-chart");
                if (canvas == null) {
                    return console.error(`TeamStatsChart> missing #team-stats-chart`);
                }

                const ctx = (canvas as HTMLCanvasElement).getContext("2d");
                if (ctx == null) {
                    return console.error(`TeamStatsChart> no 2d context?`);
                }

                this.chart = new Chart(ctx, {
                    type: "line",
                    data: {
                        labels: this.stats.map(iter => iter.frame).filter((v, i, arr) => arr.indexOf(v) == i).map(iter => `${TimeUtils.duration(iter / 30)}`),
                        datasets: []
                    },
                    options: {
                        scales: {
                            x: {
                                reverse: false,
                                ticks: {
                                    color: "#fff",
                                },
                                grid: {
                                    color: "#888",
                                    display: true,
                                },
                            },
                            y: {
                                ticks:{
                                    color: "#fff"
                                },
                                grid: {
                                    color: "#bbb"
                                }
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            tooltip: {
                                enabled: false,
                                mode: "index",
                                position: "nearest",
                                intersect: false,
                                external: (ctx) => TableUtils.chart("team-stats-chart-tooltip", ctx, CompactUtils.compact)
                            },
                            legend: {
                                labels: {
                                    color: "#fff",
                                    filter: (item) => item.hidden != true
                                },
                                position: "right"
                            }
                        },
                        hover: {
                            mode: "index",
                            intersect: false
                        }
                    },
                });
            },

            makeDatasets: function(): void {
                if (this.chart == null) {
                    console.log(`TeamStatsChart> chart is null, creating`);
                    this.makeChart();
                }

                if (this.chart == null) {
                    throw `why is chart still null`;
                }

                this.datasets.clear();

                for (const stat of this.statNames) {
                    const map: Map<number, number[]> = new Map();
                    for (const i of this.stats) {
                        const v: number | string = i[stat[0]];
                        if (typeof v == "string") {
                            throw `cannot create dataset on ${stat}, this is a string field!`;
                        }
                        const a: number[] = map.get(i.teamID) ?? [];
                        a.push(v);

                        map.set(i.teamID, a);
                    }

                    for (const entry of map.entries()) {
                        const teamID: number = entry[0];
                        const values: number[] = entry[1];

                        const team: BarMatchPlayer | undefined = this.match.players.find(iter => iter.teamID == teamID);

                        const ds: ChartDataset = {
                            data: values,
                            label: `${team?.username ?? `<missing ${teamID}>`}`,
                            borderColor: team?.hexColor ?? "#333333",
                            backgroundColor: team?.hexColor ?? "#333333",
                            fill: false,
                            hidden: true,
                        };

                        //console.log(`created dataset ${teamID}-${stat[0]}`);
                        this.datasets.set(`${teamID}-${stat[0]}`, ds);

                        this.chart.data.datasets.push(ds);
                    }
                }
            },

            showDataset: function(field: keyof GameEventTeamsStats): void {
                if (this.chart == null) {
                    console.log(`TeamStatsChart> chart is null, creating`);
                    this.makeChart();
                }

                if (this.chart == null) {
                    throw `why is chart still null`;
                }

                this.showedStat = field;

                for (const i of this.chart.data.datasets) {
                    i.hidden = true;
                }
                this.chart.data.datasets.length = 0;

                const keys: Set<string> = new Set(this.teamIds.map(iter => `${iter}-${field}`));
                console.log("TeamStatsChart> keys to add:", keys);

                for (const iter of this.datasets) {
                    if (keys.has(iter[0]) == false) {
                        continue;
                    }

                    const dataset: ChartDataset = iter[1];
                    if (dataset.hidden == true) {
                        console.log(`TeamStatsChart> adding dataset ${iter[0]}`);
                        dataset.hidden = false;
                        this.chart.data.datasets.push(dataset);
                    }
                }

                this.chart.update();
            },
        },

        computed: {
            statNames: function(): [(keyof GameEventTeamsStats), string][] {
                return STATS;
            },

            teamIds: function(): number[] {
                return this.match.players.map(iter => iter.teamID);
            }

        },

        components: {

        }
    });
    export default TeamStatsChart;

</script>