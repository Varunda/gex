<template>
    <div>

        <h2 class="wt-header border-0">
            Overview
        </h2>

        <div class="d-flex mb-3 flex-wrap" style="gap: 2rem;">
            <div class="flex-grow-1 text-center flex-basis-0">
                <h2 class="border-bottom d-inline-block px-3 pb-2 mb-2">
                    Favorite faction
                </h2>

                <div :style="factionStyle">
                    <h4 class="mb-1">
                        <faction-icon :faction="mostPlayedFaction.faction" :width="32"></faction-icon>
                        {{ mostPlayedFaction.faction | faction }}
                    </h4>

                    <h4 class="d-flex">
                        <span class="flex-grow-1">
                            {{ mostPlayedFaction.playCount | locale(0) }} played
                        </span>

                        <span class="flex-grow-1">
                            {{ mostPlayedFaction.winCount / mostPlayedFaction.playCount * 100 | locale(0) }}% won
                        </span>
                    </h4>

                    <div style="height: 300px; max-height: 300px; width: 300px; max-width: 300px;" class="ms-auto me-auto">
                        <canvas id="most-played-faction" width="300" height="300"></canvas>
                    </div>
                </div>
            </div>

            <div class="flex-grow-1 text-center flex-basis-0">
                <h2 class="border-bottom d-inline-block px-3 pb-2 mb-2">
                    Favorite gamemode
                </h2>

                <div :style="gamemodeStyle">
                    <h4 class="mb-1">
                        {{ mostPlayedGamemode.gamemode | gamemode }}
                    </h4>

                    <h4 class="d-flex">
                        <span class="flex-grow-1">
                            {{ mostPlayedGamemode.sum.playCount | locale(0) }} played
                        </span>

                        <span class="flex-grow-1">
                            {{ mostPlayedGamemode.sum.winCount / mostPlayedGamemode.sum.playCount * 100 | locale(0) }}% won
                        </span>
                    </h4>

                    <div style="height: 300px; max-height: 300px; width: 300px; max-width: 300px;" class="ms-auto me-auto">
                        <canvas id="most-played-gamemode" width="300" height="300"></canvas>
                    </div>
                </div>
            </div>

            <div class="flex-grow-1 text-center flex-basis-0">
                <h2 class="border-bottom d-inline-block px-3 pb-2 mb-2">
                    Favorite maps
                </h2>

                <div v-if="mostPlayedMaps.length == 0">
                    No maps played
                </div>

                <div v-else>
                    <div class="d-none2 d-lg-block2">
                        <div v-for="map in mostPlayedMaps" :key="map.map" class="mb-3 text-start" style="height: 114px;">
                            <div class="img-overlay max-width"></div>

                            <div class="position-absolute max-width img-map-parent" style="z-index: 0; font-size: 0">
                                <div :style="mapBackground(map)" class="img-map-side img-map-left"></div>
                                <div :style="mapBackground(map)" class="img-map-center"></div>
                                <div :style="mapBackground(map)" class="img-map-side img-map-right"></div>
                            </div>

                            <div style="z-index: 10; position: relative; top: 50%; transform: translateY(-50%); left: 20px;">
                                <img :src="'/image-proxy/MapNameBackground?map=' + map.map + '&size=texture-thumb'" width="80" height="80" class="d-inline corner-img me-2"/>

                                <div class="d-inline-flex flex-column align-items-start" style="vertical-align: top;">
                                    <h4 class="mb-0">
                                        <a :href="'/mapname/' + map.map" class="text-white" style="text-decoration: none;">
                                            {{ map.map }}
                                        </a>
                                    </h4>

                                    <span>
                                        {{ map.playCount }} plays
                                    </span>

                                    <span>
                                        {{ map.winCount / map.playCount * 100 | locale(0) }}% won
                                    </span>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <h6 class="ps-3">
            <a :href="'https://www.bar-stats.pro/playerstats?playerName=' + user.username" class="btn btn-link" target="_blank" ref="nofollow">BarStats link</a>

            <a :href="'https://server4.beyondallreason.info/profile/' + user.userID" class="btn btn-link" target="_blank" ref="nofollow">BAR website account</a>
        </h6>

        <div class="d-none">
            <img id="faction-icon-armada" src="/img/armada.png" height="36" />
            <img id="faction-icon-cortex" src="/img/cortex.png" height="36" />
            <img id="faction-icon-legion" src="/img/legion.png" height="36" />
            <img id="faction-icon-random" src="/img/random.png" height="36" />
        </div>

        <div class="mb-4">
            <h4 class="wt-header bg-light text-dark mb-3">
                <b>Gamemode ratings</b>
            </h4>

            <div class="d-flex flex-wrap justify-content-around border-bottom pb-3 mb-3" style="gap: 1rem;">
                <div v-for="skill in skills" :key="skill.gamemode" class="hoverable text-center mx-2 rounded p-3">
                    <h5 class="border-bottom py-1">
                        {{ skill.gamemode | gamemode }}
                    </h5>

                    <div>
                        {{ skill.skill | locale(2) }}
                        <span class="text-muted">
                            &plusmn;{{ skill.skillUncertainty | locale(2) }}
                        </span>
                    </div>

                    <div :title="skill.lastUpdated | moment">
                        On {{ skill.lastUpdated | moment("yyyy-MM-dd") }}
                    </div>
                </div>
            </div>

            <div style="height: 200px; max-height: 200px;">
                <canvas id="user-info-skill-changes"></canvas>
            </div>

            <div class="text-muted mt-2">
                Data is pulled from demofiles, which can provide incorrect data under certain conditions.
                <a href="/faq#gamemode-demofile-ratings">Learn more here.</a>
            </div>
        </div>
    </div>
</template>

<style scoped>
    /* from: https://stackoverflow.com/a/61913549 by Temani Afif */
    .corner-img {
        --s: 8px; /* size on corner */
        --t: 1px; /* thickness of border */
        --g: 0px; /* gap between border//image */
        
        padding: calc(var(--g) + var(--t));
        outline: var(--t) solid var(--bs-white); /* color here */
        outline-offset: calc(-1*var(--t));
        mask:
            conic-gradient(at var(--s) var(--s),#0000 75%,#000 0)
            0 0/calc(100% - var(--s)) calc(100% - var(--s)),
            conic-gradient(#000 0 0) content-box;
    }

    .max-width {
        max-width: calc(100vw - (var(--bs-gutter-x) * 0.5)) !important;
    }

    .img-map-parent {
        max-width: 100vw;
        white-space: nowrap;
        overflow: hidden;
    }

    .img-overlay {
        width: 434px;
        height: 114px;
        position: absolute;
        background: #0005;
        background: linear-gradient(to right, #0005, var(--bs-body-bg));
        z-index: 1;
    }

    .img-map-side {
        display: inline-block;
        width: 124px;
        height: 114px;
        transform: scaleX(-1);
        background-repeat: no-repeat !important;
        background-size: 150% !important;
    }

    .img-map-left {
        background-position: left -36px !important;
    }

    .img-map-center {
        display: inline-block;
        width: 186px;
        height: 114px;
        background-position: center -36px !important;
        background-size: cover !important;
        background-repeat: no-repeat !important;
    }

    .img-map-right {
        background-position: right -36px !important;
    }
</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";
    Chart.defaults.font.family = "Atkinson Hyperlegible";
    import "chartjs-adapter-luxon";

    import { FactionIcon } from "components/app/FactionIcon";

    import { BarUser } from "model/BarUser";
    import { BarUserMapStats } from "model/BarUserMapStats";
    import { BarUserFactionStats } from "model/BarUserFactionStats";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";
    import { BarUserSkillChanges } from "model/BarUserSkillChanges";
    import { BarUserSkill } from "model/BarUserSkill";

    import { BarUserApi } from "api/BarUserApi";

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";
    import "filters/BarFactionFilter";
    import "filters/BarGamemodeFilter";

    import { FactionUtil } from "util/Faction";
    import { GamemodeUtil } from "util/Gamemode";
    import ColorUtils from "util/Color";
    import LocaleUtil from "util/Locale";
    import { MapUtil } from "util/MapUtil";

    import { GroupedFaction, GroupedFactionGamemode } from "./common";

    export const UserOverview = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            matches: { type: Array as PropType<BarMatch[]>, required: true }
        },

        data: function() {
            return {
                faction: {
                    chart: null as Chart | null,
                },
                gamemodeChart: null as Chart | null,

                skillChanges: {
                    data: Loadable.idle() as Loading<BarUserSkillChanges>,
                    chart: null as Chart | null
                },
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeCharts();
                this.loadSkillChanges();
            });
        },

        methods: {
            loadSkillChanges: async function(): Promise<void> {
                this.skillChanges.data = Loadable.loading();
                this.skillChanges.data = await BarUserApi.getSkillChanges(this.user.userID);

                if (this.skillChanges.data.state == "loaded") {
                    this.makeSkillChart();
                }
            },

            mapBackground: function(map: BarUserMapStats): any {
                return {
                    "background": `url("/image-proxy/MapNameBackground?map=${map.map}&size=texture-thumb")`
                };
            },

            makeCharts: function(): void {
                this.makeFactionChart();
                this.makeGamemodeChart();
            },

            mapNameWithoutVersion: function(name: string): string {
                return MapUtil.getNameNameWithoutVersion(name);
            },

            makeFactionChart: function(): void {
                if (this.faction.chart != null) {
                    this.faction.chart.destroy();
                    this.faction.chart = null;
                }

                const canvas = document.getElementById("most-played-faction") as HTMLCanvasElement | null;
                if (canvas == null) {
                    return console.warn(`cannot make faction chart: #most-played-faction is missing`);
                }

                this.faction.chart = new Chart(canvas.getContext("2d")!, {
                    type: "pie",
                    data: {
                        labels: this.mostPlayedFactions.map(iter => FactionUtil.getName(iter.faction)),
                        datasets: [{
                            data: this.mostPlayedFactions.map(iter => iter.playCount),
                            backgroundColor: this.mostPlayedFactions.map(iter => ColorUtils.getFactionColor(iter.faction))
                        }]
                    },
                    options: {
                        plugins: {
                            legend: { display: false, },
                            tooltip: { enabled: false }
                        },
                        responsive: false,
                        maintainAspectRatio: false,

                        // make it smaller so the icons can go outside the pie with the text
                        radius: "66%",

                        animation: {
                            duration: 0,
                            delay: 0,

                            onComplete: (ev) => {
                                const chart: Chart = ev.chart;
                                const ctx: CanvasRenderingContext2D = chart.ctx;

                                const centerX = chart.width / 2;
                                const centerY = chart.height / 2;

                                ctx.font = `18px "Atkinson Hyperlegible"`;
                                ctx.fillStyle = "#ffffff";
                                ctx.textAlign = "center";

                                const IMAGE_SIZE: number = 36;
                                const IMAGE_RADIUS: number = IMAGE_SIZE / 2;

                                const totalPlays: number = this.mostPlayedFactions.reduce((acc, iter) => acc += iter.playCount, 0);

                                chart.data.datasets.forEach((dataset: ChartDataset, i: number) => {
                                    const meta = chart.getDatasetMeta(i);

                                    meta.data.forEach((bar: Element, index: number) => {
                                        const data = dataset.data[index];
                                        const start: number = (bar as any).startAngle;
                                        const end: number = (bar as any).endAngle;
                                        const radius: number = (bar as any).outerRadius;
                                        const label: string = (chart.data.labels ?? [])[index] as unknown as string;

                                        const center = start + ((end - start) / 2);

                                        const x = Math.cos(center) * (radius * 1.2);
                                        const y = Math.sin(center) * (radius * 1.2);

                                        //console.log(`d=${data} sa=${start} ea=${end} x=${x}/${centerX + x} y=${y}/${centerY + y} r=${radius} name=${label} ca=${center}`);

                                        // debug dot that shows where the calculated (x,y) is
                                        //ctx.fillRect(centerX + x - 5, centerY + y - 5, 10, 10);
                                        const img = document.getElementById(`faction-icon-${label.toLocaleLowerCase()}`) as HTMLImageElement;
                                        ctx.drawImage(img, centerX + x - IMAGE_RADIUS, centerY + y - IMAGE_RADIUS, IMAGE_SIZE, IMAGE_SIZE);

                                        const facData: BarUserFactionStats | undefined = this.mostPlayedFactions
                                            .find(iter => FactionUtil.getName(iter.faction).toLowerCase() == label.toLowerCase());

                                        if (facData != undefined) {
                                            const text: string = `${LocaleUtil.locale(facData.playCount / totalPlays * 100, 0)}%`;
                                            if (y > 0) {
                                                ctx.fillText(text, centerX + x, centerY + y + IMAGE_RADIUS + 12);
                                            } else {
                                                ctx.fillText(text, centerX + x, centerY + y - 20);
                                            }
                                        } else {
                                            console.warn(`UserHeader> faction graph: failed to find faction data for ${label}`);
                                        }
                                    });
                                });

                            }
                        }
                    }
                });
            },

            makeGamemodeChart: function(): void {
                if (this.gamemodeChart != null) {
                    this.gamemodeChart.destroy();
                    this.gamemodeChart = null;
                }

                const canvas = document.getElementById("most-played-gamemode") as HTMLCanvasElement | null;
                if (canvas == null) {
                    return console.warn(`cannot make faction chart: #most-played-gamemode is missing`);
                }

                this.gamemodeChart = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        labels: this.groupedFactionData.map(iter => GamemodeUtil.getName(iter.gamemode)),
                        datasets: [{
                            data: this.groupedFactionData.map(iter => iter.sum.playCount),
                            backgroundColor: "#f80"
                        }]
                    },
                    options: {
                        plugins: {
                            legend: { display: false, },
                            tooltip: { enabled: false }
                        },
                        responsive: false,
                        maintainAspectRatio: false,
                        scales: {
                            y: {
                                grid: {
                                    borderWidth: 0,
                                    lineWidth: 0
                                },
                                display: false
                            },
                            x: {
                                grid: {
                                    borderWidth: 0,
                                    lineWidth: 0
                                },
                                ticks: {
                                    color: "#fff"
                                }
                            }
                        }
                    }
                });
            },
            
            makeSkillChart: function(): void {
                if (this.skillChanges.data.state != "loaded") {
                    return console.warn(`UserInfo> cannot make skill change chart, skillChanges.data is ${this.skillChanges.data.state} not 'loaded'`);
                }

                if (this.skillChanges.chart != null) {
                    this.skillChanges.chart.destroy();
                    this.skillChanges.chart = null;
                }

                const canvas = document.getElementById("user-info-skill-changes") as HTMLCanvasElement | null;
                if (canvas == null) {
                    return console.error(`missing #user-info-skill-changes`);
                }

                const colors: string[] = ColorUtils.randomColors(0.5, this.skillChanges.data.data.gamemodes.length);

                this.skillChanges.chart = new Chart(canvas.getContext("2d")!, {
                    type: "line",
                    data: {
                        datasets: this.skillChanges.data.data.gamemodes.map((iter, index) => {
                            return {
                                label: GamemodeUtil.getName(iter.gamemode),
                                backgroundColor: colors[index],
                                borderColor: colors[index],
                                pointRadius: 1,
                                data: iter.changes.map(iter => {
                                    return {
                                        x: iter.timestamp.getTime(),
                                        y: iter.skill
                                    };
                                })
                            }
                        }),
                    },
                    options: {
                        scales: {
                            x: {
                                type: "time",
                                time: {
                                    unit: "day"
                                },
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
                                display: true,
                                labels: {
                                    color: "#fff",
                                    font: {
                                        family: "Atkinson Hyperlegible"
                                    }
                                }
                            }
                        },
                    },
                });
            },

        },

        computed: {
            skills: function(): BarUserSkill[] {
                return [...this.user.skill].sort((a, b) => a.gamemode - b.gamemode);
            },

            gamemodeStyle: function(): any {
                return {
                    //"background-image": `linear-gradient(to bottom, transparent, var(--bs-body-bg) 50%), url("/img/banner/large_team.jpg")`,
                    "background-repeat": "no-repeat"
                };
            },

            factionStyle: function(): any {
                return {
                    //"background-image": `linear-gradient(to bottom, transparent, var(--bs-body-bg) 50%), url("/img/banner/armada_large.jpg")`,
                    "background-repeat": "no-repeat",
                };
            },

            usernameStyle: function(): any {
                let factionName: string = FactionUtil.getName(this.mostPlayedFaction.faction).toLocaleLowerCase();
                if (factionName == "random") {
                    factionName = "armada";
                }

                return {
                    //"background": `no-repeat url("/img/banner/${factionName}_large.jpg")`,
                    //"height": "120px",
                    //"color": "var(--bs-white)",
                    //"text-shadow": "-2px -2px 0 #000, 2px -2px 0 #000, -2px 2px 0 #000, 2px 2px 0 #000"
                };
            },

            groupedFactionData: function(): GroupedFactionGamemode[] {
                const skill: Map<number, number> = new Map();
                const count: Map<number, number> = new Map();
                const diff: Map<number, number> = new Map();

                for (const match of this.matches) {
                    if (skill.has(match.gamemode) == false) {
                        skill.set(match.gamemode, 0);
                        count.set(match.gamemode, 0);
                    }

                    let s: number = skill.get(match.gamemode) ?? 0;
                    let c: number = count.get(match.gamemode) ?? 0;
                    let d: number = diff.get(match.gamemode) ?? 0;

                    const player: BarMatchPlayer | undefined = match.players.find(iter => iter.userID == this.user.userID);
                    if (player == undefined) {
                        console.warn(`UserInfo> missing BarMatchPlayer from match where a user was a part of [gameID=${match.id}]`);
                        continue;
                    }

                    const enemyPlayers: BarMatchPlayer[] = match.players.filter(iter => iter.allyTeamID != player.allyTeamID);
                    if (enemyPlayers.length == 0) {
                        console.warn(`UserInfo> game is missing any opponents [gameID=${match.id}]`);
                        continue;
                    }

                    const totalSkill: number = enemyPlayers.reduce((acc, iter) => acc += iter.skill, 0);
                    const avgSkill: number = totalSkill / enemyPlayers.length;

                    const playerSkill: number = player?.skill ?? 0;
                    const skillDiff: number = playerSkill - avgSkill;
                    //console.log(`UserInfo> match ${match.id} player skill ${playerSkill} diff ${skillDiff}`);

                    if (Number.isNaN(skillDiff)) {
                        console.warn(`UserInfo> got NaN skill diff [gameID=${match.id}]`);
                        continue;
                    }

                    s += playerSkill;
                    c += 1;
                    d += skillDiff;

                    skill.set(match.gamemode, s);
                    count.set(match.gamemode, c);
                    diff.set(match.gamemode, d);
                }

                for (const iter of diff) {
                    const gamemode: number = iter[0];
                    const s: number = iter[1];
                    const c: number = count.get(gamemode) ?? 1;

                    diff.set(gamemode, s / Math.max(1, c));
                }

                const map: Map<number, GroupedFaction[]> = new Map();

                for (const faction of this.user.factionStats) {
                    if (faction.gamemode == 0) {
                        continue;
                    }

                    const factionData: GroupedFaction[] = (map.get(faction.gamemode) ?? []);
                    factionData.push({
                        faction: faction.faction,
                        playCount: faction.playCount,
                        winCount: faction.winCount,
                    });

                    map.set(faction.gamemode, factionData);
                }

                return Array.from(map.entries()).map(iter => {
                    const sum: GroupedFaction = {
                        faction: 0,
                        playCount: iter[1].reduce((acc, iter) => acc += iter.playCount, 0),
                        winCount: iter[1].reduce((acc, iter) => acc += iter.winCount, 0),
                    }

                    const c: number = count.get(iter[0]) ?? 1;

                    return {
                        gamemode: iter[0],
                        armada: iter[1].find(iter => iter.faction == FactionUtil.ARMADA) ?? null,
                        cortex: iter[1].find(iter => iter.faction == FactionUtil.CORTEX) ?? null,
                        legion: iter[1].find(iter => iter.faction == FactionUtil.LEGION) ?? null,
                        random: iter[1].find(iter => iter.faction == FactionUtil.RANDOM) ?? null,
                        sum: sum,
                        averageSkill: (skill.get(iter[0]) ?? 0) / Math.max(1, c),
                        averageSkillDiff: (diff.get(iter[0]) ?? 0)
                    }
                }).sort((a, b) => {
                    return b.sum.playCount - a.sum.playCount;
                });
            },

            mostPlayedFactions: function(): BarUserFactionStats[] {
                // vetur moment doesn't know es2024 stuff
                const map: Map<number, BarUserFactionStats[]> = Map.groupBy(this.user.factionStats, (elem: BarUserFactionStats) => {
                    return elem.faction;
                });

                // TODO 2025-08-24: this kinda fuckin sucks yo, is there a better way for this?
                return Array.from(map.entries()).sort((a, b) => {
                    const aCount: number = a[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    const bCount: number = b[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    return bCount - aCount;
                }).map(iter => {
                    const fac: BarUserFactionStats = {
                        faction: iter[0],
                        gamemode: 0,
                        playCount: iter[1].reduce((acc, iter) => acc += iter.playCount, 0),
                        lossCount: 0,
                        tieCount: 0,
                        lastUpdated: new Date(),
                        userID: 0,
                        winCount: iter[1].reduce((acc, iter) => acc += iter.winCount, 0),
                    };
                    return fac;
                });
            },

            mostPlayedFaction: function(): BarUserFactionStats {
                return this.mostPlayedFactions[0];
            },

            mostPlayedGamemode: function(): GroupedFactionGamemode {
                return [...this.groupedFactionData].sort((a, b) => {
                    return b.sum.playCount - a.sum.playCount;
                })[0];
            },

            mostPlayedMaps: function(): BarUserMapStats[] {
                const map: Map<string, BarUserMapStats[]> = Map.groupBy(this.user.mapStats, (iter: BarUserMapStats) => iter.map);

                return Array.from(map.entries()).sort((a, b) => {
                    const aCount: number = a[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    const bCount: number = b[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    return bCount - aCount;
                }).slice(0, 3).map(iter => {
                    const map: BarUserMapStats = {
                        map: iter[0],
                        gamemode: 0,
                        lastUpdated: new Date(),
                        lossCount: 0,
                        winCount: iter[1].reduce((acc, iter) => acc += iter.winCount, 0),
                        playCount: iter[1].reduce((acc, iter) => acc += iter.playCount, 0),
                        tieCount: 0,
                        userID: 0
                    };
                    return map;
                });
            }
        },

        components: {
            FactionIcon,
        }

    });
    export default UserOverview;

</script>