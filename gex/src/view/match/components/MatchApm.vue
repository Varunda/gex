<template>
    <collapsible header-text="APM" bg-color="bg-light" size-class="h3">

        <div class="border-bottom pb-2 mb-2">
            <toggle-button v-model="normalizedGraphs">normalize graphs</toggle-button>

            <toggle-button v-model="usePlayerColors">use player colors</toggle-button>
        </div>

        <div class="d-grid align-items-center" style="grid-template-columns: auto auto auto 1fr;">
            <div class="p-2">Player</div>
            <div class="p-2">Average</div>
            <div class="p-2">Peak</div>
            <div></div>

            <template v-for="player in match.players">

                <div class="p-2" style="text-shadow: 1px 1px 1px #000000" :style="{ color: player.hexColor }">
                    {{ player.username }}
                </div>

                <div class="p-2 text-center">
                    {{ avgApm(player.teamID) }}
                </div>

                <div class="p-2 text-center">
                    {{ peakApm(player.teamID) }}
                </div>

                <div style="height: 50px">
                    <canvas :id="'player-apm-' + player.playerID" height="50"></canvas>
                </div>

                <div class="border mb-2"></div>
                <div class="border mb-2"></div>
                <div class="border mb-2"></div>
                <div class="border mb-2"></div>
            </template>
        </div>
    </collapsible>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarMatch } from "model/BarMatch";
    import { GameOutput } from "model/GameOutput";
    import { GameEventExtraStatsUpdate } from "model/GameEventExtraStatsUpdate";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import Collapsible from "components/Collapsible.vue";
    import ToggleButton from "components/ToggleButton";

    import LocaleUtil from "util/Locale";
    import TimeUtils from "util/Time";

    import Chart from "chart.js/auto/auto.esm";

    type ApmInterval = {
        actions: number;
        frames: number;
        apm: number;
    }

    export const MatchApm = Vue.extend({ 
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: {type: Object as PropType<GameOutput>, required: true }
        },

        data: function() {
            return {
                charts: [] as Chart[],
                apms: new Map() as Map<number, ApmInterval[]>,

                normalizedGraphs: true as boolean,
                usePlayerColors: true as boolean
            }
        },

        mounted: function(): void {
            this.makeData();
            this.$nextTick(() => {
                this.makeCharts();
            });
        },

        methods: {
            
            makeData: function(): void {
                const map: Map<number, GameEventExtraStatsUpdate> = new Map();
                const apms: Map<number, { actions: number, frame: number}[]> = new Map();
                const intervals: Map<number, ApmInterval[]> = new Map();

                for (const extraStat of this.output.extraStats) {
                    const last: GameEventExtraStatsUpdate | undefined = map.get(extraStat.teamID);
                    if (last != undefined) {
                        const actionDiff: number = extraStat.actions - last.actions;
                        const frameDiff: number = extraStat.frame - last.frame;

                        const apm: {actions: number, frame: number}[] = apms.get(extraStat.teamID) ?? [];
                        apm.push({
                            actions: actionDiff,
                            frame: frameDiff
                        });

                        const actionCount: number = apm.reduce((acc, iter) => acc += iter.actions, 0);
                        const frameCount: number = apm.reduce((acc, iter) => acc += iter.frame, 0);

                        const int: ApmInterval[] = intervals.get(extraStat.teamID) ?? [];
                        int.push({
                            actions: actionCount,
                            frames: frameCount,
                            apm: actionCount / Math.max(1, frameCount) * 30 * 60
                        });
                        intervals.set(extraStat.teamID, int);

                        if (apm.length >= 4) {
                            apm.shift();
                        }

                        apms.set(extraStat.teamID, apm);
                    }

                    map.set(extraStat.teamID, extraStat);
                }

                this.apms = intervals;
            },

            avgApm: function(teamID: number): string {
                const extraStats: GameEventExtraStatsUpdate[] = this.output.extraStats.filter(iter => iter.teamID == teamID);

                if (extraStats.length == 0) {
                    return `no actions`;
                }

                const actionPeak: number = Math.max(...extraStats.map(iter => iter.actions));
                const framePeak: number = Math.max(...extraStats.map(iter => iter.frame));

                return `${LocaleUtil.locale(actionPeak / framePeak * 30 * 60, 0)}`;
            },

            peakApm: function(teamID: number): string {
                const apms: ApmInterval[] = [...this.apms.get(teamID) ?? []];
                if (apms.length == 0) {
                    return `no apms`;
                }

                const peak = apms.sort((a, b) => { return b.apm - a.apm; })[0];
                return `${LocaleUtil.locale(peak.apm, 0)}`;
            },

            makeCharts: function(): void {
                for (const chart of this.charts) {
                    chart.destroy();
                }
                this.charts = [];

                const labels: string[] = this.output.extraStats.map(iter => TimeUtils.duration(iter.frame / 30))
                    .filter((iter, idx, arr) => arr.indexOf(iter) == idx);

                let maxApm: number = 0;
                for (const entry of this.apms.entries()) {
                    const playerMax: number = Math.max(...entry[1].map(iter => iter.apm));
                    if (playerMax > maxApm) {
                        maxApm = playerMax;
                    }
                }

                console.log(`MatchApm> max apm is ${maxApm}`);

                for (const entry of this.apms.entries()) {
                    const teamID: number = entry[0];
                    const apm: ApmInterval[] = entry[1];

                    const player: BarMatchPlayer | undefined = this.match.players.find(iter => iter.teamID == teamID);

                    const playerMax: number = Math.max(...apm.map(iter => iter.apm));

                    const id: string = `player-apm-${teamID}`;
                    const elem: HTMLElement | null = document.getElementById(id);
                    if (elem == null) {
                        console.error(`MatchApm> missing #${id}`);
                        continue;
                    }

                    this.charts.push(new Chart((elem as HTMLCanvasElement).getContext("2d")!, {
                        type: "line",
                        data: {
                            labels: labels,
                            datasets: [
                                {
                                    data: apm.map(iter => iter.apm),
                                    fill: true,
                                    backgroundColor: this.usePlayerColors == true ? (`${player?.hexColor ?? "#ffffff"}aa`) : "#ffffffaa",
                                    pointRadius: 0,
                                    tension: 0.3
                                }
                            ]
                        },
                        options: {
                            scales: {
                                x: {
                                    ticks: {
                                        display: false,
                                    },
                                    grid: {
                                        display: false,
                                    },
                                },
                                y: {
                                    beginAtZero: true,
                                    max: this.normalizedGraphs == true ? maxApm : playerMax,
                                    grid: {
                                        display: false,
                                    },
                                    ticks: {
                                        display: false
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
                    }));
                }

            },

        },

        watch: {

            normalizedGraphs: function(): void {
                this.makeCharts();
            },

            usePlayerColors: function(): void {
                this.makeCharts();
            }
        },

        components: {
            Collapsible, ToggleButton
        }
    });
    export default MatchApm;

</script>
