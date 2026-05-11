
<template>
    <div>
        <div :id="'match-chat-ping-' + key" style="overflow: hidden; position: sticky;" class="d-inline-block">
            <svg :id="'map-svg-' + key" :viewBox="viewboxStr"></svg>
        </div>

        <div v-if="isMapImageLoading == true" :style="loadingRectangleDimensions" class="text-center">
            Loading map image...
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import * as d3 from "d3";

    import { BarMatch } from "model/BarMatch";
    import { BarMap } from "model/BarMap";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchMapDrawPoint } from "model/BarMatchMapDraws";

    import "filters/MomentFilter";
    import "filters/CompactUnitNameFilter";

    export const MatchOpenerGraphic = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            ping: { type: Object as PropType<BarMatchMapDrawPoint>, required: true }
        },

        data: function() {
            return {
                root: null as d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null,
                svg: null as d3.Selection<d3.BaseType, unknown, HTMLElement, unknown> | null,

                groupBg: null as d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null,
                groupIcon: null as d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null,

                mapW: 0 as number,
                mapH: 0 as number,
                imgW: 0 as number,
                imgH: 0 as number,
                isMapImageLoading: false as boolean,
            }
        },

        mounted: function(): void {
            const mapData: BarMap | null = this.match.mapData;
            if (mapData == null) {
                console.warn(`MatchChatPingMap> cannot add player start pos: map data is missing!`);
                return;
            } else {
                this.mapW = mapData.width * 512;
                this.mapH = mapData.height * 512;
            }

            this.isMapImageLoading = true;

            this.$nextTick(() => {
                const img: HTMLImageElement | null = document.getElementById("map-dims") as HTMLImageElement | null;

                if (img == null) {
                    console.error(`MatchChatPingMap> missing #map-dims!`);
                    return;
                }

                if (img.complete) {
                    this.imgCallback(img);
                } else {
                    img.addEventListener("load", (ev: Event) => {
                        this.imgCallback(img);
                    });
                }
            });

        },

        methods: {

            imgCallback: function(img: HTMLImageElement): void {
                const svgName: string = `#map-svg-${this.key}`;
                this.svg = d3.select(svgName);

                this.isMapImageLoading = false;
                if (this.svg == null) {
                    console.error(`MatchChatPingMap> missing ${svgName}`);
                    return;
                }

                this.imgH = (img as HTMLImageElement).naturalHeight;
                this.imgW = (img as HTMLImageElement).naturalWidth;
                const ratio: number = this.imgW / this.imgH;
                console.log(`MatchChatPingMap> image is ${this.imgW} x ${this.imgH}, ratio=${ratio} [key=${this.key}]`);

                //this.svg.attr("height", this.imgH);
                //this.svg.attr("width", this.imgW);
                this.svg.attr("height", 256);
                this.svg.attr("width", 256);

                this.root = this.svg.append("g")
                    .attr("id", `doc-root-${this.ping.playerID}`);

                this.root.append("image")
                    .classed("map-no-remove", true)
                    .attr("width", this.imgW).attr("height", this.imgH)
                    .attr("href", this.mapUrl);

                this.root.append("rect")
                    .classed("map-no-remove", true)
                    .attr("x", 0).attr("y", 0)
                    .attr("width", this.imgW).attr("height", this.imgH)
                    .style("fill", "#0a224244");

                if (this.mapH == 0 || this.mapW == 0) {
                    throw `MatchChatPingMap> bad map dimensions!`;
                }

                this.drawMap();
            },


            toImgX(x: number): number { return x / this.mapW * this.imgW; },
            toImgZ(z: number): number { return z / this.mapH * this.imgH; },

            drawMap: function(): void {
                if (this.svg == null) { return console.warn(`cannot draw map: svg is null`); }
                if (this.root == null) { return console.warn(`cannot draw map: root is null`); }

                this.root.selectAll("*:not(.map-no-remove)").remove();

                this.root.append("circle")
                    .attr("cx", this.toImgX(this.ping.x)).attr("cy", this.toImgZ(this.ping.z))
                    .attr("r", "50")
                    .style("stroke", "black").style("stroke-width", "2px")
                    .style("fill", this.player?.hexColor ?? "#ffffff");
            },
        },

        computed: {

            key: function(): string {
                return `${this.ping.playerID}-${Math.floor(this.ping.gameTime * 30)}`;
            },

            player: function(): BarMatchPlayer | undefined {
                return this.match.players.find(iter => iter.playerID == this.ping.playerID);
            },

            mapUrl: function(): string {
                return `/image-proxy/MapBackground?mapName=${this.match.mapName}&size=texture-mq`;
            },

            viewboxStr: function(): string {
                return `${0},${0},${this.imgW},${this.imgH}`;
            },

            loadingRectangleDimensions: function() {
                const mapRatio: number = (this.match.mapData == null) ? 1 : this.match.mapData.width / this.match.mapData.height;

                return {
                    "width": `256px`,
                    "height": `${256 / mapRatio}px`
                };
            }

        },

        components: {

        }

    });
    export default MatchOpenerGraphic;
</script>