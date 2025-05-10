<template>
    <div class="container">
        <div v-if="barMap.state == 'idle'"></div>

        <div v-else-if="barMap.state == 'loading'">
            Loading...
        </div>

        <div v-else-if="barMap.state == 'nocontent'">
            No map with name <code>{{ mapFilename }}</code> exists
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
                <div class="flex-grow-0"></div>
                <div id="d3_canvas" style="overflow: hidden; position: sticky; background-color: #0a224255" class="d-inline-block">
                    <svg id="map-svg" :viewBox="viewboxStr"></svg>
                </div>
                <div class="flex-grow-0"></div>
            </div>

            <div v-if="isMapImageLoading == true" :style="loadingRectangleDimensions" class="text-center">
                Loading map image...
            </div>

            <div class="d-flex mb-3">
                <div class="flex-grow-1">
                    <table class="table table-borderless">
                        <tr>
                            <td>Size</td>
                            <td>{{ barMap.data.width }}x{{ barMap.data.height }}</td>
                        </tr>

                        <tr>
                            <td>Max metal</td>
                            <td>{{ barMap.data.maxMetal }}</td>
                        </tr>

                        <tr>
                            <td>Tidal</td>
                            <td>{{ barMap.data.tidalStrength }}</td>
                        </tr>

                        <tr>
                            <td>Wind</td>
                            <td>{{ barMap.data.minimumWind }}-{{ barMap.data.maximumWind }}</td>
                        </tr>
                    </table>

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
                    </template>

                    <span class="text-muted text-small">
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
    import "filters/MomentFilter";
    import "filters/LocaleFilter";

    import { BarMap } from "model/BarMap";
    import { MapStatsByGamemode } from "model/map_stats/MapStatsByGamemode";
    import { MapStats } from "model/map_stats/MapStats";
    import { BarMatch } from "model/BarMatch";

    import { MapApi } from "api/MapApi";
    import { MapStatsApi } from "api/map_stats/MapStatsApi";
    import { BarMatchApi } from "api/BarMatchApi";

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

                mapW: 0 as number,
                mapH: 0 as number,
                imgW: 0 as number,
                imgH: 0 as number,
                isMapImageLoading: false as boolean,

                barMap: Loadable.idle() as Loading<BarMap>,
                stats: Loadable.idle() as Loading<MapStats>,
                recent: Loadable.idle() as Loading<BarMatch[]>
            }
        },

        mounted: function(): void {
            this.getMapFilenameFromUrl();
            this.bind();
        },

        methods: {

            toImgX(x: number): number { return x / this.mapW * this.imgW; },
            toImgZ(z: number): number { return z / this.mapH * this.imgH; },

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
                    .attr("width", this.imgW).attr("height", this.imgH)
                    .attr("href", this.mapUrl);

                ROOT.append("rect")
                    .classed("map-no-remove", true)
                    .attr("x", 0).attr("y", 0)
                    .attr("width", this.imgW).attr("height", this.imgH)
                    .style("fill", "#0a224244");

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

                const cellSize: number = 64;

                if (this.stats.state != "loaded") {
                    console.warn(`Map> cannot drawMap, stats is not loaded`);
                    return;
                }

                const total: number = this.stats.data.startSpots.reduce((acc, iter) => acc += iter.countTotal, 0);
                const max: number = Math.max(...this.stats.data.startSpots.map(iter => iter.countTotal));

                console.log(`drawing start spots`);
                for (const spot of this.stats.data.startSpots) {

                    const opacity: number = this.lerp(15, 100, spot.countTotal / max);
                    console.log(opacity.toString());

                    this.svg.append("rect")
                        .attr("id", `start-spot-${spot.startX}-${spot.startZ}`)
                        .attr("x", this.toImgX(spot.startX))
                        .attr("y", this.toImgZ(spot.startZ))
                        .attr("width", `${this.toImgX(cellSize)}px`)
                        .attr("height", `${this.toImgZ(cellSize)}px`)
                        .style("fill", `rgba(255, 0, 0, ${opacity / 100})`)
                        .on("mouseenter", (ev: any) => {
                            const id: string = ev.target.id;
                            const parts: string[] = id.split("-");
                            const startX: number = Number.parseInt(parts[2]);
                            const startZ: number = Number.parseInt(parts[3]);

                            if (this.stats.state != "loaded") {
                                return;
                            }

                            const startSpot = this.stats.data.startSpots.find(iter => iter.startX == startX && iter.startZ == startZ);
                            if (startSpot == undefined) {
                                console.log(`Map> missing start spot at (${startX},${startZ})`);
                                return;
                            }

                            this.showTooltip(`Starts: ${startSpot.countTotal}<br>Wins: ${startSpot.countWin} (${Math.round(startSpot.countWin / startSpot.countTotal * 100)}%)`);
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
            }

        },

        components: {
            ApiError, MatchList
        }
    });
    export default MapView;

</script>