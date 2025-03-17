
<template>
    <div>
        <h2 class="wt-header">
            Wind over time
        </h2>

        <div>
            Min wind: {{  map.minimumWind }}
        </div>
        <div>
            Max wind: {{  map.maximumWind }}
        </div>
        <div>
            Average wind: {{ avg | locale(2) }}
        </div>
        <div>
            Time at minimum wind: {{ timeAtMin / 30 | mduration }}
        </div>

        <div>
            Time at maximum wind: {{  timeAtMax / 30 | mduration }}
        </div>

        <div style="height: 200px">
            <canvas id="wind-over-time-graph" height="200"></canvas>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { GameEventWindUpdate } from "model/GameEventWindUpdate";
    import { BarMap } from "model/BarMap";

    import Chart from "chart.js/auto/auto.esm";

    import "filters/MomentFilter";
    import "filters/LocaleFilter";
    import TimeUtils from "util/Time";

    export const MatchWindGraph = Vue.extend({
        props: {
            updates: { type: Array as PropType<GameEventWindUpdate[]>, required: true },
            map: { type: Object as PropType<BarMap>, required: true }
        },

        data: function() {
            return {
                chart: null as null | Chart,

                avg: 0 as number,
                timeAtMin: 0 as number,
                timeAtMax: 0 as number
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.calcNumbers();
                this.makeGraph();
            });
        },

        methods: {

            calcNumbers: function(): void {

                for (const ev of this.updates) {
                    this.avg += ev.value;

                    // these are floats, use a larger epsilon than normal
                    if (Math.abs(this.map.minimumWind - ev.value) < 0.1) {
                        this.timeAtMin += 150; // unit: frames
                    }
                    if (Math.abs(this.map.maximumWind - ev.value) < 0.1) {
                        this.timeAtMax += 150; // unit: frames
                    }
                }

                this.avg = this.avg / this.updates.length;
            },

            makeGraph: function(): void {
                const canvas: HTMLElement | null = document.getElementById("wind-over-time-graph");
                if (canvas == null) {
                    return console.error(`MatchWindGraph> missing #wind-over-time-graph`);
                }

                const ctx = (canvas as HTMLCanvasElement).getContext("2d");
                if (ctx == null) {
                    return console.error(`MatchWindGraph> no 2d context?`);
                }

                if (this.chart != null) {
                    this.chart.destroy();
                    this.chart = null;
                }

                this.chart = new Chart(ctx, {
                    type: "line",
                    data: {
                        labels: this.updates.map(iter => `${TimeUtils.duration(iter.frame / 30)}`),
                        datasets: [
                            {
                                label: "Wind speed",
                                data: this.updates.map(iter => { return { x: iter.frame, y: iter.value}}),
                                fill: true,
                                backgroundColor: "#FFFFFFAA"
                            }
                        ]
                    },
                    options: {
                        scales: {
                            x: {
                                reverse: false,
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
                                max: this.map.maximumWind,
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
                                labels: {
                                    color: "#fff",
                                }
                            }
                        }
                    },

                });
            }

        },

        components: {

        }
    });
    export default MatchWindGraph;
</script>