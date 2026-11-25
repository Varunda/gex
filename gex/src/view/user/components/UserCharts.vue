
<template>
    <div>

        <h2 class="wt-header border-0">
            Charts
        </h2>

        <div class="mt-3 mb-3 text-center">
            <label class="d-block w-100">Gamemode</label>
            <div class="btn-group">
                <button class="btn" :class="[ gamemode == null ? 'btn-primary' : 'btn-secondary' ]" @click="gamemode = null">
                    All
                </button>
                <button class="btn" :class="[ gamemode == 1 ? 'btn-primary' : 'btn-secondary' ]" @click="gamemode = 1">
                    Duel
                </button>
                <button class="btn" :class="[ gamemode == 2 ? 'btn-primary' : 'btn-secondary' ]" @click="gamemode = 2">
                    Small team
                </button>
                <button class="btn" :class="[ gamemode == 3 ? 'btn-primary' : 'btn-secondary' ]" @click="gamemode = 3">
                    Large team
                </button>
                <button class="btn" :class="[ gamemode == 4 ? 'btn-primary' : 'btn-secondary' ]" @click="gamemode = 4">
                    FFA
                </button>
                <button class="btn" :class="[ gamemode == 5 ? 'btn-primary' : 'btn-secondary' ]" @click="gamemode = 5">
                    Team FFA
                </button>
            </div>
        </div>

        <div>
            <h3 class="wt-header d-flex">
                <div class="d-inline flex-grow-1">
                    Win/Loss per {{ winLossPerDay.range }}
                </div>

                <div class="d-inline-block flex-grow-0">
                    <div class="btn-group">
                        <button class="btn" :class="[ winLossPerDay.range == 'day' ? 'btn-primary' : 'btn-secondary' ]" @click="winLossPerDay.range = 'day'">
                            Day
                        </button>
                        <button class="btn" :class="[ winLossPerDay.range == 'week' ? 'btn-primary' : 'btn-secondary' ]" @click="winLossPerDay.range = 'week'">
                            Week
                        </button>
                        <button class="btn" :class="[ winLossPerDay.range == 'month' ? 'btn-primary' : 'btn-secondary' ]" @click="winLossPerDay.range = 'month'">
                            Month
                        </button>
                    </div>
                </div>
            </h3>

            <div class="mb-1">
                <div style="height: 200px; max-height: 200px;">
                    <canvas id="user-win-loss-per-day"></canvas>
                </div>

                <button class="btn btn-secondary btn-sm" @click="resetZoom">
                    Reset zoom
                </button>
            </div>

            <div class="mb-3 text-center">
                Showing win/loss per {{ winLossPerDay.range }} for
                <span v-if="gamemode == null">
                    all
                </span>
                <span v-else>
                    {{ gamemode | gamemode }}
                </span>
                games
                ({{ filteredMatches.length | locale }})
            </div>
        </div>

        <div>
            <h3 class="wt-header">Win rate by duration</h3>

            <div class="mb-1">
                <div style="height: 200px; max-height: 200px;">
                    <canvas id="user-win-rate-by-duration"></canvas>
                </div>
            </div>

            <div class="mb-3 text-center">
                Showing win rate by duration for 
                <span v-if="gamemode == null">
                    all
                </span>
                <span v-else>
                    {{ gamemode | gamemode }}
                </span>
                games
                ({{ filteredMatches.length | locale }})
            </div>
        </div>

        <div>
            <h3 class="wt-header">
                Match durations
            </h3>

            <div class="mb-1">
                <div style="height: 200px; max-height: 200px;">
                    <canvas id="user-chart-duration"></canvas>
                </div>
            </div>

            <div class="mb-3 text-center">
                Showing match duration per minute for 
                <span v-if="gamemode == null">
                    all
                </span>
                <span v-else>
                    {{ gamemode | gamemode }}
                </span>
                games
                ({{ filteredMatches.length | locale }})
            </div>
        </div>

        <div>
            <h3 class="wt-header">
                Opponent skill difference
            </h3>

            <div class="mb-1">
                <div style="height: 200px; max-height: 200px;">
                    <canvas id="user-chart-skill-diff"></canvas>
                </div>
            </div>

            <div class="mb-3 text-center">
                Showing average opponent OS difference over
                <span v-if="gamemode == null">
                    all
                </span>
                <span v-else>
                    {{ gamemode | gamemode }}
                </span>
                games
                ({{ filteredMatches.length | locale }})
            </div>
        </div>

    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import Chart from "chart.js/auto/auto.esm";
    import zoomPlugin from "chartjs-plugin-zoom";
    import "chartjs-adapter-luxon";
    Chart.defaults.font.family = "Atkinson Hyperlegible";
    Chart.register(zoomPlugin);

    import * as lx from "luxon";

    import { BarUser } from "model/BarUser";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";

    import TimeUtils from "util/Time";
    import DateUtil from "util/Date";

    import "filters/LocaleFilter";

    interface WinrateDurationEntry {
        minute: number;
        wins: number;
        total: number;
    }

    interface WinLossPerDayEntry {
        day: Date;
        wins: number;
        losses: number;
    }

    export const UserCharts = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            matches: { type: Array as PropType<BarMatch[]>, required: true },
        },

        data: function() {
            return {
                gamemode: null as number | null,

                winrateDuration: {
                    chart: null as Chart | null,
                    data: new Map() as Map<number, WinrateDurationEntry>,
                },

                duration: {
                    chart: null as Chart | null,
                },

                skillDiff: {
                    chart: null as Chart | null
                },

                winLossPerDay: {
                    chart: null as Chart | null,
                    data: new Map() as Map<number, WinLossPerDayEntry>,
                    range: "week" as "day" | "week" | "month",
                }

            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeAll();
            });
        },

        methods: {

            makeAll: function(): void {
                this.makeWinLossPerDayChart();
                this.makeWinRateDurationChart();
                this.makeDuration();
                this.makeSkillDiff();
            },

            makeWinLossPerDayChart: function(): void {
                if (this.winLossPerDay.chart != null) {
                    this.winLossPerDay.chart.destroy();
                    this.winLossPerDay.chart = null;
                }

                const canvas = document.getElementById("user-win-loss-per-day") as HTMLCanvasElement | null;
                if (canvas == null) {
                    return console.error(`missing #user-win-loss-per-day`);
                }

                this.winLossPerDay.data.clear();
                for (const match of this.filteredMatches) {
                    const matchPlayer: BarMatchPlayer | undefined = match.players.find(iter => iter.userID == this.user.userID);
                    if (matchPlayer == undefined) {
                        debugger;
                        throw `UserInfo> user ${this.user.userID} was not found in match ${match.id}: ${match.players.map(iter => `${iter.username}/${iter.userID}`).join(", ")}`;
                    }

                    const allyTeam: BarMatchAllyTeam | undefined = match.allyTeams.find(iter => iter.allyTeamID == matchPlayer.allyTeamID);
                    if (allyTeam == undefined) {
                        debugger;
                        throw `UserInfo> user ${this.user.userID} on ally team ${matchPlayer.allyTeamID} was not found in match ${match.id}: ${match.allyTeams.map(iter => iter.allyTeamID).join(", ")}`;
                    }

                    let period: Date = new Date(match.startTime.toDateString());
                    if (this.winLossPerDay.range == "week") {
                        const dt = lx.DateTime.fromJSDate(match.startTime);
                        period = lx.DateTime.fromISO(`${dt.year}W${dt.weekNumber}`).toJSDate();
                    } else if (this.winLossPerDay.range == "month") {
                        const dt = lx.DateTime.fromJSDate(match.startTime);
                        period = lx.DateTime.fromISO(`${dt.year}-${dt.month.toString().padStart(2, '0')}`).toJSDate();
                    }

                    const day: number = period.getTime();

                    if (this.winLossPerDay.data.has(day) == false) {
                        console.log(`UserCharts> win-loss-per-day ${match.startTime} -> ${day}`);
                    }

                    let entry: WinLossPerDayEntry = this.winLossPerDay.data.get(day) ?? { 
                        day: new Date(day),
                        wins: 0,
                        losses: 0
                    };

                    if (allyTeam.won == true) {
                        ++entry.wins;
                    } else {
                        ++entry.losses;
                    }

                    this.winLossPerDay.data.set(day, entry);
                }

                this.winLossPerDay.chart = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        datasets: [
                            {
                                label: "Wins",
                                backgroundColor: "green",
                                borderColor: "green",
                                data: Array.from(this.winLossPerDay.data.values()).sort((a, b) => {
                                    return a.day.getTime() - b.day.getTime();
                                }).map(iter => {
                                    return {
                                        x: iter.day.getTime(),
                                        y: iter.wins
                                    }
                                })
                            },
                            {
                                label: "Losses",
                                backgroundColor: "red",
                                borderColor: "red",
                                data: Array.from(this.winLossPerDay.data.values()).sort((a, b) => {
                                    return a.day.getTime() - b.day.getTime();
                                }).map(iter => {
                                    return {
                                        x: iter.day.getTime(),
                                        y: iter.losses
                                    }
                                })
                            }
                        ]
                    },
                    options: {
                        scales: {
                            x: {
                                type: "linear",
                                ticks: {
                                    color: "#fff",
                                    autoSkip: false,
                                    callback: function(value) {
                                        if (typeof(value) == "string") {
                                            return value;
                                        }

                                        return DateUtil.getLocalDateOnlyString(new Date(value));
                                    }
                                },
                                grid: {
                                    display: true,
                                    color: "#555"
                                },
                                stacked: true,
                                max: Math.max(...this.filteredMatches.map(iter => iter.startTime.getTime()))
                            },
                            y: {
                                stacked: true,
                                ticks: {
                                    color: "#fff",
                                    precision: 0
                                },
                                beginAtZero: true,
                                grid: {
                                    color: "#777"
                                }
                            },
                        },

                        interaction: {
                            intersect: false,
                            mode: "index"
                        }
                        ,
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

                            tooltip: {
                                mode: "index",

                                callbacks: {
                                    title: (tooltip) => {
                                        const time: number = tooltip[0].parsed.x;
                                        return DateUtil.getLocalDateOnlyString(new Date(time));
                                    },
                                }
                            },

                            zoom: {
                                pan: {
                                    enabled: true,
                                    modifierKey: "ctrl",
                                    mode: "x",
                                },
                                zoom: {
                                    drag: {
                                        enabled: true
                                    },
                                    mode: "x"
                                }
                            },
                        },
                    },
                });

            },

            makeWinRateDurationChart: function(): void {
                if (this.winrateDuration.chart != null) {
                    this.winrateDuration.chart.destroy();
                    this.winrateDuration.chart = null;
                }

                const canvas = document.getElementById("user-win-rate-by-duration") as HTMLCanvasElement | null;
                if (canvas == null) {
                    return console.error(`missing #user-win-rate-by-duration`);
                }

                const matchCount: number = this.filteredMatches.length;
                const interval: number = matchCount < 20 ? 15
                    : matchCount < 50 ? 10
                    : matchCount < 500 ? 2
                    : 1;

                console.log(`UserInfo> interval ${interval} over ${matchCount} matches`);

                this.winrateDuration.data.clear();
                for (const match of this.filteredMatches) {
                    const matchDurationMins: number = Math.floor(match.durationFrameCount / 30 / 60 / interval);

                    const matchPlayer: BarMatchPlayer | undefined = match.players.find(iter => iter.userID == this.user.userID);
                    if (matchPlayer == undefined) {
                        debugger;
                        throw `UserInfo> user ${this.user.userID} was not found in match ${match.id}: ${match.players.map(iter => `${iter.username}/${iter.userID}`).join(", ")}`;
                    }

                    const allyTeam: BarMatchAllyTeam | undefined = match.allyTeams.find(iter => iter.allyTeamID == matchPlayer.allyTeamID);
                    if (allyTeam == undefined) {
                        debugger;
                        throw `UserInfo> user ${this.user.userID} on ally team ${matchPlayer.allyTeamID} was not found in match ${match.id}: ${match.allyTeams.map(iter => iter.allyTeamID).join(", ")}`;
                    }

                    let entry: WinrateDurationEntry = this.winrateDuration.data.get(matchDurationMins) ?? { 
                        minute: matchDurationMins,
                        wins: 0,
                        total: 0
                    };

                    if (allyTeam.won == true) {
                        ++entry.wins;
                    }
                    ++entry.total;

                    this.winrateDuration.data.set(matchDurationMins, entry);
                }

                const maxInterval: number = Math.max(...Array.from(this.winrateDuration.data.values()).map(iter => iter.total));

                this.winrateDuration.chart = new Chart(canvas.getContext("2d")!, {
                    type: "line",
                    data: {
                        datasets: [
                            {
                                label: "Win rate",
                                backgroundColor: "white",
                                borderColor: "white",
                                tension: 0.2,
                                pointRadius: (ctx) => {
                                    const minute: number = ctx.parsed.x;

                                    const entry: WinrateDurationEntry | undefined = this.winrateDuration.data.get(minute);
                                    if (entry == undefined) {
                                        console.warn(`UserInfo> missing WinrateDurationEntry for ${minute}`);
                                    }
                                    return Math.max(3, Math.floor((entry?.total ?? 10) / maxInterval * 12));
                                },
                                data: Array.from(this.winrateDuration.data.values()).sort((a, b) => {
                                    return a.minute - b.minute;
                                }).map(iter => {
                                    return {
                                        x: iter.minute,
                                        y: Math.round(iter.wins / iter.total * 100)
                                    }
                                })
                            }
                        ]
                    },
                    options: {
                        scales: {
                            x: {
                                type: "linear",
                                ticks: {
                                    color: "#fff",
                                    autoSkip: false,
                                    callback: function(value) {
                                        if (typeof(value) == "string") {
                                            return value;
                                        }

                                        return TimeUtils.duration(value * interval * 60);
                                    }
                                },
                                grid: {
                                    display: true,
                                    color: "#555"
                                },
                            },
                            y: {
                                ticks: {
                                    color: "#fff"
                                },
                                beginAtZero: true,
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
                            tooltip: {
                                callbacks: {
                                    title: (tooltip) => {
                                        const minute: number = tooltip[0].parsed.x;
                                        return `${(minute * interval)} - ${(minute + 1) * interval} minutes`;
                                    },
                                    label: (tooltip) => {
                                        const minute: number = tooltip.parsed.x;
                                        const winrate: number = tooltip.parsed.y;

                                        const entry: WinrateDurationEntry | undefined = this.winrateDuration.data.get(minute);
                                        if (entry == undefined) {
                                            console.warn(`UserInfo> missing WinrateDurationEntry for ${minute}`);
                                        }

                                        return `Win: ${winrate}% over ${entry?.total ?? 0} games`;
                                    }
                                }
                            }
                        },
                    },
                });
            },

            makeDuration: function(): void {
                if (this.duration.chart != null) {
                    this.duration.chart.destroy();
                    this.duration.chart = null;
                }

                const canvas = document.getElementById("user-chart-duration") as HTMLCanvasElement | null;
                if (canvas == null) {
                    return console.warn(`UserCharts> cannot make duration chart: #user-chart-duration is missing`);
                }

                let maxMinute: number = 0;
                const map: Map<number, number> = new Map();
                for (const match of this.filteredMatches) {
                    const minute: number = Math.floor(match.durationFrameCount / 30 / 60);
                    map.set(minute, (map.get(minute) ?? 0) + 1);

                    maxMinute = Math.max(maxMinute, minute);
                }

                console.log(`UserCharts> max minute is ${maxMinute}`);
                for (let i = 0; i < maxMinute + 1; ++i) {
                    if (map.has(i) == false) {
                        map.set(i, 0);
                    }
                }

                const entires: [number, number][] = Array.from(map.entries()).sort((a, b) => a[0] - b[0]);
                const minutes: number[] = entires.map(iter => iter[0]);
                const matchCount: number[] = entires.map(iter => iter[1]);

                this.duration.chart = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        labels: minutes.map(iter => {
                            return `${iter} mins`;
                        }),
                        datasets: [{
                            data: matchCount,
                            backgroundColor: "white"
                        } ]
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
                            }
                        },
                    },
                });
            },

            makeSkillDiff: function(): void {
                if (this.skillDiff.chart != null) {
                    this.skillDiff.chart.destroy();
                    this.skillDiff.chart = null;
                }

                const canvas = document.getElementById("user-chart-skill-diff") as HTMLCanvasElement | null;
                if (canvas == null) {
                    return console.warn(`UserCharts> cannot make duration chart: #user-chart-skill-diff is missing`);
                }

                const interval: number = 2;

                const map: Map<number, number> = new Map();

                for (const match of this.filteredMatches) {
                    const userPlayer: BarMatchPlayer | undefined = match.players.find(iter => iter.userID == this.user.userID);
                    if (userPlayer == undefined) {
                        throw `UserCharts> missing user ${this.user.userID} from match ${match.id}`;
                    }

                    const userOs: number = userPlayer.skill;

                    const userAllyTeam: BarMatchAllyTeam | undefined = match.allyTeams.find(iter => iter.allyTeamID == userPlayer.allyTeamID);
                    if (userAllyTeam == undefined) {
                        throw `UserCharts> missing ally team ${userPlayer.allyTeamID} in match ${match.id}`;
                    }

                    const enemyOses: number[] = [];
                    for (const player of match.players) {
                        if (player.allyTeamID != userPlayer.allyTeamID) {
                            enemyOses.push(player.skill);
                        }
                    }

                    const enemyAvgOs: number = enemyOses.reduce((acc, iter) => acc += iter, 0) / Math.max(1, enemyOses.length);
                    const osDiff: number = userOs - enemyAvgOs;

                    const bracket: number = Math.floor(osDiff / interval) * interval;

                    map.set(bracket, (map.get(bracket) ?? 0) + 1);
                }

                const entires: [number, number][] = Array.from(map.entries()).sort((a, b) => a[0] - b[0]);
                const minutes: number[] = entires.map(iter => iter[0]);
                const matchCount: number[] = entires.map(iter => iter[1]);

                this.skillDiff.chart = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        labels: minutes.map(iter => {
                            if (iter > 0) {
                                return `+${iter}`;
                            }
                            return `${iter}`;
                        }),
                        datasets: [{
                            data: matchCount,
                            borderColor: "white",
                            tension: 0.2,
                            backgroundColor: "white"
                        } ]
                    },
                    options: {
                        scales: {
                            x: {
                                type: "linear",
                                ticks: {
                                    color: "#fff",
                                    autoSkip: false,
                                },
                                grid: {
                                    display: true,
                                    color: "#555"
                                },
                            },
                            y: {
                                ticks: {
                                    color: "#fff"
                                },
                                beginAtZero: true,
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
                            },
                            tooltip: {
                                callbacks: {
                                    title: (tooltip) => {
                                        const osDiff: number = tooltip[0].parsed.x;
                                        return `${osDiff > 0 ? "+" : ""}${osDiff} OS`;
                                    },
                                    label: (tooltip) => {
                                        const osDiff: number = tooltip.parsed.x;
                                        const total: number = tooltip.parsed.y;

                                        return `${total} games`; // where average opponent OS is ${osDiff}`;
                                    }
                                }
                            }
                        },
                    },
                });
            },

            resetZoom: function(): void {
                this.winLossPerDay.chart?.resetZoom();
            },

        },

        computed: {

            filteredMatches: function(): BarMatch[] {
                if (this.gamemode == null) {
                    return this.matches;
                }
                return this.matches.filter(iter => iter.gamemode == this.gamemode);
            }

        },

        watch: {
            gamemode: function(): void {
                this.makeAll();
            },

            "winLossPerDay.range": function(): void {
                this.makeWinLossPerDayChart();
            }
        },

        components: {

        },

    });
    export default UserCharts;

</script>