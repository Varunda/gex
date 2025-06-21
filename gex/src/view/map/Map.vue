<template>
    <div class="container">
        <div class="alert alert-info text-center">
            <span class="bi bi-cone text-warning"></span>
            This page is a heavy work in progress
            <span class="bi bi-cone text-warning"></span>
        </div>

        <div v-if="barMap.state == 'idle'"></div>

        <div v-else-if="barMap.state == 'loading'">
            Loading...
        </div>

        <div v-else-if="barMap.state == 'nocontent'">
            No map with name <code>'{{ mapFilename }}'</code> exists
        </div>

        <div v-else-if="barMap.state == 'loaded'">
            <div class="text-center">
                <h1>
                    {{ barMap.data.name }}
                </h1>

                <h5>
                    by {{ barMap.data.author }}
                </h5>
            </div>

            <img id="map-dims" :src="mapUrl" style="display: none;">

            <div class="d-flex justify-content-center">
                <div class="flex-grow-0 bg-danger">
                </div>
                <div id="d3_canvas" style="position: sticky" class="d-inline-block">
                    <svg id="map-svg" :viewBox="viewboxStr"></svg>
                </div>
                <div class="flex-grow-0 bg-warning">
                </div>
            </div>

            <table class="table table-borderless">
                <tr>
                    <td class="text-end pe-4">Size</td>
                    <td>{{ barMap.data.width }}x{{ barMap.data.height }}</td>
                </tr>

                <tr>
                    <td class="text-end pe-4">Max metal</td>
                    <td>{{ barMap.data.maxMetal }}</td>
                </tr>

                <tr>
                    <td class="text-end pe-4">Tidal</td>
                    <td>{{ barMap.data.tidalStrength }}</td>
                </tr>

                <tr>
                    <td class="text-end pe-4">Wind</td>
                    <td>{{ barMap.data.minimumWind }}-{{ barMap.data.maximumWind }}</td>
                </tr>
            </table>

            <div class="d-flex justify-content-center">
                <div class="flex-grow-0"></div>
                <div>
                    <div class="text-center">
                        Gamemode:
                    </div>
                    <div class="btn-group">
                        <button v-for="gamemode in startSpotGamemodes" :key="gamemode" @click="selectedGamemode = gamemode" class="btn" :class="[ selectedGamemode == gamemode ? 'btn-primary' : 'btn-secondary' ]">
                            {{ gamemode | gamemode }}
                        </button>
                    </div>
                </div>
                <div class="flex-grow-0"></div>
            </div>

            <div v-if="isMapImageLoading == true" :style="loadingRectangleDimensions" class="text-center">
                Loading map image...
            </div>

            <div class="d-flex mb-3">
                <div class="flex-grow-1">

                </div>
            </div>

            <div class="mb-3">
                <div v-if="stats.state == 'loaded'">

                    <h2 class="wt-header">Play stats</h2>

                    <span v-if="stats.data.stats.length == 0">
                        Gex does not have any games played on this map!
                    </span>
                    
                    <template v-else>
                        <div class="d-flex mb-3 text-center" style="justify-content: space-around;">
                            <div class="card">
                                <div class="card-body">
                                    <h5 class="card-title">Total play count</h5>
                                    <p class="card-text">{{ sumMapStats.playCountAllTime | locale(0) }}</p>
                                </div>
                            </div>

                            <div class="card">
                                <div class="card-body">
                                    <h5 class="card-title">Duration</h5>
                                    <p class="card-text">{{ sumMapStats.durationMedianMs / 1000 | mduration }}</p>
                                </div>
                            </div>

                            <div class="card">
                                <div class="card-body">
                                    <h5 class="card-title">Gamemode</h5>
                                    <p class="card-text">{{ sumMapStats.gamemode | gamemode }}</p>
                                </div>
                            </div>
                        </div>

                        <table class="table">
                            <thead>
                                <tr class="table-secondary">
                                    <th>Gamemode</th>
                                    <th>Average duration</th>
                                    <th>Median duration</th>
                                    <th>Play count</th>
                                    <th>Play count (month)</th>
                                    <th>Play count (week)</th>
                                    <th>Play count (day)</th>
                                </tr>
                            </thead>

                            <tbody>
                                <tr v-for="stat in stats.data.stats" :key="stat.gamemode">
                                    <td>{{ stat.gamemode | gamemode }}</td>
                                    <td>{{ stat.durationAverageMs / 1000 | mduration }}</td>
                                    <td>{{ stat.durationMedianMs / 1000 | mduration }}</td>
                                    <td>{{ stat.playCountAllTime | locale(0) }}</td>
                                    <td>{{ stat.playCountMonth | locale(0) }}</td>
                                    <td>{{ stat.playCountWeek | locale(0) }}</td>
                                    <td>{{ stat.playCountDay | locale(0) }}</td>
                                </tr>
                            </tbody>
                        </table>

                        <h2 class="wt-header">Faction stats</h2>

                        <table class="table">
                            <thead>
                                <tr class="table-secondary">
                                    <th>Faction</th>
                                    <th>All time</th>
                                    <th>Month</th>
                                    <th>Week</th>
                                    <th>Day</th>
                                </tr>
                            </thead>

                            <tbody>
                                <tr v-for="faction in factionStats" :key="faction.faction">
                                    <td>
                                        <img v-if="faction.faction == 1" src="/img/armada.png" width="24" height="24">
                                        <img v-else-if="faction.faction == 2" src="/img/cortex.png" width="24" height="24">
                                        <img v-else-if="faction.faction == 3" src="/img/legion.png" width="24" height="24">
                                        <span v-else-if="faction.faction == 4">?</span>
                                        <span v-else>unchecked faction {{ faction.faction }}</span>
                                        {{ faction.faction | faction }}
                                    </td>
                                    <td>
                                        {{ faction.winCountAllTime / Math.max(faction.playCountAllTime, 1) * 100 | locale(2) }}%
                                        ({{ faction.winCountAllTime }} / {{ faction.playCountAllTime }})
                                    </td>
                                    <td>
                                        {{ faction.winCountMonth / Math.max(faction.playCountMonth, 1) * 100 | locale(2) }}%
                                        ({{ faction.winCountMonth }} / {{ faction.playCountMonth }})
                                    </td>
                                    <td>
                                        {{ faction.winCountWeek / Math.max(faction.playCountWeek, 1) * 100 | locale(2) }}%
                                        ({{ faction.winCountWeek }} / {{ faction.playCountWeek }})
                                    </td>
                                    <td>
                                        {{ faction.winCountDay / Math.max(faction.playCountDay, 1) * 100 | locale(2) }}%
                                        ({{ faction.winCountDay }} / {{ faction.playCountDay }})
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                        
                        <h2 class="wt-header">Opening lab stats</h2>

                        <table class="table">
                            <thead>
                                <tr class="table-secondary">
                                    <th>Lab</th>
                                    <th>Play rate</th>
                                    <th>Win count</th>
                                    <th>Play rate (month)</th>
                                    <th>Win rate (month)</th>
                                    <th>Play rate (week)</th>
                                    <th>Win rate (week)</th>
                                    <th>Play rate (day)</th>
                                    <th>Win rate (day)</th>
                                </tr>
                            </thead>

                            <tbody>
                                <tr v-for="lab in openingLabs" :key="lab.defName">
                                    <td>
                                        <img :src="'/image-proxy/UnitIcon?defName=' + lab.defName + '&color=' + factionColor(lab.defName)" height="24" width="24">
                                        {{ lab.defName | defName }}
                                    </td>
                                    <td>{{ lab.countTotal }}</td>
                                    <td>
                                        {{ lab.winTotal }}
                                        ({{ lab.winTotal / Math.max(1, lab.countTotal) * 100 | locale(2) }}%)
                                    </td>
                                    <td>{{ lab.countMonth }}</td>
                                    <td>
                                        {{ lab.winMonth }}
                                        ({{ lab.winMonth / Math.max(1, lab.countMonth) * 100 | locale(2) }}%)
                                    </td>
                                    <td>{{ lab.countWeek }}</td>
                                    <td>
                                        {{ lab.winWeek }}
                                        ({{ lab.winWeek / Math.max(1, lab.countWeek) * 100 | locale(2) }}%)
                                    </td>
                                    <td>{{ lab.countDay }}</td>
                                    <td>
                                        {{ lab.winDay }}
                                        ({{ lab.winDay / Math.max(1, lab.countDay) * 100 | locale(2) }}%)
                                    </td>
                                </tr>
                            </tbody>
                        </table>

                        <span v-if="factionStats.length > 0" class="text-muted text-small">
                            last updated {{ factionStats[0].timestamp | moment }}
                        </span>
                    </template>

                    <span class="text-muted text-small d-block">
                        If a gamemode does not exist, it means Gex has not seen a match played of that gamemode
                    </span>
                </div>

                <div v-else-if="stats.state == 'error'">
                    Failed to load stats
                    <api-error :problem="stats.problem"></api-error>
                </div>
            </div>

            <h2 class="wt-header">Recent games</h2>
            <div v-if="recent.state == 'loaded'">

                <match-list :matches="recent.data"></match-list>
            </div>

        </div>

        <div v-else-if="barMap.state == 'error'">
            <api-error :problem="barMap.problem"></api-error>
        </div>
    </div>

</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import * as d3 from "d3";
    import * as d3s from "d3-scale";
    import * as d3z from "d3-zoom";
    import "d3-contour";
    import "d3-color";

    import ApiError from "components/ApiError";
    import MatchList from "components/app/MatchList.vue";

    import "filters/BarGamemodeFilter";
    import "filters/BarFactionFilter";
    import "filters/MomentFilter";
    import "filters/LocaleFilter";
    import "filters/DefNameFilter";

    import { BarMap } from "model/BarMap";
    import { BarMatch } from "model/BarMatch";
    import { MapStats } from "model/map_stats/MapStats";
    import { MapStatsByGamemode } from "model/map_stats/MapStatsByGamemode";
    import { MapStatsStartSpot } from "model/map_stats/MapStatsStartSpot";
    import { MapStatsByFaction } from "model/map_stats/MapStatsByFaction";
    import { MapStatsOpeningLab } from "model/map_stats/MapStatsOpeningLab";

    import { MapApi } from "api/MapApi";
    import { MapStatsApi } from "api/map_stats/MapStatsApi";
    import { BarMatchApi } from "api/BarMatchApi";

    import LocaleUtil from "util/Locale";
    import ColorUtils, { RGB } from "util/Color";

    let ROOT: d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null = null;
    let SVG: d3.Selection<d3.BaseType, unknown, HTMLElement, unknown> | null = null;

    export const MapView = Vue.extend({
        props: {

        },

        data: function() {
            return {
                mapFilename: "" as string,

                tooltip: null as any | null,
                transform: { k: 1 as number, x: 0 as number, y: 0 as number },

                selectedGamemode: 0 as number,

                mapW: 0 as number,
                mapH: 0 as number,
                imgW: 0 as number,
                imgH: 0 as number,
                isMapImageLoading: false as boolean,

                barMap: Loadable.idle() as Loading<BarMap>,
                stats: Loadable.idle() as Loading<MapStats>,
                recent: Loadable.idle() as Loading<BarMatch[]>,

                palette: [
                    ColorUtils.hexToRgb("#b2182b"), // <44%
                    ColorUtils.hexToRgb("#ef8a62"), // 46%
                    ColorUtils.hexToRgb("#fddbc7"), // 49%
                    ColorUtils.hexToRgb("#f7f7f7"), // 50%
                    ColorUtils.hexToRgb("#d1e5f0"), // 51%
                    ColorUtils.hexToRgb("#67a9cf"), // 54%
                    ColorUtils.hexToRgb("#2166ac"), // >56%
                ] as RGB[]
            }
        },

        mounted: function(): void {
            this.getMapFilenameFromUrl();
            this.bind();
        },

        methods: {

            toImgX(x: number): number { return x / this.mapW * this.imgW; },
            toImgZ(z: number): number { return z / this.mapH * this.imgH; },

            factionColor: function(defName: string): string {
                if (defName.startsWith("arm")) {
                    return "4751067";
                } else if (defName.startsWith("cor")) {
                    return "12139826";
                } else if (defName.startsWith("leg")) {
                    return "9682996";
                }

                return "";
            },

            setupMap: function(): void {
                if (this.barMap.state != "loaded") {
                    console.warn(`Map> cannot setup map, barMap is not loaded`);
                    return;
                }

                const mapData: BarMap | null = this.barMap.data;
                if (mapData == null) {
                    console.warn(`cannot add player start pos: map data is missing!`);
                } else {
                    this.mapW = mapData.width * 512;
                    this.mapH = mapData.height * 512;
                }

                this.isMapImageLoading = true;

                // 1000 by 1000 square
                // width = 1000
                // height = 1000 / (width / height)

                this.$nextTick(() => {
                    const img: HTMLImageElement | null = document.getElementById("map-dims") as HTMLImageElement | null;

                    if (img == null) {
                        console.error(`missing #map-dims!`);
                        return;
                    }

                    SVG = d3.select("#map-svg");

                    if (img.complete) {
                        this.setupImg();
                    } else {
                        img.addEventListener("load", (ev: Event) => {
                            this.setupImg();
                        });
                    }

                });
            },

            setupImg: function(): void {
                const img: HTMLElement | null = document.getElementById("map-dims");

                if (img == null) {
                    console.error(`missing #map-dims!`);
                    return;
                }

                this.isMapImageLoading = false;
                if (this.svg == null) {
                    console.error(`missing #map-svg`);
                    return;
                }

                this.imgH = (img as HTMLImageElement).naturalHeight;
                this.imgW = (img as HTMLImageElement).naturalWidth;
                console.log(`image is ${this.imgW} x ${this.imgH}`);

                this.svg.attr("height", this.imgH);
                this.svg.attr("width", this.imgW);

                ROOT = this.svg.append("g")
                    .attr("id", "doc-root");

                ROOT.append("image")
                    .classed("map-no-remove", true)
                    .attr("id", "map-image")
                    .attr("width", this.imgW).attr("height", this.imgH)
                    .attr("href", this.mapUrl)
                    .style("filter", "saturate(0%)")
                    ;

                /*
                ROOT.append("rect")
                    .classed("map-no-remove", true)
                    .attr("x", 0).attr("y", 0)
                    .attr("width", this.imgW).attr("height", this.imgH)
                    .style("fill", "#0a224244");
                */

                this.tooltip = d3.select("#d3_canvas")
                    .append("div")
                    .style("opacity", 0)
                    .attr("class", "tooltip")
                    .style("pointer-events", "none")
                    .style("position", "absolute")
                    .style("background-color", "var(--bs-body-bg)")
                    .style("border", "solid")
                    .style("border-width", "2px")
                    .style("border-radius", "5px")
                    .style("padding", "5px");

                this.drawMap();
            },

            getMapFilenameFromUrl: function(): void {
                this.mapFilename = location.pathname.split("/")[2];
            },

            bind: async function(): Promise<void> {
                this.barMap = Loadable.loading();
                this.barMap = await MapApi.getByFilename(this.mapFilename);
                if (this.barMap.state == "loaded") {
                    document.title = `Gex / Map / ${this.barMap.data.name}`;
                }

                this.stats = Loadable.loading();
                this.stats = await MapStatsApi.getByMapFilename(this.mapFilename);

                this.setupMap();
                this.bindRecent();
            },

            bindRecent: async function(): Promise<void> {
                if (this.barMap.state != "loaded") {
                    console.warn(`Map> cannot load recent games, map is not loaded ('${this.barMap.state}')`);
                    return;
                }

                this.recent = Loadable.loading();
                this.recent = await BarMatchApi.search(0, 8, {
                    map: this.barMap.data.name
                });
            },

            showTooltip: function(html: string): void {
                if (this.tooltip == null) {
                    console.warn(`tooltip is null`);
                    return;
                }

                this.tooltip.style("opacity", 1);
                this.tooltip.html(html);
            },

            hideTooltip: function(): void {
                this.tooltip?.style("opacity", 0).style("left", 0).style("top", 0);
            },

            moveTooltip: function(ev: any) {
                if (this.tooltip == null) {
                    return;
                }

                this.tooltip.style("left", null).style("right", null).style("top", null).style("bottom", null);

                const pos = d3.pointer(ev, this.root?.node());

                const scale: number = this.transform.k;
                const x: number = this.transform.x;
                const y: number = this.transform.y;

                const left: number = -x / scale;
                const top: number = -y / scale;

                const w: number = this.imgW / scale;
                const h: number = this.imgH / scale;

                // the tooltip isn't part of the svg, so we need to calculate it's position based on the pan/zoom of the svg
                const posx: number = (pos[0] - left) / w * this.imgW;
                const posy: number = (pos[1] - top) / h * this.imgH;

                //console.log(`MatchMap> pos=${pos[0].toFixed(2)},${pos[1].toFixed(2)} => ${posx.toFixed(2)},${posy.toFixed(2)} (${left.toFixed(2)},${top.toFixed(2)}) [${w.toFixed(2)},${h.toFixed(2)}]@${scale.toFixed(2)}`);
                if (pos[0] <= this.imgW / 2) {
                    this.tooltip.style("left", `${posx}px`);
                } else {
                    this.tooltip.style("right", `${this.imgW - posx}px`);
                }

                if (pos[1] <= this.imgH / 2) {
                    this.tooltip.style("top", `${posy}px`);
                } else {
                    this.tooltip.style("bottom", `${this.imgH - posy}px`);
                }
            },

            lerp: function(a: number, b: number, p: number): number {
                return a * (1 - p) + b * p;
            },

            drawMap: function(): void {
                if (this.svg == null) { return console.warn(`cannot draw map: svg is null`); }
                if (this.root == null) { return console.warn(`cannot draw map: root is null`); }

                this.root.selectAll("*:not(.map-no-remove)").remove();

                const cellSize: number = 128;

                if (this.stats.state != "loaded") {
                    console.warn(`Map> cannot drawMap, stats is not loaded`);
                    return;
                }

                if (this.selectedGamemode == 0 && this.startSpotGamemodes.length > 0) {
                    this.selectedGamemode = this.sumMapStats.gamemode;
                }

                const spots: MapStatsStartSpot[] = this.stats.data.startSpots.filter(iter => iter.gamemode == this.selectedGamemode);

                const max: number = Math.max(...spots.map(iter => iter.countTotal));

                for (const spot of spots) {
                    const winRate: number = spot.countWin / Math.max(1, spot.countTotal);

                    const opacity: number = this.lerp(0, 100, spot.countTotal / Math.max(1, max));
                    let color: RGB = this.palette[0];
                    if (winRate <= 0.44) {
                        color =  this.palette[0];
                    } else if (winRate > 0.44 && winRate <= 0.46) {
                        color = this.palette[1];
                    } else if (winRate > 0.46 && winRate <= 0.49) {
                        color = this.palette[2];
                    } else if (winRate > 0.49 && winRate <= 0.51) {
                        color = this.palette[3];
                    } else if (winRate > 0.51 && winRate <= 0.54) {
                        color = this.palette[4];
                    } else if (winRate > 0.54 && winRate <= 0.56) {
                        color = this.palette[5];
                    } else {
                        color = this.palette[6];
                    }

                    const c: string = ColorUtils.rgbaToString(color, Math.max(0.15, Math.pow((opacity / 100), (1 / Math.E))));

                    this.root.append("rect")
                        .attr("id", `start-spot-${spot.startX}-${spot.startZ}`)
                        .attr("x", this.toImgX(spot.startX))
                        .attr("y", this.toImgZ(spot.startZ))
                        .attr("width", `${this.toImgX(cellSize)}px`)
                        .attr("height", `${this.toImgZ(cellSize)}px`)
                        //.style("fill", `rgba(255, 0, 0, ${opacity / 100})`)
                        .style("fill", c)
                        //.style("stroke", "#0000007F")
                        //.style("stroke-width", "1px")
                        .style("paint-order", "fill stroke")
                        .on("mouseenter", (ev: any) => {
                            const id: string = ev.target.id;
                            const parts: string[] = id.split("-");
                            const startX: number = Number.parseInt(parts[2]);
                            const startZ: number = Number.parseInt(parts[3]);

                            if (this.stats.state != "loaded") {
                                return;
                            }

                            const startSpot = this.stats.data.startSpots.find(iter => iter.startX == startX && iter.startZ == startZ && iter.gamemode == this.selectedGamemode);
                            if (startSpot == undefined) {
                                console.log(`Map> missing start spot at (${startX},${startZ})`);
                                return;
                            }

                            this.showTooltip(`${startX},${startZ}<br><table class="table table-sm mb-0">
                                <tr><td>Starts</td><td>${LocaleUtil.locale(startSpot.countTotal, 0)}</td></tr>
                                <tr><td>Wins</td><td>${LocaleUtil.locale(startSpot.countWin, 0)} (${Math.round(startSpot.countWin / startSpot.countTotal * 100)}%)</td></tr></table>`);
                        })
                        .on("mousemove", (ev: any) => {
                            this.moveTooltip(ev);
                        })
                        .on("mouseleave", (ev: any) => {
                            this.hideTooltip();
                        });
                }
            },

        },

        computed: {
            mapUrl: function(): string {
                return `/image-proxy/MapBackground?mapName=${this.mapFilename}&size=texture-lq`;
            },

            mapSaturation: function() {
                return {
                    "filter": "saturation(50%);"
                }
            },

            sumMapStats: function(): MapStatsByGamemode {
                const stats: MapStatsByGamemode = new MapStatsByGamemode();
                if (this.stats.state != "loaded") {
                    return stats;
                }

                let maxIndex: number = 0;
                let maxValue: number = 0;
                for (let i = 0; i < this.stats.data.stats.length; ++i) {
                    const stat: MapStatsByGamemode = this.stats.data.stats[i];

                    if (stat.playCountAllTime > maxValue) {
                        maxIndex = i;
                        maxValue = stat.playCountAllTime;
                    }
                }

                stats.gamemode = this.stats.data.stats[maxIndex].gamemode;
                stats.playCountAllTime = this.stats.data.stats.reduce(((acc, iter) => acc += iter.playCountAllTime), 0);
                stats.playCountMonth = this.stats.data.stats.reduce(((acc, iter) => acc += iter.playCountMonth), 0);
                stats.playCountWeek = this.stats.data.stats.reduce(((acc, iter) => acc += iter.playCountWeek), 0);
                stats.playCountDay = this.stats.data.stats.reduce(((acc, iter) => acc += iter.playCountDay), 0);
                
                stats.durationAverageMs = this.stats.data.stats.reduce((acc, iter) => acc += iter.durationAverageMs, 0) / this.stats.data.stats.length;
                stats.durationMedianMs = this.stats.data.stats.reduce((acc, iter) => acc += iter.durationMedianMs, 0) / this.stats.data.stats.length;

                return stats;
            },

            root: function(): d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null {
                return ROOT;
            },

            svg: function() {
                return SVG;
            },

            viewboxStr: function(): string {
                return `${0},${0},${this.imgW},${this.imgH}`;
            },

            loadingRectangleDimensions: function() {
                if (this.barMap.state != "loaded") {
                    return;
                }

                const mapRatio: number = (this.barMap.data == null) ? 1 : this.barMap.data.width / this.barMap.data.height;

                return {
                    "width": `500px`,
                    "height": `${500 / mapRatio}px`
                };
            },

            startSpotGamemodes: function(): number[] {
                if (this.stats.state != "loaded") {
                    return [];
                }

                return Array.from(new Set(this.stats.data.startSpots.map(iter => iter.gamemode)).values()).sort();
            },

            factionStats: function(): MapStatsByFaction[] {
                if (this.stats.state != "loaded") {
                    return [];
                }

                return this.stats.data.factionStats.filter((iter) => iter.gamemode == this.selectedGamemode);
            },

            openingLabs: function(): MapStatsOpeningLab[] {
                if (this.stats.state != "loaded") {
                    return [];
                }

                return [...this.stats.data.openingLabs.filter((iter) => iter.gamemode == this.selectedGamemode)].sort((a, b) => a.defName.localeCompare(b.defName));
            },

            openingLabSum: function(): number {
                return this.openingLabs.reduce((acc, iter) => acc += iter.countTotal, 0);
            }
        },

        watch: {
            selectedGamemode: function(): void {
                this.drawMap();
            }
        },

        components: {
            ApiError, MatchList
        }
    });
    export default MapView;

</script>