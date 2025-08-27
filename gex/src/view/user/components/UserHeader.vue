<template>
    <div>
        <h1 class="text-center border-bottom pb-2 mb-4" :style="usernameStyle">
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
                    <div class="d-lg-none">
                        <div v-for="map in mostPlayedMaps" :key="map.map" class="d-flex mb-3">
                            <img :src="'/image-proxy/MapNameBackground?map=' + map.map + '&size=texture-thumb'" width="80" height="80" class="d-inline rounded me-2"/>

                            <div class="d-inline-flex flex-column align-items-start">
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

                    <div class="d-none d-lg-block">
                        <div :style="mapStyle" class="p-3 border rounded mb-3">
                            <h3 class="text-center">
                                <a :href="'/mapname/' + mostPlayedMaps[0].map" class="text-white" style="text-decoration: none;">
                                    {{ mostPlayedMaps[0].map }}
                                </a>
                            </h3>

                            <h4 class="d-flex">
                                <span class="flex-grow-1">
                                    {{ mostPlayedMaps[0].playCount }} plays
                                </span>

                                <span class="flex-grow-1">
                                    {{ mostPlayedMaps[0].winCount / mostPlayedMaps[0].playCount * 100 | locale(0) }}% won
                                </span>
                            </h4>
                        </div>

                        <div v-for="map in mostPlayedMaps.slice(1)" :key="map.map" class="d-flex mb-3">
                            <img :src="'/image-proxy/MapNameBackground?map=' + map.map + '&size=texture-thumb'" width="80" height="80" class="d-inline rounded me-2"/>

                            <div class="d-inline-flex flex-column align-items-start">
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

            makeCharts: function(): void {
                this.makeFactionChart();
                this.makeGamemodeChart();
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

            mapStyle: function(): any {
                return {
                    "background-image": `url("/image-proxy/MapNameBackground?map=${this.mostPlayedMaps[0].map.replaceAll(" ", "%20")}&size=texture-thumb")`
                }
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