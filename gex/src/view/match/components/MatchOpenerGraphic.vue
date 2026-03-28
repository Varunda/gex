<template>
    <div :style="{ 'border': '3px solid ' + player.color }" class="rounded">
        <div class="text-center">
            <span :style="{ 'color': player.color }" class="fs-4">
                {{ player.playerName }}
            </span>
        </div>

        <div class="d-flex justify-content-center">
            <div class="flex-grow-0"></div>
            <div :id="'match-opener-' + player.teamID" style="overflow: hidden; position: sticky; background-color: #0a224255" class="d-inline-block">
                <svg :id="'map-svg-' + player.teamID" :viewBox="viewboxStr"></svg>
            </div>
            <div class="flex-grow-0"></div>
        </div>

        <div v-if="isMapImageLoading == true" :style="loadingRectangleDimensions" class="text-center">
            Loading map image...
        </div>

    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import * as d3 from "d3";
    import * as d3s from "d3-scale";
    import * as d3z from "d3-zoom";
    import "d3-contour";
    import "d3-color";

    import { BarMatch } from "model/BarMatch";
    import { BarMap } from "model/BarMap";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { GameOutput } from "model/GameOutput";
    import { GameEventUnitCreated } from "model/GameEventUnitCreated";
    import { GameEventUnitDef } from "model/GameEventUnitDef";

    import { PlayerOpener } from "../compute/PlayerOpenerData";

    import "filters/MomentFilter";
    import "filters/CompactUnitNameFilter";

    export const MatchOpenerGraphic = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: { type: Object as PropType<GameOutput>, required: true },
            player: { type: Object as PropType<PlayerOpener>, required: true }
        },

        data: function() {
            return {
                root: null as d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null,
                svg: null as d3.Selection<d3.BaseType, unknown, HTMLElement, unknown> | null,

                groupBg: null as d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null,
                groupIcon: null as d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null,

                tooltip: null as any | null,
                zoom: {} as any,

                transform: { k: 1 as number, x: 0 as number, y: 0 as number },

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
                console.warn(`MatchOpenerGraphic> cannot add player start pos: map data is missing!`);
                return;
            } else {
                this.mapW = mapData.width * 512;
                this.mapH = mapData.height * 512;
            }

            this.isMapImageLoading = true;

            this.$nextTick(() => {
                const img: HTMLImageElement | null = document.getElementById("map-dims") as HTMLImageElement | null;

                if (img == null) {
                    console.error(`MatchOpenerGraphic> missing #map-dims!`);
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
                const svgName: string = `#map-svg-${this.player.teamID}`;
                this.svg = d3.select(svgName);

                this.isMapImageLoading = false;
                if (this.svg == null) {
                    console.error(`MatchOpenerGraphic> missing ${svgName}`);
                    return;
                }

                this.imgH = (img as HTMLImageElement).naturalHeight;
                this.imgW = (img as HTMLImageElement).naturalWidth;
                const ratio: number = this.imgW / this.imgH;
                console.log(`MatchOpenerGraphic> image is ${this.imgW} x ${this.imgH}, ratio=${ratio}`);

                //this.svg.attr("height", this.imgH);
                //this.svg.attr("width", this.imgW);
                this.svg.attr("height", 256);
                this.svg.attr("width", 256);

                this.root = this.svg.append("g")
                    .attr("id", `doc-root-${this.player.teamID}`);

                this.zoom = d3z.zoom()
                    .scaleExtent([1, 15])
                    .translateExtent([[0, 0], [this.imgW, this.imgH]])
                    .on("zoom", (ev: any) => {
                        this.root!.attr("transform", ev.transform);
                        this.transform = ev.transform;
                    }
                );

                this.svg.call(this.zoom);

                this.root.append("image")
                    .classed("map-no-remove", true)
                    .attr("width", this.imgW).attr("height", this.imgH)
                    .attr("href", this.mapUrl);

                this.root.append("rect")
                    .classed("map-no-remove", true)
                    .attr("x", 0).attr("y", 0)
                    .attr("width", this.imgW).attr("height", this.imgH)
                    .style("fill", "#0a224244");

                // the background and icons are put in different groups so if icons overlap there isn't
                // a background cell around them
                this.groupBg = this.root.append("g")
                    .attr("id", `group-bg-${this.player.teamID}`)
                    .classed("map-no-remove", true);
                this.groupIcon = this.root.append("g")
                    .attr("id", `group-icon-${this.player.teamID}`)
                    .classed("map-no-remove", true);

                this.tooltip = d3.select(`#match-opener-${this.player.teamID}`)
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

                if (this.mapH == 0 || this.mapW == 0) {
                    throw `MatchOpenerGraphic> bad map dimensions!`;
                }

                this.drawMap();
            },


            toImgX(x: number): number { return x / this.mapW * this.imgW; },
            toImgZ(z: number): number { return z / this.mapH * this.imgH; },

            setTransform(k: number, x: number, y: number): void {
                this.svg!.call(this.zoom.scaleTo, k)
                this.svg!.call(this.zoom.translateTo, x, y);

                const t: any = { k: k, x: x, y: y };
                this.transform = t;
            },

            drawMap: function(): void {
                if (this.svg == null) { return console.warn(`cannot draw map: svg is null`); }
                if (this.root == null) { return console.warn(`cannot draw map: root is null`); }

                this.root.selectAll("*:not(.map-no-remove)").remove();

                // move view to where player started
                this.setTransform(8, this.toImgX(this.startSpot.x - 512), this.toImgZ(this.startSpot.z - 512));

                let comDefName: string;
                if (this.player.playerFaction == "Armada") {
                    comDefName = "armcom";
                } else if (this.player.playerFaction == "Cortex") {
                    comDefName = "corcom";
                } else if (this.player.playerFaction == "Legion") {
                    comDefName = "legcom";
                } else {
                    console.log(`MatchOpenerGraphic> unchecked player faction ${this.player.playerFaction}`);
                    comDefName = "armcom";
                }

                this.drawIcon(comDefName, this.startSpot.x, this.startSpot.z, 8, "gold");

                const created: GameEventUnitCreated[] = this.output.unitsCreated.filter(iter => {
                    const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(iter.definitionID);
                    if (unitDef == undefined) {
                        console.error(`MatchOpenerGraphic> missing unit def ${iter.definitionID}`);
                        return false;
                    }

                    return iter.teamID == this.player.teamID && iter.frame < (30 * 60 * 3) && unitDef.speed == 0;
                });

                for (const unit of created) {
                    const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(unit.definitionID);
                    if (unitDef == undefined) {
                        throw `unit def must exist here?`;
                    }

                    this.drawIcon(unitDef.definitionName, unit.unitX, unit.unitZ, Math.max(unitDef.sizeX, unitDef.sizeZ));
                }
            },

            drawIcon: function(defName: string, x: number, z: number, size: number, color?: string): void {
                if (this.svg == null) { return console.warn(`cannot draw map: svg is null`); }
                if (this.root == null) { return console.warn(`cannot draw map: root is null`); }
                if (this.groupBg == null) { return console.warn(`cannot draw icon: groupBg is null`); }
                if (this.groupIcon == null) { return console.warn(`cannot draw icon: groupIcon is null`); }

                this.groupBg.append("rect")
                    .attr("x", this.toImgX(x - size))
                    .attr("y", this.toImgZ(z - size))
                    .attr("width", `${size}px`).attr("height", `${size}px`)
                    .style("fill", this.player.color)
                    .style("stroke", color ?? this.player.color)
                    .style("stroke-width", "1px")

                this.groupIcon.append("image")
                    .attr("x", this.toImgX(x - size))
                    .attr("y", this.toImgZ(z - size))
                    .attr("width", size).attr("height", size)
                    .style("transform-box", "fill-box").style("transform-origin", "center")
                    .attr("href", `/image-proxy/UnitPic?defName=${defName}`)
                    .on("mouseenter", (ev: any) => {
                        this.showTooltip(`${defName}`);
                    })
                    .on("mousemove", (ev: any) => {
                        this.moveTooltip(ev);
                    })
                    .on("mouseleave", (ev: any) => {
                        this.hideTooltip();
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

                //const w: number = this.imgW / scale;
                //const h: number = this.imgH / scale;
                const w: number = 256 / scale;
                const h: number = 256 / scale;

                // the tooltip isn't part of the svg, so we need to calculate it's position based on the pan/zoom of the svg
                //const posx: number = (pos[0] - left) / w * this.imgW;
                //const posy: number = (pos[1] - top) / h * this.imgH;
                const posx: number = (pos[0] - left) / w * 256;
                const posy: number = (pos[1] - top) / h * 256;

                //console.log(`MatchOpenerGraphic> ${this.player.teamID} pos=${pos[0].toFixed(2)},${pos[1].toFixed(2)} => ${posx.toFixed(2)},${posy.toFixed(2)} (${left.toFixed(2)},${top.toFixed(2)}) [${w.toFixed(2)},${h.toFixed(2)}]@${scale.toFixed(2)}`);
                //if (pos[0] <= this.imgW / 2) {
                if (pos[0] <= 128) {
                    this.tooltip.style("left", `${posx}px`);
                    //this.tooltip.style("left", `${-left}px`);
                } else {
                    this.tooltip.style("right", `${256 - posx}px`);
                    //this.tooltip.style("right", `${this.imgW - left}px`);
                }

                //if (pos[1] <= this.imgH / 2) {
                if (pos[1] <= 128) {
                    this.tooltip.style("top", `${posy}px`);
                    //this.tooltip.style("top", `${-top}px`);
                } else {
                    this.tooltip.style("bottom", `${256 - posy}px`);
                    //this.tooltip.style("bottom", `${this.imgH - top}px`);
                }
            },

        },

        computed: {

            matchPlayer: function(): BarMatchPlayer {
                return this.match.players.find(iter => iter.teamID == this.player.teamID)!;
            },

            startSpot: function(): { x: number, y: number, z: number } {
                return this.matchPlayer.startingPosition;
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