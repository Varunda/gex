<template>
    <div class="container">

        <div class="mb-3 d-flex">
            <img src="/favicon-96x96.png" width="96">

            <div class="ms-3 d-flex flex-column justify-content-center">
                <h2>Gex</h2>
                <p>
                    Beyond All Reason game extractor.
                    <a href="/faq" target="_blank" ref="nofollow">More info</a>
                </p>
            </div>

        </div>

        <div class="mb-3">
            <h2 class="wt-header bg-light text-dark">Leaderboard</h2>

            <div class="d-flex flex-wrap justify-content-around" style="gap: 1rem;">
                <div v-for="group in skillLeaderboardGrouped" :key="group.gamemode">
                    <h3 class="text-center mb-0">
                        {{ group.gamemode | gamemode }}
                    </h3>

                    <table class="table table-sm table-hover">
                        <thead class="table-ligh text-center">
                            <tr>
                                <th>Player</th>
                                <th>Skill</th>
                            </tr>
                        </thead>

                        <tbody>
                            <tr v-for="entry in group.entries" :key="entry.userID">
                                <td class="px-2">
                                    <a :href="'/user/' + entry.userID">
                                        {{ entry.username }}
                                    </a>
                                </td>
                                <td class="px-2">
                                    {{ entry.skill | locale(2) }}
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <div class="mb-3">
            <h2 class="wt-header bg-light text-dark">
                Recent games
                <a href="/recent" class="fs-4" target="_blank" ref="nofollow">
                    View more
                </a>
            </h2>

            <div v-if="recent.state == 'idle'"></div>

            <div v-else-if="recent.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="recent.state == 'loaded'">
                <match-list :matches="recent.data"></match-list>

                <div v-if="recent.data.length == 0">
                    No matches found!
                </div>
            </div>

            <div v-else-if="recent.state == 'error'">
                <api-error :error="recent.problem"></api-error>
            </div>
        </div>

        <div class="mb-3">
            <h2 class="wt-header bg-light text-dark">Recent maps played (24h)</h2>

            <div class="d-flex flex-wrap justify-content-around" style="gap: 2rem;">
                <div v-for="group in mapPlayRecentGroups" :key="group.gamemode">
                    <h3 class="text-center mb-0">
                        {{ group.gamemode | gamemode }}
                    </h3>

                    <table class="table table-sm table-hover">
                        <thead class="table-ligh text-center">
                            <tr>
                                <th>Map</th>
                                <th>Plays</th>
                            </tr>
                        </thead>

                        <tbody>
                            <tr v-for="entry in group.entries" :key="entry.map">
                                <td class="px-2">
                                    {{ entry.map }}
                                </td>
                                <td class="px-2">
                                    {{ entry.count | locale(0) }}
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>

        <div class="mb-3">
            <h2 class="wt-header bg-light text-dark">
                OpenSkill distribution
                <info-hover text="Excludes players with less than 5 games played, and exludes the default skill"></info-hover>
            </h2>

            <div style="height: 200px; max-height: 200px" class="mw-100">
                <canvas id="skill-histogram" height="200"></canvas>
            </div>
        </div>

    </div>
</template>

<style scoped>

    @media (max-width: 768px) {
        .fetch-interval {
            order: 99;
        }
    }

</style>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";

    import InfoHover from "components/InfoHover.vue";
    import MatchList from "components/app/MatchList.vue";
    import ToggleButton from "components/ToggleButton";
    import ApiError from "components/ApiError";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchApi } from "api/BarMatchApi";
    import { BarLeaderboardApi } from "api/BarLeaderboardApi";
    import { SkillHistogramApi } from "api/SkillHistogramApi";
    import { MapPlayCountApi } from "api/MapPlayCountApi";

    import { BarLeaderboardSkillEntry } from "model/BarLeaderboardSkillEntry";
    import { SkillHistogramEntry } from "model/SkillHistogramEntry";
    import { MapPlayCountEntry } from "model/MapPlayCountEntry";

    import "filters/MomentFilter";
    import "filters/LocaleFilter";
    import "filters/BarGamemodeFilter";

    type GroupedSkillLeaderboard = {
        gamemode: number;
        entries: BarLeaderboardSkillEntry[];
    };

    type GroupedMapPlayCount = {
        gamemode: number;
        entries: MapPlayCountEntry[];
    };

    export const Mainpage = Vue.extend({
        props: {

        },

        data: function() {
            return {
                recent: Loadable.idle() as Loading<BarMatch[]>,
                skillLeaderboard: Loadable.idle() as Loading<BarLeaderboardSkillEntry[]>,
                skillHistogram: Loadable.idle() as Loading<SkillHistogramEntry[]>,
                recentMaps: Loadable.idle() as Loading<MapPlayCountEntry[]>,

                histogramChart: null as Chart | null
            }
        },

        created: function(): void {
            document.title = "Gex";
        },

        beforeMount: function(): void {
            this.loadRecent();
            this.loadSkillLeaderboard();
            this.loadSkillHistogram();
            this.loadRecentMaps();
        },

        methods: {

            loadRecent: async function(): Promise<void> {
                this.recent = Loadable.loading();
                this.recent = await BarMatchApi.search(0, 8, {
                    processingAction: true
                });
            },

            loadSkillLeaderboard: async function(): Promise<void> {
                this.skillLeaderboard = Loadable.loading();
                this.skillLeaderboard = await BarLeaderboardApi.getSkillLeaderboard();
            },

            loadRecentMaps: async function(): Promise<void> {
                this.recentMaps = Loadable.loading();
                this.recentMaps = await MapPlayCountApi.getRecent();
            },

            loadSkillHistogram: async function(): Promise<void> {
                this.skillHistogram = Loadable.loading();
                this.skillHistogram = await SkillHistogramApi.getHistogram();

                if (this.skillHistogram.state != "loaded") {
                    return;
                }

                if (this.histogramChart != null) {
                    this.histogramChart.destroy();
                    this.histogramChart = null;
                }

                const canvas = document.getElementById("skill-histogram") as HTMLCanvasElement | null; 
                if (canvas == null) {
                    throw `missing #skill-histogram`;
                }

                this.histogramChart = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        labels: this.skillHistogram.data.map(iter => `${iter.skillLowerBound}-${iter.skillLowerBound + 1}`),
                        datasets: [
                            {
                                data: this.skillHistogram.data.map(iter => iter.playerCount),
                                backgroundColor: "#fff"
                            }
                        ]
                    },
                    options: {
                        plugins: {
                            legend: {
                                display: false,
                            },
                            tooltip: {
                                mode: "x",
                                intersect: false
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,
                    }
                })
            }

        },

        computed: {
            skillLeaderboardGrouped: function(): GroupedSkillLeaderboard[] {
                if (this.skillLeaderboard.state != "loaded") {
                    return [];
                }

                return this.skillLeaderboard.data.reduce((acc: GroupedSkillLeaderboard[], iter: BarLeaderboardSkillEntry) => {
                    const g = acc.find(i => i.gamemode == iter.gamemode);

                    if (g == undefined) {
                        acc.push({
                            gamemode: iter.gamemode,
                            entries: [iter]
                        });
                    } else {
                        g.entries.push(iter);
                    }

                    return acc;
                }, []).sort((a, b) => a.gamemode - b.gamemode);
            },

            mapPlayRecentGroups: function(): GroupedMapPlayCount[] {
                if (this.recentMaps.state != "loaded") {
                    return [];
                }

                return this.recentMaps.data.reduce((acc: GroupedMapPlayCount[], iter: MapPlayCountEntry) => {
                    const g = acc.find(i => i.gamemode == iter.gamemode);

                    if (g == undefined) {
                        acc.push({
                            gamemode: iter.gamemode,
                            entries: [iter]
                        });
                    } else {
                        g.entries.push(iter);
                    }

                    return acc;
                }, []).sort((a, b) => a.gamemode - b.gamemode);
            }
        },

        watch: {

        },

        components: {
            InfoHover, MatchList, ToggleButton, ApiError,
        }
    });

    export default Mainpage;
</script>