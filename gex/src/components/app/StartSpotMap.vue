<template>
    <div>
        <img id="map-dims" :src="mapUrl" style="display: none;">

        <div class="d-flex justify-content-center">
            <div class="flex-grow-0 bg-danger"></div>
            <div id="d3_canvas" style="position: sticky" class="d-inline-block">
                <svg id="map-svg" :viewBox="viewboxStr"></svg>
            </div>
            <div class="flex-grow-0 bg-warning"></div>
        </div>

        <div v-if="isMapImageLoading == true" :style="loadingRectangleDimensions" class="text-center">
            Loading map image...
            <busy class="busy busy-sm"></busy>
        </div>
    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import * as d3 from "d3";
    import * as d3s from "d3-scale";
    import * as d3z from "d3-zoom";
    import "d3-contour";
    import "d3-color";

    import ApiError from "components/ApiError";
    import Busy from "components/Busy.vue";

    import "filters/BarGamemodeFilter";
    import "filters/BarFactionFilter";
    import "filters/MomentFilter";
    import "filters/LocaleFilter";
    import "filters/DefNameFilter";

    import { BarMap } from "model/BarMap";
    import { MapStatsStartSpot } from "model/map_stats/MapStatsStartSpot";

    import LocaleUtil from "util/Locale";
    import ColorUtils, { RGB } from "util/Color";

    let ROOT: d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null = null;
    let SVG: d3.Selection<d3.BaseType, unknown, HTMLElement, unknown> | null = null;

    export const StartSpotMap = Vue.extend({
        props: {
            MapData: { type: Object as PropType<BarMap>, required: true },
            StartSpots: { type: Array as PropType<MapStatsStartSpot[]>, required: true }
        },

        data: function() {
            return {
                tooltip: null as any | null,
                transform: { k: 1 as number, x: 0 as number, y: 0 as number },

                mapW: 0 as number,
                mapH: 0 as number,
                imgW: 0 as number,
                imgH: 0 as number,
                isMapImageLoading: false as boolean,

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
                this.mapW = this.MapData.width * 512;
                this.mapH = this.MapData.height * 512;

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
                console.log(`StartSpotMap> image is ${this.imgW} x ${this.imgH}`);

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

            bind: async function(): Promise<void> {
                this.setupMap();
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

                const max: number = Math.max(...this.StartSpots.map(iter => iter.countTotal));

                for (const spot of this.StartSpots) {
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

                            const startSpot = this.StartSpots.find(iter => iter.startX == startX && iter.startZ == startZ);
                            if (startSpot == undefined) {
                                console.log(`StartSpotMap> missing start spot at (${startX},${startZ})`);
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
            mapFilename: function(): string {
                if (this.StartSpots.length == 0) {
                    return "";
                }

                return this.StartSpots[0].mapFilename;
            },

            mapUrl: function(): string {
                return `/image-proxy/MapBackground?mapName=${this.mapFilename}&size=texture-lq`;
            },

            mapSaturation: function() {
                return {
                    "filter": "saturation(50%);"
                }
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
                const mapRatio: number = this.MapData.width / this.MapData.height;

                return {
                    "width": `500px`,
                    "height": `${500 / mapRatio}px`
                };
            },
        },

        watch: {

        },

        components: {
            ApiError, Busy
        }
    });
    export default StartSpotMap;
</script>