
<template>
    <div>
        <h2 class="wt-header bg-primary">
            Map
        </h2>

        <div class="d-flex justify-content-lg-center mb-2 flex-wrap" style="gap: 0.5rem;">
            <toggle-button v-model="map.startingBox">
                Starting box
            </toggle-button>

            <toggle-button v-model="map.startingPosition">
                Starting positions
            </toggle-button>

            <div :class="{ 'd-none': !hasEvents, 'd-md-block': !hasEvents }" class="d-flex justify-content-lg-center flex-wrap" style="gap: 0.5rem;">
                <toggle-button v-model="map.deathHeatmap" :disabled="!hasEvents">
                    Unit death heatmap
                </toggle-button>

                <!--
                <div class="btn-group border border-light">
                    <button type="button" class="btn" :class="[ map.deathHeatmap ? 'btn-primary' : 'btn-dark' ]" @click="map.deathHeatmap = !map.deathHeatmap" :disabled="!hasEvents">
                        Unit death heatmap
                    </button>
                    <button type="button" class="btn dropdown-toggle dropdown-toggle-split" data-bs-toggle="dropdown" :class="[ map.deathHeatmap ? 'btn-primary' : 'btn-dark' ]" :disabled="!hasEvents">
                        <span class="visually-hidden">toggle dropdown</span>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end">
                        <li v-for="player in match.players" :key="player.teamID">
                            <a class="dropdown-item" :style="{ 'color': player.hexColor, 'user-select': 'none' }" @click.stop>
                                <input class="form-check-input" type="checkbox" :id="'unit-death-' + player.teamID">
                                <label class="form-check-label w-100" :for="'unit-death-' + player.teamID">
                                    {{ player.username }}
                                </label>
                            </a>
                        </li>
                    </ul>
                </div>
                -->

                <toggle-button v-model="map.commanderPositions" :disabled="!hasEvents">
                    Com positions
                </toggle-button>

                <toggle-button v-model="map.commanderHeatmap" :disabled="!hasEvents">
                    Com heatmap
                </toggle-button>

                <toggle-button v-model="map.factories" :disabled="!hasEvents">
                    Factories
                </toggle-button>

                <toggle-button v-model="map.radars" :disabled="!hasEvents">
                    Radars
                </toggle-button>

                <toggle-button v-model="map.staticDefense" :disabled="!hasEvents">
                    Static defense
                </toggle-button>

                <toggle-button v-model="map.buildingHeatmap" :disabled="!hasEvents">
                    Building heatmap
                </toggle-button>

            </div>
        </div>

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

        <img id="map-dims" :src="mapUrl" style="display: none;">

        <div class="mt-2 mx-2">
            <div class="d-flex">
                <button v-if="playback.playing == false" class="btn btn-sm btn-primary" @click="startUnitPositionPlayback" :disabled="!hasEvents">
                    Play
                </button>

                <button v-else class="btn btn-sm btn-primary" @click="pauseUnitPositionPlayback" :disabled="!hasEvents">
                    Pause
                </button>

                <span class="ms-3 flex-grow-1">
                    Viewing units at {{ playback.frame / 30 | mduration }}
                </span>

                <button v-if="hasEvents" class="btn btn-sm" @click="playback.useStrategicIcons = !playback.useStrategicIcons" :class="[ playback.useStrategicIcons ? 'btn-primary' : 'btn-secondary' ]">
                    Use strategic icons
                </button>
            </div>

            <div class="mt-1">
                <input type="range" min="0" :max="unitPositionFrames[unitPositionFrames.length - 1]" step="900" class="form-range" v-model.number="playback.frame" :disabled="!hasEvents">
            </div>

            <span v-if="!hasEvents" class="text-info text-center">
                Gex has not replayed this game, so unit positions are not available
            </span>
        </div>

    </div>
</template>

<style>

    .rotate-90 {
        transform-box: fill-box;
        transform-origin: center;
        transform: rotate(90deg);
    }

    .rotate-180 {
        transform-box: fill-box;
        transform-origin: center;
        transform: rotate(90deg);
    }

    .rotate-270 {
        transform-box: fill-box;
        transform-origin: center;
        transform: rotate(90deg);
    }

    .animate-move {
        transition: all 900ms linear;
    }

    .unit-pos-hide {
        fill-opacity: 0;
        stroke-opacity: 0;
        pointer-events: none;
        display: none;
    }

</style>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import * as d3 from "d3";
    import * as d3s from "d3-scale";
    import * as d3z from "d3-zoom";
    import "d3-contour";
    import "d3-color";

    import "filters/MomentFilter";

    import TimeUtils from "util/Time";
    import ColorUtils, { RGB } from "util/Color";

    import ToggleButton from "components/ToggleButton";
    import InfoHover from "components/InfoHover.vue";

    import { BarMatch } from "model/BarMatch";
    import { GameOutput } from "model/GameOutput";
    import { BarMap } from "model/BarMap";

    import { GameEventUnitDef } from "model/GameEventUnitDef";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import { CommanderData } from "../compute/ComputeCommanderData";
    import { FactoryData, PlayerFactories } from "../compute/FactoryData";
    import { UnitPositionFrame } from "../compute/UnitPositionFrame";

    // 2025-04-17: yeah, this is some good quality code that i love to touch. i can't wait for the next time i want to update the map again!

    let ROOT: d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null = null;
    let SVG: d3.Selection<d3.BaseType, unknown, HTMLElement, unknown> | null = null;

    export const MatchMap = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: { type: Object as PropType<GameOutput>, required: true },
            ScreenWidth: { type: Number, required: true }
        },

        data: function() {
            return {
                
                //svg: null as d3.Selection<d3.BaseType, unknown, HTMLElement, unknown> | null,
                //root: null as d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null,
                tooltip: null as any | null,
                zoom: {} as any,

                transform: { k: 1 as number, x: 0 as number, y: 0 as number },

                mapW: 0 as number,
                mapH: 0 as number,
                imgW: 0 as number,
                imgH: 0 as number,
                isMapImageLoading: false as boolean,

                computedData: {
                    commander: [] as CommanderData[],
                    factories: [] as PlayerFactories[],
                    position: [] as UnitPositionFrame[]
                },

                unitIdToDefId: new Map as Map<number, number>,

                playback: {
                    playing: false as boolean,
                    intervalId: -1 as number,
                    frameIndex: 0 as number,
                    frame: 0 as number,
                    shownUnits: new Set() as Set<number>,
                    useStrategicIcons: false as boolean,
                },

                map: {
                    startingBox: false as boolean,
                    startingPosition: true as boolean,
                    commanderPositions: true as boolean,
                    commanderHeatmap: false as boolean,
                    deathHeatmap: true as boolean,
                    buildingHeatmap: false as boolean,
                    radars: true as boolean,
                    staticDefense: true as boolean,
                    factories: true as boolean,
                }

            }
        },

        mounted: function(): void {
            const mapData: BarMap | null = this.match.mapData;
            if (mapData == null) {
                console.warn(`MatchMap> cannot add player start pos: map data is missing!`);
                return;
            } else {
                this.mapW = mapData.width * 512;
                this.mapH = mapData.height * 512;
            }

            this.isMapImageLoading = true;

            // 1000 by 1000 square
            // width = 1000
            // height = 1000 / (width / height)

            this.$nextTick(() => {
                const img: HTMLElement | null = document.getElementById("map-dims");

                if (img == null) {
                    console.error(`MatchMap> missing #map-dims!`);
                    return;
                }

                SVG = d3.select("#map-svg");

                img.addEventListener("load", (ev: Event) => {
                    this.isMapImageLoading = false;
                    if (this.svg == null) {
                        console.error(`MatchMap> missing #map-svg`);
                        return;
                    }

                    this.imgH = (img as HTMLImageElement).naturalHeight;
                    this.imgW = (img as HTMLImageElement).naturalWidth;
                    const ratio: number = this.imgW / this.imgH;
                    console.log(`MatchMap> image is ${this.imgW} x ${this.imgH}, ratio=${ratio}`);

                    if (this.imgW > this.ScreenWidth) {
                        console.log(`MatchMap> screen is only ${this.ScreenWidth}px, image is ${this.imgW}, capping`);

                        this.imgH = this.imgH / (this.imgW / this.ScreenWidth);
                        this.imgW = this.ScreenWidth;
                        console.log(`MatchMap> image is now ${this.imgW} x ${this.imgH}, ratio=${ratio}`);
                    }

                    this.svg.attr("height", this.imgH);
                    this.svg.attr("width", this.imgW);

                    ROOT = this.svg.append("g")
                        .attr("id", "doc-root");

                    this.zoom = d3z.zoom()
                        .scaleExtent([1, 10])
                        .translateExtent([[0, 0], [this.imgW, this.imgH]])
                        .on("zoom", (ev: any) => {
                            this.root!.attr("transform", ev.transform);
                            this.transform = ev.transform;
                        }
                    );

                    this.svg.call(this.zoom);

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

                    for (const ev of this.output.unitsCreated) {
                        this.unitIdToDefId.set(ev.unitID, ev.definitionID);
                    }

                    this.computedData.commander = CommanderData.compute(this.output, this.match);
                    this.computedData.factories = PlayerFactories.compute(this.match, this.output);
                    this.computedData.position = UnitPositionFrame.compute(this.match, this.output);

                    if (this.mapH == 0 || this.mapW == 0) {
                        throw `MatchMap> bad map dimensions!`;
                    }

                    this.drawMap();
                });
            });
        },

        methods: {
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

            drawMap: function(): void {
                if (this.svg == null) { return console.warn(`cannot draw map: svg is null`); }
                if (this.root == null) { return console.warn(`cannot draw map: root is null`); }

                this.root.selectAll("*:not(.map-no-remove)").remove();

                this.drawInitialUnitPositionFrame();
                this.addStartingBoxes();
                this.addUnitDeathHeatmap();
                this.addCommanderHeatmap();
                this.addCommanderPositionUpdates();
                this.addBuildingHeatmap();
                this.addFactoryPositions();
                this.addPlayerStartingPositions();
                this.addRadars();
                this.addStaticDefense();

                /*
                this.root.append("rect")
                    .classed("map-no-remove", true)
                    .attr("x", 0).attr("y", 0)
                    .attr("width", this.imgW).attr("height", this.imgH)
                    .style("fill", "#0a224244");
                    */
            },

            /**
             * start unit position playback, updating once per second
             */
            startUnitPositionPlayback: function(): void {

                this.map.startingBox = false;
                this.map.startingPosition = false;
                this.map.commanderHeatmap = false;
                this.map.buildingHeatmap = false;
                this.map.commanderPositions = false;
                this.map.deathHeatmap = false;
                this.map.radars = false;
                this.map.staticDefense = false;
                this.map.factories = false;

                this.playback.playing = true;
                this.playback.intervalId = setInterval(() => {
                    this.playback.frame = this.unitPositionFrames[this.playback.frameIndex];
                    // changing the frame also calls this
                    //this.renderUnitPositionFrame(this.playback.frame);

                    ++this.playback.frameIndex;

                    if (this.playback.frameIndex > this.unitPositionFrames.length - 1) {
                        console.log(`MatchMap> playback complete`);
                        this.stopUnitPositionPlayback();
                        return;
                    }
                }, 1000 * 1) as unknown as number;
            },

            /**
             * pause unit position playback
             */
            pauseUnitPositionPlayback: function(): void {
                clearInterval(this.playback.intervalId);
                this.playback.playing = false;
            },

            /**
             * stop unit position playback, resetting it back to 0
             */
            stopUnitPositionPlayback: function(): void {
                clearInterval(this.playback.intervalId);
                this.playback.frameIndex = 0;
                this.playback.frame = 0;
                this.playback.playing = false;
                this.drawInitialUnitPositionFrame();
            },

            /**
             * draw all initial elements used to render the unit position over time map
             */
            drawInitialUnitPositionFrame: function(): void {
                if (this.svg == null) { return console.warn(`cannot render frame: svg is null`); }
                if (this.root == null) { return console.warn(`cannot render frame: root is null`); }

                //const unitPos: UnitPositionFrame[] = this.computedData.position.filter((v, i, a) => a.findIndex(iter => iter.unitID == v.unitID) == i);
                const map: Map<number, UnitPositionFrame> = new Map();
                for (const iter of this.computedData.position) {
                    // only take the first position, so the first time a unit is drawn it doesn't jump around widly
                    if (map.has(iter.unitID) == false) {
                        map.set(iter.unitID, iter);
                    }
                }
                const unitPos: UnitPositionFrame[] = Array.from(map.values());

                if (unitPos.length == 0) {
                    return console.warn(`MatchMap> cannot render initial unit position, no units available!`);
                }

                console.log(`MatchMap> rendering initial location of ${unitPos.length} units`);

                this.root.selectAll(".map-unit-pos").remove();
                this.root.select("#unit-pos-root").remove();

                const g = this.root.append("g")
                    .attr("id", "unit-pos-root");

                for (const pos of unitPos) {
                    const player: BarMatchPlayer | undefined = this.match.players.find(iter => iter.teamID == pos.teamID);

                    const defId: number | undefined = this.unitIdToDefId.get(pos.unitID);

                    if (defId == undefined) {
                        console.warn(`MatchMap> missing unit def ID for unit ${pos.unitID}`);
                        continue;
                    }

                    const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(defId);

                    if (unitDef == undefined) {
                        console.warn(`MatchMap> missing unit def ${defId}`);
                        continue;
                    }

                    // minimum 2 pixel size
                    const sizePx = Math.max(2, Math.max(this.toImgX(unitDef.sizeX), this.toImgZ(unitDef.sizeZ)) * 4);
                    const ux = this.toImgX(pos.x);
                    const uz = this.toImgZ(pos.z);

                    if (this.playback.useStrategicIcons == true) {
                        const unitGroup = this.root.append("g")
                            .attr("id", `map-unit-pos_${pos.unitID}`)
                            .attr("transform", `translate(${ux}, ${uz})`)
                            .attr("width", sizePx).attr("height", sizePx)
                            .classed("map-unit-pos", true)
                            .classed("animate-move", true)
                            .classed("unit-pos-hide", true)
                            .on("mouseenter", (ev: any) => {
                                this.showTooltip(`${unitDef?.name ?? `<missing def ${defId}>`}`);
                            })
                            .on("mousemove", (ev: any) => {
                                this.moveTooltip(ev);
                            })
                            .on("mouseleave", (ev: any) => {
                                this.hideTooltip();
                            });

                        unitGroup.append("image")
                            .attr("width", sizePx).attr("height", sizePx)
                            .style("transform-box", "fill-box").style("transform-origin", "center")
                            .attr("href", `/image-proxy/UnitIcon?defName=${unitDef.definitionName}&color=${player?.color ?? 0}`);

                    } else if (unitDef.speed == 0) {
                        g.append("rect")
                            .attr("id", `map-unit-pos_${pos.unitID}`)
                            .classed("map-unit-pos", true)
                            .classed("animate-move", true)
                            .classed("unit-pos-hide", true)
                            .attr("x", this.toImgX(pos.x)).attr("y", this.toImgZ(pos.z))
                            .attr("width", `${sizePx}px`).attr("height", `${sizePx}px`)
                            .style("fill", player?.hexColor ?? `#333333`)
                            .style("paint-order", "stroke")
                            .style("stroke", "black").style("stroke-width", "2px")
                            .on("mouseenter", (ev: any) => {
                                this.showTooltip(`${unitDef?.name ?? `<missing def ${defId}>`}`);
                            })
                            .on("mousemove", (ev: any) => {
                                this.moveTooltip(ev);
                            })
                            .on("mouseleave", (ev: any) => {
                                this.hideTooltip();
                            });
                    } else {
                        g.append("circle")
                            .attr("id", `map-unit-pos_${pos.unitID}`)
                            .classed("map-unit-pos", true)
                            .classed("animate-move", true)
                            .classed("unit-pos-hide", true)
                            .attr("cx", ux + (sizePx / 2)).attr("cy", uz + (sizePx / 2))
                            .attr("r", `${sizePx / 2}px`)
                            .style("fill", player?.hexColor ?? `#333333`)
                            .style("paint-order", "stroke")
                            .style("stroke", "black").style("stroke-width", "1px")
                            .on("mouseenter", (ev: any) => {
                                this.showTooltip(`${unitDef?.name ?? `<missing def ${defId}>`}`);
                            })
                            .on("mousemove", (ev: any) => {
                                this.moveTooltip(ev);
                            })
                            .on("mouseleave", (ev: any) => {
                                this.hideTooltip();
                            });
                    }
                }
            },

            /**
             * render 1 frame of unit positions, which really just hides stuff dead, and shows stuff alive then moves them to the latest position
             * @param frame frame to render
             */
            renderUnitPositionFrame: function(frame: number): void {
                if (this.svg == null) { return console.warn(`cannot render frame: svg is null`); }
                if (this.root == null) { return console.warn(`cannot render frame: root is null`); }

                console.log(`MatchMap> rendering unit pos frame [frame=${frame}]`);

                //this.root.selectAll(".map-unit-pos").remove();

                const unitPos: UnitPositionFrame[] = this.computedData.position.filter(iter => iter.frame == frame);
                if (unitPos.length == 0) {
                    return console.warn(`MatchMap> cannot render unit position on frame ${frame}, no positions available!`);
                }

                const map: Map<number, UnitPositionFrame> = new Map();
                for (const iter of unitPos) {
                    map.set(iter.unitID, iter);
                }

                for (const unitID of Array.from(this.unitIdToDefId.keys())) {
                    const entry: UnitPositionFrame | undefined = map.get(unitID);
                    const defId: number | undefined = this.unitIdToDefId.get(unitID);

                    if (defId == undefined) {
                        console.warn(`MatchMap> missing unit def ID for unit ${unitID}`);
                        continue;
                    }

                    const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(defId);
                    if (unitDef == undefined) {
                        console.warn(`MatchMap> missing unit def ${defId}`);
                        continue;
                    }

                    const elem = this.root.select(`#map-unit-pos_${unitID}`);

                    // minimum 2 pixel size
                    const sizePx = Math.max(2, Math.max(this.toImgX(unitDef.sizeX), this.toImgZ(unitDef.sizeZ)) * 4);

                    if (entry == undefined) {
                        elem.classed("unit-pos-hide", true);
                    } else {
                        elem.classed("unit-pos-hide", false);
                        //console.log(`MatchMap> showing unit ${unitID} ${elem}`);

                        const type = (elem.node()! as Element).tagName;

                        const ux = this.toImgX(entry.x);
                        const uz = this.toImgZ(entry.z);

                        if (type == "g") {
                            elem.attr("transform", `translate(${ux}, ${uz})`);
                        } else if (type == "rect") {
                            elem.attr("x", ux).attr("y", uz);
                        } else if (type == "circle") {
                            elem.attr("cx", ux + (sizePx / 2)).attr("cy", uz + (sizePx / 2));
                        }
                    }
                }
            },

            /**
             * add a circle and text for each player's starting position
             */
            addPlayerStartingPositions: function(): void {
                if (this.svg == null) { return console.warn(`cannot add start pos: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add start pos: root is null`); }

                console.log(`MatchMap> adding starting dots for ${this.match.players.length} players`);

                for (const player of this.match.players) {
                    this.root.append("circle")
                        .attr("cx", `${player.startingPosition.x / this.mapW * this.imgW}px`)
                        .attr("cy", `${player.startingPosition.z / this.mapH * this.imgH}px`)
                        .attr("r", "10px")
                        .classed("map-starting-position", true)
                        .style("pointer-events", "none")
                        .style("fill", player.hexColor)
                        .style("stroke", "#000000")
                        .style("stroke-width", "1px")
                        .style("paint-order", "fill stroke");

                    const rightSide: boolean = (player.startingPosition.x / this.mapW) > 0.5;
                    const offset: number = rightSide ? -16 : 16;

                    this.root.append("text")
                        .attr("x", (this.toImgX(player.startingPosition.x)) + offset)
                        .attr("y", this.toImgZ(player.startingPosition.z))
                        .classed("map-starting-position", true)
                        .style("fill", player.hexColor)
                        .style("text-anchor", rightSide ? "end" : "start")
                        .style("font-size", "1.3rem")
                        .style("paint-order", "stroke")
                        .style("stroke", "black").style("stroke-width", "2px")
                        .style("pointer-events", "none")
                        .text(player.username);
                }
            },

            /**
             * add starting boxes to the map
             */
            addStartingBoxes: function(): void {
                if (this.svg == null) { return console.warn(`cannot add starting box: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add starting box: root is null`); }

                for (const allyTeam of this.match.allyTeams) {
                    const firstPlayer: BarMatchPlayer | undefined = this.match.players.find(iter => iter.allyTeamID == allyTeam.allyTeamID);
                    console.log(`MatchMap> allyTeam ${allyTeam.allyTeamID} first player name ${firstPlayer?.username} is ${firstPlayer?.hexColor} at ${JSON.stringify(allyTeam.startBox)}`);

                    this.root.append("rect")
                        .attr("x", `${allyTeam.startBox.left * 100}%`)
                        .attr("y", `${allyTeam.startBox.top * 100}%`)
                        .attr("width", `${(allyTeam.startBox.right - allyTeam.startBox.left) * 100}%`)
                        .attr("height", `${(allyTeam.startBox.bottom - allyTeam.startBox.top) * 100}%`)
                        .classed("map-starting-box", true)
                        .style("pointer-events", "none")
                        .style("opacity", this.map.startingBox == true ? "1" : "0")
                        .attr("id", `map-starting-box-${allyTeam.allyTeamID}`)
                        .style("fill", (firstPlayer?.hexColor ?? "#333333") + "33");
                }
            },

            /**
             * add per-team paths that show the commanders position over time
             */
            addCommanderPositionUpdates: function(): void {
                if (this.svg == null) { return console.warn(`cannot add com pos: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add com pos: root is null`); }

                console.log(`MatchMap> adding commander paths for ${this.computedData.commander.length} commanders`);

                for (const update of this.computedData.commander) {
                    if (update.positions.length == 0) {
                        continue;
                    }

                    const g = this.root.append("g")
                        .attr("id", `commander-updates-${update.teamID}`)
                        .style("opacity", this.map.commanderPositions == true ? "1" : "0")
                        .classed("commander-updates", true)

                    const color: string = "#" + (this.match.players.find(iter => iter.teamID == update.teamID)?.color.toString(16).padStart(6, "0") ?? `ffffff`);
                    let path: string = ``;

                    update.positions.sort((a, b) => { return a.frame - b.frame; });
                    for (const iter of update.positions) {
                        if (path == "") {
                            path += `M ${this.toImgX(iter.x)}, ${this.toImgZ(iter.z)}`;
                        } else {
                            path += `L ${this.toImgX(iter.x)}, ${this.toImgZ(iter.z)}`;
                        }

                        g.append("circle")
                            .attr("cx", this.toImgX(iter.x))
                            .attr("cy", this.toImgX(iter.z))
                            .attr("r", `3px`)
                            .style("fill", color)
                            .style("stroke", "#000000")
                            .style("stroke-width", "1px")
                            .style("paint-order", "fill stroke")
                            .on("mouseenter", (ev: any) => {
                                this.showTooltip(`${update.name}'s commander was here at ${TimeUtils.duration(iter.frame / 30)}`);
                            })
                            .on("mousemove", (ev: any) => {
                                this.moveTooltip(ev);
                            })
                            .on("mouseleave", (ev: any) => {
                                this.hideTooltip();
                            });
                    }

                    g.append("path")
                        .attr("d", path)
                        .style("pointer-events", "none")
                        .style("fill", "transparent")
                        .style("stroke", color)
                        .style("stroke-width", "1px");
                }
            },

            /**
             * add per-team heat maps of where units die
             */
            addUnitDeathHeatmap: function(): void {
                if (this.svg == null) { return console.warn(`cannot add death heatmap: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add death heatmap: root is null`); }

                // only include unit deaths from before a team is killed
                // otherwise the heatmap is just the units at the end blowing up lol
                const teamDiedAt: Map<number, number> = new Map(this.output.teamDiedEvents.map(iter => {
                    return [iter.teamID, iter.frame];
                }));

                for (const player of this.match.players) {
                    const deathLocations: [number, number][] = this.output.unitsKilled.filter(iter => iter.teamID == player.teamID).filter(iter => {
                        return iter.frame < (teamDiedAt.get(iter.teamID) ?? Number.MAX_VALUE);
                    }).map(iter => {
                        return [iter.killedX, iter.killedZ];
                    });

                    const heatmap = d3.contourDensity()
                        .x((d) => this.toImgX(d[0]))
                        .y((d) => this.toImgZ(d[1]))
                        .size([this.imgW, this.imgH])
                        .bandwidth(20)(deathLocations);

                    const max: number = Math.max(...heatmap.map(iter => iter.value));

                    const lerp = d3.interpolate(0, 30);

                    this.root.append("g")
                        .attr("id", `map-unit-death-heatmap-${player.teamID}`)
                        .selectAll("path")
                        .data(heatmap)
                        .enter()
                        .append("path")
                            .classed("map-unit-death-heatmap", true)
                            .style("pointer-events", "none")
                            .style("opacity", this.map.deathHeatmap == true ? "1" : "0")
                            .attr("d", d3.geoPath())
                            .attr("fill", (d) => {
                                return `${player.hexColor}${Math.floor(lerp(d.value / max)).toString(16).padStart(2, "0")}`
                            });
                }
            },

            /**
             * add per-team heatmaps of the commanders position
             */
            addCommanderHeatmap: function(): void {
                if (this.svg == null) { return console.warn(`cannot add com heatmap: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add com heatmap: root is null`); }

                for (const player of this.match.players) {
                    const playerCom = this.computedData.commander.find(iter => iter.teamID == player.teamID);
                    if (!playerCom) {
                        continue;
                    }

                    const locs: [number, number][] = playerCom.positions.map(iter => {
                        return [iter.x, iter.z];
                    });

                    const heatmap = d3.contourDensity()
                        .x((d) => this.toImgX(d[0]))
                        .y((d) => this.toImgZ(d[1]))
                        .size([this.imgW, this.imgH])
                        .bandwidth(20)(locs);

                    const max: number = Math.max(...heatmap.map(iter => iter.value));

                    const lerp = d3.interpolate(0, 30);

                    this.root.append("g")
                        .attr("id", `map-commander-position-heatmap-${player.teamID}`)
                        .selectAll("path")
                        .data(heatmap)
                        .enter()
                        .append("path")
                            .classed("map-commander-position-heatmap", true)
                            .style("pointer-events", "none")
                            .style("opacity", this.map.commanderHeatmap == true ? "1" : "0")
                            .attr("d", d3.geoPath())
                            .attr("fill", (d) => {
                                return `${player.hexColor}${Math.floor(lerp(d.value / max)).toString(16).padStart(2, "0")}`
                            });
                }
            },

            /**
             * add per-team heatmaps of where buildings were made
             */
            addBuildingHeatmap: function(): void {
                if (this.svg == null) { return console.warn(`cannot add building heatmap: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add building heatmap: root is null`); }

                const buildingUnitDefIds: Set<number> = new Set();
                for (const unitDef of this.output.unitDefinitions) {
                    if (unitDef[1].speed == 0) {
                        buildingUnitDefIds.add(unitDef[0]);
                    }
                }

                for (const player of this.match.players) {

                    const locs: [number, number][] = this.output.unitsCreated.filter(iter => {
                        return iter.teamID == player.teamID && buildingUnitDefIds.has(iter.definitionID);
                    }).map(iter => {
                        return [iter.unitX, iter.unitZ];
                    });

                    const heatmap = d3.contourDensity()
                        .x((d) => this.toImgX(d[0]))
                        .y((d) => this.toImgZ(d[1]))
                        .size([this.imgW, this.imgH])
                        .bandwidth(30)(locs);

                    const max: number = Math.max(...heatmap.map(iter => iter.value));

                    const lerp = d3.interpolateBasisClosed([5, 10, 30, 40]);

                    this.root.append("g")
                        .attr("id", `map-building-heatmap-${player.teamID}`)
                        .selectAll("path")
                        .data(heatmap)
                        .enter()
                        .append("path")
                            .classed("map-building-heatmap", true)
                            .style("pointer-events", "none")
                            .style("opacity", this.map.buildingHeatmap == true ? "1" : "0")
                            .attr("d", d3.geoPath())
                            .attr("fill", (d) => {
                                return `${player.hexColor}${Math.floor(lerp(d.value / max)).toString(16).padStart(2, "0")}`
                            });
                }
            },

            /**
             * add factory positions and what they created
             */
            addFactoryPositions: function(): void {
                if (this.svg == null) { return console.warn(`cannot add factories: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add factories: root is null`); }

                console.log(`MatchMap> adding factories for ${this.computedData.factories.length} players`);

                for (const player of this.computedData.factories) {
                    for (const fac of player.factories) {
                        const facGroup = this.root.append("g")
                            .attr("x", this.toImgX(fac.position.x - fac.size.x))
                            .attr("y", this.toImgZ(fac.position.z - fac.size.z))
                            .classed("map-factory", true)
                            .attr("width", fac.size.x).attr("height", fac.size.z)
                            .attr("data-id", fac.factoryID)
                            .on("mouseenter", (ev: any) => {
                                const factoryID: number = Number.parseInt(ev.target.dataset.id);

                                let factory: FactoryData | null = null;
                                for (const fac of this.computedData.factories) {
                                    for (const iter of fac.factories) {
                                        if (iter.factoryID == factoryID) {
                                            factory = iter;
                                            break;
                                        }
                                    }
                                    if (factory != null) { break; }
                                }

                                if (factory == null) {
                                    console.warn(`MatchMap> failed to find factory ${factoryID}`);
                                    return;
                                }

                                this.showTooltip(`Factory: ${factory.name}<br>`
                                    + `Created at: ${TimeUtils.duration(factory.createdAt / 30)}<br>`
                                    + ((factory.destroyedAt != null) ? `Destroyed at: ${TimeUtils.duration(factory.destroyedAt! / 30)}<br>` : ``)
                                    + `<table class="table table-sm mb-0"><thead><tr><th colspan="2" class="text-center">Units created</th></tr>`
                                    + `<tbody>${factory.units.map(iter => `<tr><td>${iter.name}</td><td>${iter.count}</td></tr>`).join(" ")}</tbody>`
                                    + `</table>`
                                );
                            }).on("mousemove", (ev: any) => {
                                this.moveTooltip(ev);
                            }).on("mouseleave", (ev: any) => {
                                this.hideTooltip();
                            });

                        facGroup.append("rect")
                            .attr("x", this.toImgX(fac.position.x - fac.size.x))
                            .attr("y", this.toImgZ(fac.position.z - fac.size.z))
                            .attr("width", fac.size.x).attr("height", fac.size.z)
                            .style("fill", player.color)
                            .style("transform-box", "fill-box").style("transform-origin", "center")
                            .style("transform", `rotate(${fac.position.rotation / Math.PI * 180}deg)`)
                            .style("stroke-width", "2px")
                            .style("stroke", "black")
                            .style("paint-order", "stroke");

                        facGroup.append("image")
                            .attr("x", this.toImgX(fac.position.x - fac.size.x))
                            .attr("y", this.toImgZ(fac.position.z - fac.size.z))
                            .attr("width", fac.size.x).attr("height", fac.size.z)
                            .style("transform-box", "fill-box").style("transform-origin", "center")
                            .style("transform", `rotate(${fac.position.rotation / Math.PI * 180}deg)`)
                            .attr("href", `/image-proxy/UnitIcon?defName=${fac.factoryDefinitionName}&color=${player.colorInt}`);
                    }
                }
            },

            /**
             * add radar markers, and range of radar
             */
            addRadars: function(): void {
                if (this.svg == null) { return console.warn(`cannot add radars: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add radars: root is null`); }

                for (const ev of this.output.unitsCreated) {

                    const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(ev.definitionID);
                    if (unitDef == undefined) {
                        console.warn(`MatchMap> missing unit definition [defID=${ev.definitionID}]`);
                        continue;
                    }

                    if (unitDef.unitGroup != "util" || unitDef.speed != 0 || unitDef.weaponCount != 0 || unitDef.radarDistance == 0) {
                        continue;
                    }

                    const player: BarMatchPlayer | undefined = this.match.players.find(iter => iter.teamID == ev.teamID);
                    if (player == undefined) {
                        console.warn(`MatchMap> missing player ${ev.teamID} from unit created ${ev.unitID}`);
                        continue;
                    }

                    //const x: number = this.toImgX(ev.unitX - unitDef.sizeX * 2);
                    //const z: number = this.toImgZ(ev.unitZ - unitDef.sizeZ * 2);

                    this.createHoverRange(`radar-range-${ev.unitID}`, ev.unitID, "map-radar", "#00FF0044", ev.unitX - unitDef.sizeX / 2, ev.unitZ - unitDef.sizeZ / 2,
                        unitDef.radarDistance, unitDef.sizeX, unitDef.sizeZ, player.hexColor, player.color, unitDef.definitionName);
                }

            },

            /**
             * add static defense markers, along with range indicators and how many units killed
             */
            addStaticDefense: function(): void {
                if (this.svg == null) { return console.warn(`cannot add static defense: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add static defense: root is null`); }

                for (const ev of this.output.unitsCreated) {
                    const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(ev.definitionID);
                    if (unitDef == undefined) {
                        console.warn(`MatchMap> missing unit definition [defID=${ev.definitionID}]`);
                        continue;
                    }

                    if (unitDef.unitGroup != "weapon" || unitDef.speed != 0 || unitDef.weaponCount == 0) {
                        continue;
                    }

                    const player: BarMatchPlayer | undefined = this.match.players.find(iter => iter.teamID == ev.teamID);
                    if (player == undefined) {
                        console.warn(`MatchMap> missing player ${ev.teamID} from unit created ${ev.unitID}`);
                        continue;
                    }

                    const x: number = this.toImgX(ev.unitX - unitDef.sizeX * 2);
                    const z: number = this.toImgZ(ev.unitZ - unitDef.sizeZ * 2);

                    const g = this.createHoverRange(`static-defense-${ev.unitID}`, ev.unitID, "map-static-defense", "#FF000022", ev.unitX - unitDef.sizeX / 2, ev.unitZ - unitDef.sizeZ / 2,
                        unitDef.attackRange, unitDef.sizeX, unitDef.sizeZ, player.hexColor, player.color, unitDef.definitionName, {
                            mouseenter: (ev: any) => {
                                const unitID: number = Number.parseInt(ev.target.dataset.unitId);
                                //console.log(`moused over static defense ${unitID}`);

                                const defID: number | undefined = this.unitIdToDefId.get(unitID);
                                if (defID == undefined) {
                                    return console.warn(`MatchMap> cannot show static defense ${unitID} tooltip: failed to find defID for ${unitID}`);
                                }

                                const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(defID);
                                if (unitDef == undefined) {
                                    return console.warn(`MatchMap> cannot show static defense ${unitID} tooltip: failed to find unit def ${defID}`);
                                }

                                let count: number = 0;
                                for (const ev of this.output.unitsKilled) {
                                    if (ev.attackerID == unitID) {
                                        ++count;
                                    }
                                }

                                this.showTooltip(`${unitDef.name}<br>`
                                    + `Killed ${count} units`
                                );
                            },
                            mousemove: (ev: any) => {
                                this.moveTooltip(ev);
                            },
                            mouseleave: (ev: any) => {
                                this.hideTooltip();
                            }
                        }
                    );
                }
            },

            toImgX(x: number): number { return x / this.mapW * this.imgW; },
            toImgZ(z: number): number { return z / this.mapH * this.imgH; },

            /**
             * this is too many arguments
             * @param elemID 
             * @param unitID 
             * @param className 
             * @param fillColor 
             * @param x 
             * @param z 
             * @param r 
             * @param sx 
             * @param sz 
             * @param unitColor 
             * @param playerColor 
             * @param defName 
             * @param callbacks 
             */
            createHoverRange: function(
                elemID: string, unitID: number, className: string,
                fillColor: string, x: number, z: number, r: number, sx: number, sz: number,
                unitColor: string, playerColor: number, defName: string,
                callbacks?: { 
                    mouseenter?: (ev: any) => void,
                    mousemove?: (ev: any) => void,
                    mouseleave?: (ev: any) => void
                },
            ): d3.Selection<SVGGElement, unknown, HTMLElement, unknown> {

                if (this.root == null) { throw `missing root!`; }

                const tx: number = this.toImgX(x);
                const tz: number = this.toImgZ(z);

                this.root.append("circle")
                    .attr("id", elemID)
                    .attr("pointer-events", "none")
                    .attr("cx", tx).attr("cy", tz)
                    .attr("r", Math.max(this.toImgX(r), this.toImgZ(r)))
                    .style("fill", fillColor)
                    .style("stroke", "black")
                    .style("stroke-width", "2px")
                    .style("opacity", 0);

                const facGroup = this.root.append("g")
                    .attr("x", tx).attr("y", tz)
                    .attr("width", sx).attr("height", sz)
                    .attr("data-unit-id", unitID)
                    .classed(className, true)
                    .on("mouseenter", (ev: any) => {
                        this.root!.select("#" + elemID).style("opacity", 1);
                        if (callbacks?.mouseenter) {
                            callbacks.mouseenter(ev);
                        }
                    })
                    .on("mousemove", (ev: any) => {
                        if (callbacks?.mousemove) {
                            callbacks.mousemove(ev);
                        }
                    })
                    .on("mouseleave", (ev: any) => {
                        this.root!.select("#" + elemID).style("opacity", 0);
                        if (callbacks?.mouseleave) {
                            callbacks.mouseleave(ev);
                        }
                    });

                facGroup.append("rect")
                    .attr("x", tx) .attr("y", tz)
                    .attr("width", sx).attr("height", sz)
                    .style("fill", "#" + unitColor)
                    .style("stroke-width", "2px")
                    .style("stroke", "black")
                    .style("paint-order", "stroke");

                facGroup.append("image")
                    .attr("x", tx).attr("y", tz)
                    .attr("width", sx).attr("height", sz)
                    .attr("href", `/image-proxy/UnitIcon?defName=${defName}&color=${playerColor}`);

                return facGroup;
            }

        },

        computed: {
            root: function(): d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null {
                return ROOT;
            },

            svg: function() {
                return SVG;
            },

            mapUrl: function(): string {
                return `/image-proxy/MapBackground?mapName=${this.match.mapName}&size=texture-mq`;
            },

            hasEvents: function(): boolean {
                return this.match.processing != null && this.match.processing.actionsParsed != null;
            },

            viewboxStr: function(): string {
                return `${0},${0},${this.imgW},${this.imgH}`;
            },

            unitPositionFrames: function(): number[] {
                const set: Set<number> = new Set();
                for (const ev of this.output.unitPosition) {
                    set.add(ev.frame);
                }
                return Array.from(set.values()).sort((a, b) => a - b);
            },
            
            loadingRectangleDimensions: function() {
                const mapRatio: number = (this.match.mapData == null) ? 1 : this.match.mapData.width / this.match.mapData.height;

                return {
                    "width": `1000px`,
                    "height": `${1000 / mapRatio}px`
                };
            }

        },

        watch: {

            "playback.frame": function(): void {
                this.renderUnitPositionFrame(this.playback.frame);
            },
            

            "playback.useStrategicIcons": function(): void {
                this.drawInitialUnitPositionFrame();
                this.renderUnitPositionFrame(this.playback.frame);
            },

            "map.commanderPositions": function(): void {
                if (this.map.commanderPositions == false) {
                    this.root?.selectAll(".commander-updates")
                        .style("opacity", "0")
                        .style("pointer-events", "none");
                } else {
                    this.root?.selectAll(".commander-updates")
                        .style("opacity", "1")
                        .style("pointer-events", "auto");
                }
            },

            "map.staticDefense": function(): void {
                if (this.map.staticDefense == false) {
                    this.root?.selectAll(".map-static-defense")
                        .style("opacity", "0")
                        .style("pointer-events", "none");
                } else {
                    this.root?.selectAll(".map-static-defense")
                        .style("opacity", "1")
                        .style("pointer-events", "auto");
                }
            },
            
            "map.radars": function(): void {
                if (this.map.radars == false) {
                    this.root?.selectAll(".map-radar")
                        .style("opacity", "0")
                        .style("pointer-events", "none");
                } else {
                    this.root?.selectAll(".map-radar")
                        .style("opacity", "1")
                        .style("pointer-events", "auto");
                }
            },

            "map.factories": function(): void {
                if (this.map.factories == false) {
                    this.root?.selectAll(".map-factory")
                        .style("opacity", "0")
                        .style("pointer-events", "none");
                } else {
                    this.root?.selectAll(".map-factory")
                        .style("opacity", "1")
                        .style("pointer-events", "auto");
                }
            },

            "map.startingBox": function(): void {
                if (this.map.startingBox == false) {
                    this.root?.selectAll(".map-starting-box")
                        .style("opacity", "0")
                        .style("pointer-events", "none");
                } else {
                    this.root?.selectAll(".map-starting-box")
                        .style("opacity", "1")
                        .style("pointer-events", "auto");
                }
            },
            // .classed("map-unit-death-heatmap")

            "map.deathHeatmap": function(): void {
                if (this.map.deathHeatmap == false) {
                    this.root?.selectAll(".map-unit-death-heatmap")
                        .style("opacity", "0")
                        .style("pointer-events", "none");
                } else {
                    this.root?.selectAll(".map-unit-death-heatmap")
                        .style("opacity", "1")
                        .style("pointer-events", "auto");
                }
            },

            "map.commanderHeatmap": function(): void {
                if (this.map.commanderHeatmap == false) {
                    this.root?.selectAll(".map-commander-position-heatmap")
                        .style("opacity", "0")
                        .style("pointer-events", "none");
                } else {
                    this.root?.selectAll(".map-commander-position-heatmap")
                        .style("opacity", "1")
                        .style("pointer-events", "auto");
                }
            },

            "map.buildingHeatmap": function(): void {
                if (this.map.buildingHeatmap == false) {
                    this.root?.selectAll(".map-building-heatmap")
                        .style("opacity", "0")
                        .style("pointer-events", "none");
                } else {
                    this.root?.selectAll(".map-building-heatmap")
                        .style("opacity", "1")
                        .style("pointer-events", "auto");
                }
            },

            "map.startingPosition": function(): void {
                if (this.map.startingPosition == false) {
                    this.root?.selectAll(".map-starting-position")
                        .style("opacity", "0")
                        .style("pointer-events", "none");
                } else {
                    this.root?.selectAll(".map-starting-position")
                        .style("opacity", "1")
                        .style("pointer-events", "auto");
                }
            }

        },

        components: {
            ToggleButton, InfoHover
        }
    });

    export default MatchMap;
</script>