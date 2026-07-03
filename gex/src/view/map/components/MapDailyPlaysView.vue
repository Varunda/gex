
<template>
    <div>
        <h2 class="wt-header">
            Plays per day
            <h5 class="d-inline">(Across all gamemodes)</h5>
        </h2>

        <div style="height: 300px">
            <canvas id="map-daily-plays-chart" height="200"></canvas>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import Chart from "chart.js/auto/auto.esm";
    import * as luxon from "luxon";

    import { BarMap } from "model/BarMap";
    import { MapStats } from "model/map_stats/MapStats";
    import { MapDailyPlays } from "model/map_stats/MapDailyPlays";

    import TimeUtils from "util/Time";

    export const MapDailyPlaysView = Vue.extend({
        props: {
            map: { type: Object as PropType<BarMap>, required: true },
            stats: { type: Object as PropType<MapStats>, required: true }
        },

        data: function() {
            return {
                chart: null as Chart | null
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeChart();
            });
        },

        methods: {

            makeChart: function(): void {

                const canvas: HTMLElement | null = document.getElementById("map-daily-plays-chart");
                if (canvas == null) {
                    return console.error(`MapDailyPlaysView> missing #wind-over-time-graph`);
                }

                const ctx = (canvas as HTMLCanvasElement).getContext("2d");
                if (ctx == null) {
                    return console.error(`MapDailyPlaysView> no 2d context?`);
                }

                if (this.chart != null) {
                    this.chart.destroy();
                    this.chart = null;
                }

                const minTs: Date = luxon.DateTime.fromJSDate(new Date(Math.min(...this.stats.dailyPlays.map(iter => iter.day.getTime())))).toUTC().startOf("day").toJSDate();
                const maxTs: Date = luxon.DateTime.fromJSDate(new Date(Math.max(...this.stats.dailyPlays.map(iter => iter.day.getTime())))).toUTC().startOf("day").toJSDate();

                const map: Map<number, MapDailyPlays> = new Map();
                const days: Date[] = [ minTs ];
                for (let i = minTs.getTime(); i <= maxTs.getTime();) {
                    const next = luxon.DateTime.fromJSDate(new Date(i)).toUTC().plus(luxon.Duration.fromObject({ days: 1 })).toJSDate();
                    days.push(next);

                    i = next.getTime();

                    const plays: MapDailyPlays = new MapDailyPlays();
                    plays.day = new Date(i);

                    map.set(i, plays);
                }

                console.log(days);

                for (const entry of this.stats.dailyPlays) {
                    const key: number = entry.day.getTime();

                    const count: MapDailyPlays | undefined = map.get(key);
                    if (count == undefined) {
                        console.warn(`MapDailyPlaysView> missing key [key=${key} / ${new Date(key)}]`);
                        continue;
                    }

                    count.count = entry.count;
                }

                const entries = Array.from(map.entries()).sort((a, b) => {
                    return a[0] - b[0];
                });

                this.chart = new Chart(ctx, {
                    type: "line",
                    data: {
                        labels: entries.map(iter => TimeUtils.format(new Date(iter[0]), "yyyy-MM-dd")),
                        datasets: [{
                            data: entries.map(iter => iter[1].count),
                            borderColor: "white",
                            pointRadius: 0,
                            tension: 0.3
                        }]
                    },
                    options: {
                        scales: {
                            x: {
                                ticks: {
                                    color: "#fff",
                                },
                                grid: {
                                    color: "#999",
                                    display: false,
                                },
                            },
                            y: {
                                beginAtZero: true,
                                grid: {
                                    color: "#999"
                                }
                            },
                        },
                        interaction: {
                            intersect: false,
                            mode: "nearest"
                        },
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            legend: {
                                display: false,
                                labels: {
                                    color: "#fff",
                                }
                            }
                        },
                    },
                })

            }

        },

        computed: {

        },

        components: {

        }
    });
    export default MapDailyPlaysView;
</script>
