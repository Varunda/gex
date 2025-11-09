<template>
    <div class="container">
        yo

        <div style="max-height: 800px">
            <canvas id="map-chart" height="800"></canvas>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { MapPlayCountEntry } from "model/MapPlayCountEntry";

    import { MapPlayCountApi } from "api/MapPlayCountApi";

    import Chart from "chart.js/auto/auto.esm";

    export const RecentMaps = Vue.extend({
        props: {

        },

        data: function() {
            return {
                recent: Loadable.idle() as Loading<MapPlayCountEntry[]>,
                map: new Map() as  Map<string, Map<Date, MapPlayCountEntry>>,
                chart: null as Chart | null
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.loadData();
            });
        },

        methods: {

            loadData: async function(): Promise<void> {
                this.recent = Loadable.loading();
                this.recent = await MapPlayCountApi.getRecent();

                if (this.recent.state == "loaded") {
                    this.makeChart();
                }
            },

            makeChart: function(): void {
                if (this.recent.state != "loaded") {
                    console.error(`RecentMaps> recent is not 'loaded', is ${this.recent.state}`);
                    return;
                }

                this.chart?.destroy();
                this.chart = null;

                const chart: HTMLElement | null = document.getElementById("map-chart");
                if (chart == null) {
                    throw `RecentMaps> missing #map-chart`;
                }

                console.log(`RecentMaps> making chart`);

                this.map = new Map();

                console.time("a");
                const dates: Date[] = Array.from(new Set<Date>(this.recent.data.map(iter => iter.timestamp!)))
                    .sort((a, b) => a.getTime() - b.getTime());
                console.timeEnd("a");

                console.time("b");
                for (const entry of this.recent.data) {
                    const entries: Map<Date, MapPlayCountEntry> = this.map.get(entry.map) ?? new Map();
                    entries.set(entry.timestamp!, entry);
                    this.map.set(entry.map, entries);
                }
                console.timeEnd("b");

                /*
                this.chart = new Chart((chart as HTMLCanvasElement).getContext("2d")!, {
                    type: "line",
                    data: {
                        datasets: Array.from(map.entries()).map(iter => {
                            return {
                                label: iter[0],
                                data: dates.map(d => iter[1].get(d)?.count ?? 0),
                            }
                        })
                    },
                    options: {
                        scales: {
                            x: {
                                ticks: {
                                    display: false,
                                },
                                grid: {
                                    display: true,
                                },
                            },
                            y: {
                                beginAtZero: true,
                                grid: {
                                    display: false,
                                },
                                ticks: {
                                    display: true
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

                });
                */
            }

        },

        components: {

        }
    });
    export default RecentMaps;
</script>