
<template>
    <div>
        <div class="wt-header mb-0" style="white-space: nowrap; text-wrap: wrap;">
            <h4 class="ms-2 d-inline-block mb-0">
                <strong>
                    {{ gamemode.gamemode | gamemode }}
                </strong>
            </h4>

            <wbr/>

            <h6 class="d-inline-block mb-0">
                {{ gamemode.sum.winCount / gamemode.sum.playCount * 100 | locale(0) }}% win rate over {{ gamemode.sum.playCount }} games
            </h6>
        </div>

        <table class="table table-sm mb-1">
            <thead>
                <tr class="table-active">
                    <th class="ps-2">Faction</th>
                    <th>Plays</th>
                    <th>Wins</th>
                    <th>Win %</th>
                </tr>
            </thead>
            
            <tbody>
                <tr v-if="gamemode.armada" is="FactionStatsRow" :data="gamemode.armada" :faction="1"></tr>
                <tr v-if="gamemode.cortex" is="FactionStatsRow" :data="gamemode.cortex" :faction="2"></tr>
                <tr v-if="gamemode.legion" is="FactionStatsRow" :data="gamemode.legion" :faction="3"></tr>
                <tr v-if="gamemode.random" is="FactionStatsRow" :data="gamemode.random" :faction="4"></tr>
                <tr class="table-active" is="FactionStatsRow" :data="gamemode.sum" :faction="0"></tr>
            </tbody>
        </table>

        <span class="text-muted mb-3">
            Average opponent skill is 
            <span v-if="gamemode.averageSkillDiff > 0">
                {{ gamemode.averageSkillDiff | locale(2) }} <abbr title="OpenSkill (elo)">OS</abbr> below this user
            </span>
            <span v-else>
                {{ -1 * gamemode.averageSkillDiff | locale(2) }} <abbr title="OpenSkill (elo)">OS</abbr> above this user
            </span>

            <span v-if="gamemode.wrongSkillCount > 0" class="text-muted">
                (Excluding {{ gamemode.wrongSkillCount }} matches due to the demofiles containing the wrong skill values)
            </span>
        </span>

        <div v-if="showChart" class="my-3">
            <div class="d-flex flew-wrap align-items-center">
                <h3 class="d-inline">Recent {{ recentMatches.length }} games</h3>

                <div class="d-inline border rounded px-2 py-1 mx-2 text-center">
                    <label class="d-inline-block"><b>Skill &Delta;</b></label>
                    <span v-if="skillDiff > 0" style="color: var(--bs-success-text-emphasis)">
                        +{{ skillDiff | locale(2) }} OS
                    </span>
                    <span v-else style="color: var(--bs-danger-text-emphasis)">
                        {{ skillDiff | locale(2) }} OS
                    </span>
                </div>

                <div class="d-inline border rounded px-2 py-1 mx-2 text-center">
                    <label class="d-inline-block"><b>Wins</b></label>
                    <span>
                        {{ winCount }}
                    </span>
                </div>
            </div>

            <div style="max-height: 240px">
                <canvas :id="'user-gamemode-stats-recent-skill-changes-' + gamemode.gamemode" height="200"></canvas>
            </div>

            <span class="text-muted">
                This graph may not contain all matches due to ranked private games
            </span>
        </div>
        <div v-else>
            <h3>
                Recent games
            </h3>

            <span>
                Not enough games
            </span>
        </div>

        <hr class="my-4">
    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";
    import ChartDataLabels from "chartjs-plugin-datalabels";
    Chart.defaults.font.family = "Atkinson Hyperlegible";
    import "chartjs-adapter-luxon";

    import { BarMatch } from "model/BarMatch";
    import { BarUser } from "model/BarUser";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";
    import "filters/BarFactionFilter";
    import "filters/BarGamemodeFilter";

    import { FactionUtil } from "util/Faction";
    import { GamemodeUtil } from "util/Gamemode";
    import ColorUtils from "util/Color";
    import TimeUtils from "util/Time";
    import LocaleUtil from "util/Locale";

    import { GroupedFaction, GroupedFactionGamemode } from "./common";

    const FactionStatsRow = Vue.extend({
        props: {
            faction: { type: Number, required: true },
            data: { type: Object as PropType<GroupedFaction>, required: false }
        },

        template: `
            <tr>
                <td>
                    <span v-if="faction == 0" class="ps-2"><b>Total</b></span>
                    <img v-else-if="faction == 1" src="/img/armada.png" width="24" title="icon for armada">
                    <img v-else-if="faction == 2" src="/img/cortex.png" width="24" title="icon for cortex">
                    <img v-else-if="faction == 3" src="/img/legion.png" width="24" title="icon for legion">
                    <img v-else-if="faction == 4" src="/img/random.png" width="24" title="icon for random">
                    <span v-else>
                        unchecked faction {{ faction }}
                    </span>
                    <span v-if="faction != 0">
                        {{ faction | faction }}
                    </span>
                </td>
                <template v-if="data == null">
                    <td class="text-muted">--</td>
                    <td class="text-muted">--</td>
                    <td class="text-muted">--</td>
                </template>
                <template v-else>
                    <td>{{ data.playCount | locale(0) }}</td>
                    <td>{{ data.winCount | locale(0) }}
                    <td>{{ data.winCount / data.playCount * 100 | locale(0) }}%</td>
                </template>
            </tr>
        `
    });

    export const UserGamemodeStatView = Vue.extend({
        props: {
            gamemode: { type: Object as PropType<GroupedFactionGamemode>, required: true },
            user: { type: Object as PropType<BarUser>, required: true },
            matches: { type: Array as PropType<BarMatch[]>, required: true }
        },

        data: function() {
            return {
                chart: null as Chart | null,

                showChart: true as boolean,

                matchCount: 20 as number
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeCharts();
            });
        },

        methods: {

            makeCharts: function(): void {
                if (this.chart != null) {
                    this.chart.destroy();
                    this.chart = null;
                }

                if (this.recentMatches.length < 5) {
                    this.showChart = false;
                    console.log(`UserGamemodeStats> not showing recent `);
                    return;
                }

                this.showChart = true;

                const elemID: string = `user-gamemode-stats-recent-skill-changes-${this.gamemode.gamemode}`;
                const canvas = document.getElementById(elemID) as HTMLCanvasElement | null;
                if (canvas == null) {
                    console.error(`UserGamemodeStats> missing chart for gamemode [gamemode=${this.gamemode.gamemode}] [elemID=${elemID}]`);
                    return;
                }

                const labels: string[] = this.recentMatches.map((iter, index, arr) => {
                    const label: string = TimeUtils.formatNoTimezone(iter.startTime, "LLL dd");
                    if (index > 0 && (index != arr.length - 1)) {
                        const prevValue: Date = arr[index - 1].startTime;
                        const prevLabel: string = TimeUtils.formatNoTimezone(prevValue, "LLL dd");

                        if (prevLabel == label) {
                            //return "";
                        }
                    }

                    return TimeUtils.formatNoTimezone(iter.startTime, "LLL dd");
                });

                const chart: Chart = new Chart(canvas.getContext("2d")!, {
                    type: "line",
                    data: {
                        labels: this.recentMatches.map((iter, index, arr) => {
                            return TimeUtils.formatNoTimezone(iter.startTime, "LLL dd");
                        }),
                        datasets: [{
                            label: "Skill value",
                            backgroundColor: "white",
                            borderColor: "white",
                            tension: 0.2,
                            data: this.recentMatches.map((iter, index) => {
                                const player: BarMatchPlayer | undefined = iter.players.find(p => p.userID == this.user.userID);
                                if (player == undefined) {
                                    console.log(`UserGamemodeStats> missing player in match they were in ?? [gameID=${iter.id}] [userID=${this.user.userID}]`);
                                }

                                return {
                                    x: index,
                                    y: player?.skill ?? 16.67
                                }
                            }),
                            datalabels: {
                                backgroundColor: "#000",
                                borderColor: "#999",
                                borderRadius: 5,
                                borderWidth: 1,
                                padding: 3,
                                display: function(ctx) {
                                    if (ctx.dataIndex == 0 || ctx.dataIndex == ctx.dataset.data.length - 1) {
                                        return true;
                                    }

                                    const value = ctx.dataset.data[ctx.dataIndex].y;
                                    const prev = ctx.dataset.data[ctx.dataIndex - 1].y;

                                    if (Math.abs(value - prev) <= 0.05) {
                                        return false;
                                    }

                                    return "auto";
                                },
                                align: function(ctx) {
                                    if (ctx.dataIndex == 0) {
                                        return "right";
                                    }
                                    if (ctx.dataIndex == ctx.dataset.data.length - 1) {
                                        return "left";
                                    }
                                    return "top";
                                },
                                color: function(ctx) {
                                    if (ctx.dataIndex == 0) {
                                        return "white";
                                    }

                                    const value = ctx.dataset.data[ctx.dataIndex].y;
                                    const prev = ctx.dataset.data[ctx.dataIndex - 1].y;

                                    if (value - prev >= 0) {
                                        return "#75b798"; // --bs-success-text-emphasis
                                    } else {
                                        return "#ea868f"; // --bs-danger-text-emphasis
                                    }
                                }
                            }
                        }]
                    },
                    options: {
                        scales: {
                            x: {
                                type: "category",
                                ticks: {
                                    color: "#fff",
                                    autoSkip: true,
                                    callback: function(value, index, ticks) {
                                        if (typeof(value) == "string") {
                                            return value;
                                        }

                                        const label: string = labels[value];
                                        if (index > 0 && (index != ticks.length - 1)) {
                                            if (label == labels[value - 1]) {
                                                return "";
                                            }
                                        }

                                        return labels[index];
                                    }
                                },
                                grid: {
                                    display: true,
                                    color: "#555"
                                },
                            },
                            y: {
                                ticks: {
                                    color: "#fff",
                                    precision: 0
                                },
                                grid: {
                                    color: "#777"
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
                                    font: {
                                        family: "Atkinson Hyperlegible"
                                    },
                                }
                            },
                            datalabels: {
                                display: true,
                                color: "white",
                                font: {
                                    family: "Atkinson Hyperlegible",
                                    size: 18,
                                },
                                formatter: (value, ctx) => {
                                    const index: number = ctx.dataIndex;
                                    if (index == 0 || index == ctx.dataset.data.length - 1) {
                                        return value.y;
                                    }

                                    const point: { x: number, y: number} = value;
                                    const prev: { x: number, y: number } = ctx.dataset.data[index - 1];

                                    const diff: number = point.y - prev.y;
                                    if (diff > 0) {
                                        return `+${LocaleUtil.locale(diff, 2)}`;
                                    } else {
                                        return `${LocaleUtil.locale(diff, 2)}`;
                                    }
                                }
                            }
                        },
                    },
                    plugins: [ ChartDataLabels ]
                });

                this.chart = chart;
            },

        },

        computed: {
            recentMatches: function (): BarMatch[] {
                return this.matches.filter(iter => iter.gamemode == this.gamemode.gamemode && iter.gameSettings.ranked_game == "1")
                    .sort((a, b) => {
                        return b.startTime.getTime() - a.startTime.getTime();
                    })
                    .slice(0, this.matchCount)
                    .sort((a, b) => {
                        return a.startTime.getTime() - b.startTime.getTime();
                    });
            },

            skillDiff: function(): number {
                if (this.recentMatches.length < 2) {
                    return 0;
                }

                const last: BarMatch = this.recentMatches[this.recentMatches.length - 1];
                const lastPlayer: BarMatchPlayer | undefined = last.players.find(iter => iter.userID == this.user.userID);
                if (lastPlayer == undefined) {
                    return 0;
                }

                const first: BarMatch = this.recentMatches[0];
                const firstPlayer: BarMatchPlayer | undefined = first.players.find(iter => iter.userID == this.user.userID);
                if (firstPlayer == undefined) {
                    return 0;
                }

                return lastPlayer.skill - firstPlayer.skill;
            },

            winCount: function(): number {
                return this.recentMatches.filter(iter => {
                    const winningAllyTeam: BarMatchAllyTeam | undefined = iter.allyTeams.find(at => at.won == true);
                    if (winningAllyTeam == undefined) {
                        return false;
                    }

                    const player: BarMatchPlayer | undefined = iter.players.find(p => p.userID == this.user.userID);
                    if (player == undefined) {
                        return false;
                    }

                    return player.allyTeamID == winningAllyTeam.allyTeamID;
                }).length;
            },

        },
        
        components: {
            FactionStatsRow
        }
    });
    export default UserGamemodeStatView;

</script>