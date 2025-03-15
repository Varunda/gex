
<template>
    <div>
        <div class="d-flex align-items-center">
            <gex-menu class="flex-grow-1"></gex-menu>
        </div>

        <div class="container">
            <div v-if="match.state == 'idle'"> </div>

            <div v-else-if="match.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="match.state == 'loaded'">
                <h1>{{ match.data.map }}</h1>

                <h2>played on {{ match.data.startTime | moment }}</h2>

                <h2>took {{  match.data.durationMs / 1000 | mduration }}</h2>

                <h2>
                    {{ match.data.allyTeams.map(iter => iter.playerCount).join(" v ") }}
                </h2>

                <div id="d3_canvas" style="overflow: hidden;">
                    <svg id="map-svg" :viewBox="viewboxStr"> </svg>
                </div>

                <img id="map-dims" :src="mapUrl" style="display: none;">

                <div v-if="output.state == 'loaded'">
                    <match-opener :openers="computedData.opener" class="my-3"></match-opener>
                    <match-factories :data="computedData.factories" class="my-3"></match-factories>
                </div>

            </div>

            <div v-else-if="match.state == 'error'">
                <api-error :error="match.problem"></api-error>
            </div>

            <div v-else>
                unchecked state of match: {{ match.state }}
            </div>
        </div>
    </div>
    
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading"

    import * as d3 from "d3";
    import * as d3s from "d3-scale";
    import * as d3z from "d3-zoom";

    import "filters/MomentFilter";

    import { GexMenu } from "components/AppMenu";
    import ApiError from "components/ApiError";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";

    import { MatchOpener } from "./components/MatchOpener.vue";
    import MatchFactories from "./components/MatchFactories.vue";

    import { BarMatchApi } from "api/BarMatchApi";
    import { GameOutputApi } from "api/GameOutputApi";

    import { GameOutput } from "model/GameOutput";
    import { BarMatch } from "model/BarMatch";
    import { BarMap } from "model/BarMap";
    import { GameEventUnitDef } from "model/GameEventUnitDef";

    import { CommanderData } from "./compute/ComputeCommanderData";
    import { PlayerOpener } from "./compute/PlayerOpenerData";
    import { FactoryData, PlayerFactories } from "./compute/FactoryData";

    export const Match = Vue.extend({
        props: {

        },

        data: function() {
            return {
                gameID: "" as string,

                match: Loadable.idle() as Loading<BarMatch>,
                output: Loadable.idle() as Loading<GameOutput>,

                loadingSteps: 3 as number,

                svg: null as d3.Selection<any> | null,
                root: null as d3.Selection<any> | null,
                zoom: {} as any,
                //svg: null as SVGElement | null,

                mapW: 0 as number,
                mapH: 0 as number,
                imgW: 0 as number,
                imgH: 0 as number,

                computedData: {
                    commander: [] as CommanderData[],
                    opener: [] as PlayerOpener[],
                    factories: [] as PlayerFactories[]
                }
            };
        },

        created: function(): void {
            this.gameID = location.pathname.split("/")[2];
        },

        beforeMount: function(): void {
            this.loadMatch();
            this.loadOutput();
        },

        methods: {
            loadMatch: async function(): Promise<void> {
                this.match = Loadable.loading();
                this.match = await BarMatchApi.getByID(this.gameID);

                if (this.match.state != "loaded") {
                    return;
                }

                const mapData: BarMap | null = this.match.data.mapData;
                if (mapData == null) {
                    console.warn(`cannot add player start pos: map data is missing!`);
                } else {
                    this.mapW = mapData.width * 512;
                    this.mapH = mapData.height * 512;
                }

                this.decLoadingStepsAndPossiblyStart();

                this.$nextTick(() => {
                    const img: HTMLElement | null = document.getElementById("map-dims");
                    //this.svg = document.getElementById("map-svg") as SVGElement | null;
                    this.svg = d3.select("#map-svg");

                    if (img == null) {
                        console.error(`missing #map-dims!`);
                        return;
                    }

                    img.addEventListener("load", (ev: Event) => {
                        if (this.svg == null) {
                            console.error(`missing #map-svg`);
                            return;
                        }

                        this.imgH = (img as HTMLImageElement).naturalHeight;
                        this.imgW = (img as HTMLImageElement).naturalWidth;
                        console.log(`image is ${this.imgW} x ${this.imgH}`);

                        this.svg.attr("height", this.imgH);
                        this.svg.attr("width", this.imgW);

                        this.root = this.svg.append("g")
                            .attr("id", "doc-root");

                        this.zoom = d3z.zoom()
                            .scaleExtent([1, 10])
                            .translateExtent([[0, 0], [this.imgW, this.imgH]])
                            .on("zoom", (ev: any) => {
                                console.log(ev.transform);
                                this.root!.attr("transform", ev.transform);
                            }
                        );

                        this.svg.call(this.zoom);

                        this.root.append("rect")
                            .attr("x", 0).attr("y", 0)
                            .attr("width", this.imgW).attr("height", this.imgH)
                            .style("fill", "transparent");

                        this.root.append("image")
                            .attr("widht", this.imgW).attr("height", this.imgH)
                            .attr("href", this.mapUrl);

                        //this.svg.setAttribute("height", `${this.imgH}`);
                        //this.svg.setAttribute("width", `${this.imgW}`);

                        this.decLoadingStepsAndPossiblyStart();
                    });
                });
            },

            decLoadingStepsAndPossiblyStart: function(): void {
                --this.loadingSteps;

                if (this.loadingSteps != 0) {
                    console.log(`have ${this.loadingSteps} until done loading all assets!`);
                    return;
                }

                if (this.match.state != "loaded") {
                    throw `expected match to be "loaded', is ${this.match.state} instead!"`;
                }
                if (this.output.state != "loaded") {
                    throw `expected output to be "loaded", is ${this.output.state} instead!`;
                }

                console.log(`add data loaded, starting processing`);
                this.computedData.commander = CommanderData.compute(this.output.data);
                this.computedData.opener = PlayerOpener.compute(this.match.data, this.output.data);
                this.computedData.factories = PlayerFactories.compute(this.match.data, this.output.data);

                this.addCommanderPositionUpdates();
                this.addFactoryPositions();
                this.addPlayerStartingPositions();
            },

            loadOutput: async function(): Promise<void> {
                this.output = Loadable.loading();
                this.output = await GameOutputApi.getEvents(this.gameID);

                if (this.output.state != "loaded") {
                    return;
                }

                this.decLoadingStepsAndPossiblyStart();
            },

            addPlayerStartingPositions: function(): void {
                if (this.svg == null) { return console.warn(`cannot add start pos: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add start pos: root is null`); }
                if (this.match.state != "loaded") { return console.warn(`cannot add start pos: match is not loaded (is ${this.match.state})`); }

                console.log(`adding starting dots for ${this.match.data.players.length} players`);

                for (const player of this.match.data.players) {
                    this.root.append("circle")
                        .attr("cx", `${player.startingPosition.x / this.mapW * 100}%`)
                        .attr("cy", `${player.startingPosition.z / this.mapH * 100}%`)
                        .attr("r", "10px")
                        .style("fill", player.hexColor);
                    /*
                    this.svg.appendChild(
                        this.createCircle(player.startingPosition.x / this.mapW * 100, player.startingPosition.z / this.mapW * 100, 10, player.hexColor, {
                            click: () => {
                                console.log(`clicked on starting position for player ${player.username}`);
                            },
                            mouseenter: () => {
                                console.log(`mouse enter starting position for player ${player.username}`);
                            }
                        }
                    ));
                    */

                    /*
                    this.root.append("text")
                        .attr("x", (this.toImgX(player.startingPosition.x)) + 16)
                        .attr("y", this.toImgZ(player.startingPosition.z))
                        .style("fill", player.hexColor)
                        .style("font-size", "18px")
                        .style("paint-order", "stroke")
                        .style("stroke", "black")
                        .style("stroke-width", "10x")
                        .text(player.username);
                        */

                    const text: SVGTextElement = document.createElementNS("http://www.w3.org/2000/svg", "text");
                    text.setAttributeNS(null, "x", "" + (this.toImgX(player.startingPosition.x) + 16));
                    text.setAttributeNS(null, "y", "" + this.toImgZ(player.startingPosition.z));
                    text.setAttributeNS(null, "style", `fill: ${player.hexColor}; font-size: 18px; paint-order: stroke; stroke: black; stroke-width: 1px;`);
                    text.innerHTML = player.username;

                    //this.svg.appendChild(text);
                }
            },

            addCommanderPositionUpdates: function(): void {
                if (this.svg == null) { return console.warn(`cannot add com pos: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add com pos: root is null`); }
                if (this.output.state != "loaded") { return console.warn(`cannot add com pos: output is not loaded (is ${this.output.state})`); }
                if (this.match.state != "loaded") { return console.warn(`cannot add com pos: match is not loaded (is ${this.match.state})`); }

                console.log(`adding commander paths for ${this.computedData.commander.length} commanders`);

                for (const update of this.computedData.commander) {
                    if (update.positions.length == 0) {
                        continue;
                    }

                    const g: SVGGElement = this.createGroup(`commander-updates-${update.teamID}`);
                    g.classList.add("commander-updates");

                    const color: string = "#" + (this.match.data.players.find(iter => iter.teamID == update.teamID)?.color.toString(16).padStart(6, "0") ?? `ffffff`);
                    let path: string = ``;

                    update.positions.sort((a, b) => { return a.frame - b.frame; });
                    for (const iter of update.positions) {
                        if (path == "") {
                            path += `M ${this.toImgX(iter.x)}, ${this.toImgZ(iter.z)}`;
                        } else {
                            path += `L ${this.toImgX(iter.x)}, ${this.toImgZ(iter.z)}`;
                        }

                        g.appendChild(this.createCircle(iter.x / this.mapW * 100, iter.z / this.mapH * 100, 2, color));
                    }

                    const pathElem: SVGPathElement = document.createElementNS("http://www.w3.org/2000/svg", "path");
                    pathElem.setAttributeNS(null, "d", path);
                    pathElem.setAttributeNS(null, "style", `fill: transparent; stroke: ${color}; stroke-width: 1px`);
                    g.appendChild(pathElem);

                    //this.svg.append("g");

                    //this.svg.appendChild(g);
                }
            },

            addFactoryPositions: function(): void {
                if (this.svg == null) { return console.warn(`cannot add factories: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add factories: root is null`); }
                if (this.output.state != "loaded") { return console.warn(`cannot add factories: output is not loaded (is ${this.output.state})`); }
                if (this.match.state != "loaded") { return console.warn(`cannot add factories: match is not loaded (is ${this.match.state})`); }

                console.log(`adding factories for ${this.computedData.factories.length} players`);

                for (const player of this.computedData.factories) {
                    for (const fac of player.factories) {
                        this.root.append("rect")
                            .attr("x", this.toImgX(fac.position.x))
                            .attr("y", this.toImgZ(fac.position.z))
                            .attr("width", 16).attr("height", 16)
                            .style("fill", player.color);
                        //this.svg.appendChild(this.createSquare(fac.position.x / this.mapW * 100, fac.position.z / this.mapH * 100, 16, player.color));
                    }
                }
            },

            toImgX(x: number): number { return x / this.mapW * this.imgW; },
            toImgZ(z: number): number { return z / this.mapH * this.imgH; },

            createGroup(id: string): SVGGElement {
                const g: SVGGElement = document.createElementNS("http://www.w3.org/2000/svg", "g");
                g.id = id;

                return g;
            },

            createSquare(x: number, y: number, length: number, color: string, events?: {
                click?: () => void,
                mouseenter?: () => void,
                mouseleave?: () => void,
            }): SVGRectElement {

                const rect: SVGRectElement = document.createElementNS("http://www.w3.org/2000/svg", "rect");
                rect.setAttributeNS(null, "x", `${x}%`);
                rect.setAttributeNS(null, "y", `${y}%`);
                rect.setAttributeNS(null, "width", `${length}px`);
                rect.setAttributeNS(null, "height", `${length}px`);
                rect.setAttributeNS(null, "style", `fill: ${color};`);

                if (events?.click) {
                    rect.addEventListener("click", events.click);
                }
                if (events?.mouseenter) {
                    rect.addEventListener("mouseenter", events.mouseenter);
                }
                if (events?.mouseleave) {
                    rect.addEventListener("mouseleave", events.mouseleave);
                }

                return rect;
            },

            /**
             * helper method to make an SVG circle element
             * @param x relative (percent) X position
             * @param y relative (percent) Y position
             * @param radius radius in abolsute units
             * @param color color in hex, include the leading # if needed
             */
            createCircle(x: number, y: number, radius: number, color: string, events?: {
                click?: () => void,
                mouseenter?: () => void,
                mouseleave?: () => void,
            }): SVGCircleElement {

                const dot: SVGCircleElement = document.createElementNS("http://www.w3.org/2000/svg", "circle");
                dot.setAttributeNS(null, "cx", `${x}%`);
                dot.setAttributeNS(null, "cy", `${y}%`);
                dot.setAttributeNS(null, "r", `${radius}px`);
                dot.setAttributeNS(null, "style", `fill: ${color};`);

                if (events?.click) {
                    dot.addEventListener("click", events.click);
                }
                if (events?.mouseenter) {
                    dot.addEventListener("mouseenter", events.mouseenter);
                }
                if (events?.mouseleave) {
                    dot.addEventListener("mouseleave", events.mouseleave);
                }

                return dot;
            }

        },

        computed: {

            mapUrl: function(): string {
                if (this.match.state != "loaded") {
                    return "";
                }
                return `/image-proxy/MapBackground?mapName=${this.match.data.mapName}&size=texture-mq`;
            },

            mapStyleUrl: function(): string {
                return `${location.origin}${this.mapUrl}`
            },

            svgStyle: function(): any {
                return {
                    "background-image": `url(${this.mapUrl})`
                };
            },

            viewboxStr: function(): string {
                return `${0},${0},${this.imgW},${this.imgH}`;
            }
        },

        components: {
            GexMenu, InfoHover, ApiError, ToggleButton,
            MatchOpener, MatchFactories
        }
    });
    export default Match;
</script>