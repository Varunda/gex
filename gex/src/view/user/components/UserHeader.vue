<template>
    <div>
        <h1 class="text-center border-bottom pb-2 mb-4" :style="usernameStyle">
            <img v-if="user.countryCode != null && user.countryCode != '??' && user.countryCode != 'ARM' && user.countryCode != 'COR'"
                :src="'/img/flags/' + user.countryCode.toLowerCase() + '.png'" width="32" height="24"
                :title="'country flag for ' + user.countryCode"
            />

            {{ user.username }}
        </h1>

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
            <a :href="'https://www.bar-stats.pro/playerstats?playerName=' + user.username" target="_blank" ref="nofollow">BarStats link</a>
        </h6>

        <div class="d-none">
            <img id="faction-icon-armada" src="/img/armada.png" height="36" />
            <img id="faction-icon-cortex" src="/img/cortex.png" height="36" />
            <img id="faction-icon-legion" src="/img/legion.png" height="36" />
            <img id="faction-icon-random" src="/img/random.png" height="36" />
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

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";
    import "filters/BarFactionFilter";
    import "filters/BarGamemodeFilter";

    import { FactionUtil } from "util/Faction";
    import { GamemodeUtil } from "util/Gamemode";
    import ColorUtils from "util/Color";
    import LocaleUtil from "util/Locale";
    import { MapUtil } from "util/MapUtil";

    import { GroupedFactionGamemode } from "./common";

    export const UserHeader = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            GroupedFactionData: { type: Array as PropType<GroupedFactionGamemode[]>, required: true }
        },

        data: function() {
            return {
                faction: {
                    chart: null as Chart | null,
                },
                gamemodeChart: null as Chart | null
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeCharts();
            });
        },

        methods: {

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
                        labels: this.GroupedFactionData.map(iter => GamemodeUtil.getName(iter.gamemode)),
                        datasets: [{
                            data: this.GroupedFactionData.map(iter => iter.sum.playCount),
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
        },

        computed: {

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
                return [...this.GroupedFactionData].sort((a, b) => {
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
            FactionIcon
        }
    });
    export default UserHeader;
</script>