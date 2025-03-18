
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

                <div v-if="match.data.processing">
                    <table class="table table-sm">
                        <tbody>
                            <tr is="ProcessingStep" step="Replay downloaded" :when="match.data.processing.replayDownloaded" :duration="match.data.processing.replayDownloadedMs"></tr>
                            <tr is="ProcessingStep" step="Replay parsed" :when="match.data.processing.replayParsed" :duration="match.data.processing.replayParsedMs"></tr>
                            <tr is="ProcessingStep" step="Replay simulated" :when="match.data.processing.replaySimulated" :duration="match.data.processing.replaySimulatedMs"></tr>
                            <tr is="ProcessingStep" step="Events parsed" :when="match.data.processing.actionsParsed" :duration="match.data.processing.actionsParsedMs"></tr>
                        </tbody>
                    </table>
                </div>

                <h4>
                    <a :href="'/downloadmatch/' + gameID" download="download">
                        Download replay
                    </a>
                </h4>

                <h4>
                    View on <a :href="'https://www.beyondallreason.info/replays?gameId=' + gameID">Beyond All Reason website</a>
                </h4>

                <div class="d-flex justify-content-center mb-2">
                    <toggle-button v-model="map.commanderPositions">
                        Commander positions
                    </toggle-button>

                    <toggle-button v-model="map.factories">
                        Factories
                    </toggle-button>

                    <toggle-button v-model="map.radars">
                        Radars
                    </toggle-button>

                    <toggle-button v-model="map.staticDefense">
                        Static defense
                    </toggle-button>
                </div>
                <div class="d-flex justify-content-center">
                    <div class="flex-grow-0"></div>
                    <div id="d3_canvas" style="overflow: hidden; position: sticky;" class="d-inline-block">
                        <svg id="map-svg" :viewBox="viewboxStr"></svg>
                    </div>
                    <div class="flex-grow-0"></div>
                </div>

                <img id="map-dims" :src="mapUrl" style="display: none;">

                <div v-if="output.state == 'loaded'">
                    <match-opener :openers="computedData.opener" class="my-3"></match-opener>
                    <team-stats-chart :stats="output.data.teamStats" :match="match.data" class="my-3"></team-stats-chart>
                    <!--
                    <match-factories :data="computedData.factories" class="my-3"></match-factories>
                    -->
                    <match-resource-production :match="match.data" :data="computedData.unitResources" class="my-3"></match-resource-production>
                    <match-unit-stats :unit-stats="computedData.unitStats" :match="match.data" class="my-3"></match-unit-stats>
                    <match-wind-graph :updates="output.data.windUpdates" :map="match.data.mapData"></match-wind-graph>
                    <unit-def-view :unit-defs="Array.from(output.data.unitDefinitions.values())" :output="output.data" class="my-3"></unit-def-view>
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

<style scoped>
    text {
        font-family: "Atkinson Hyperlegible";
    }

</style>

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
    import UnitDefView from "./components/UnitDefView.vue";
    import MatchWindGraph from "./components/MatchWindGraph.vue";
    import MatchUnitStats from "./components/MatchUnitStats.vue";
    import TeamStatsChart from "./components/TeamStatsChart.vue";
    import MatchResourceProduction from "./components/MatchResourceProduction.vue";

    import { BarMatchApi } from "api/BarMatchApi";
    import { GameOutputApi } from "api/GameOutputApi";

    import { GameOutput } from "model/GameOutput";
    import { BarMatch } from "model/BarMatch";
    import { BarMap } from "model/BarMap";
    import { GameEventUnitDef } from "model/GameEventUnitDef";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import { CommanderData } from "./compute/ComputeCommanderData";
    import { PlayerOpener } from "./compute/PlayerOpenerData";
    import { FactoryData, PlayerFactories } from "./compute/FactoryData";
    import { UnitStats } from "./compute/UnitStatData";
    import { ResourceProductionData } from "./compute/ResourceProductionData";

    export const ProcessingStep = Vue.extend({
        props: {
            step: { type: String }, 
            when: { },
            duration: { }
        },

        template: `
            <tr>
                <td>{{step}}</td>
                <td>
                    <span v-if="when != null">
                        on {{ when | moment("YYYY-MM-DD hh:mm:ssA") }} (took {{ duration / 1000 | mduration }})
                    </span>
                    <span v-else>
                        --
                    </span>
                </td>
            </tr>
        `
    });

    export const Match = Vue.extend({
        props: {

        },

        data: function() {
            return {
                gameID: "" as string,

                match: Loadable.idle() as Loading<BarMatch>,
                output: Loadable.idle() as Loading<GameOutput>,

                loadingSteps: 3 as number,

                svg: null as d3.Selection<d3.BaseType, unknown, HTMLElement, unknown> | null,
                root: null as d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null,
                tooltip: null as any | null,
                zoom: {} as any,

                mapW: 0 as number,
                mapH: 0 as number,
                imgW: 0 as number,
                imgH: 0 as number,

                unitIdToDefId: new Map as Map<number, number>,

                computedData: {
                    commander: [] as CommanderData[],
                    opener: [] as PlayerOpener[],
                    factories: [] as PlayerFactories[],
                    unitStats: [] as UnitStats[],
                    unitResources: [] as ResourceProductionData[]
                },

                map: {
                    commanderPositions: true as boolean,
                    radars: true as boolean,
                    staticDefense: true as boolean,
                    factories: true as boolean,
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
                                //console.log(ev.transform);
                                this.root!.attr("transform", ev.transform);
                            }
                        );

                        this.svg.call(this.zoom);

                        this.root.append("rect")
                            .attr("x", 0).attr("y", 0)
                            .attr("width", this.imgW).attr("height", this.imgH)
                            .style("fill", "transparent");

                        this.root.append("image")
                            .attr("width", this.imgW).attr("height", this.imgH)
                            .attr("href", this.mapUrl);

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

                for (const ev of this.output.data.unitsCreated) {
                    this.unitIdToDefId.set(ev.unitID, ev.definitionID);
                }

                this.computedData.commander = CommanderData.compute(this.output.data, this.match.data);
                this.computedData.opener = PlayerOpener.compute(this.match.data, this.output.data);
                this.computedData.factories = PlayerFactories.compute(this.match.data, this.output.data);
                this.computedData.unitStats = UnitStats.compute(this.output.data, this.match.data);
                this.computedData.unitResources = ResourceProductionData.compute(this.match.data, this.output.data);

                this.addCommanderPositionUpdates();
                this.addFactoryPositions();
                this.addPlayerStartingPositions();
                this.addRadars();
                this.addStaticDefense();
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

                    this.root.append("text")
                        .attr("x", (this.toImgX(player.startingPosition.x)) + 16)
                        .attr("y", this.toImgZ(player.startingPosition.z))
                        .style("fill", player.hexColor)
                        .style("font-size", "1.3rem")
                        .style("paint-order", "stroke")
                        .style("stroke", "black")
                        .style("stroke-width", "2px")
                        .text(player.username);
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

                    const g = this.root.append("g")
                        .attr("id", `commander-updates-${update.teamID}`)
                        .classed("commander-updates", true)

                    const color: string = "#" + (this.match.data.players.find(iter => iter.teamID == update.teamID)?.color.toString(16).padStart(6, "0") ?? `ffffff`);
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
                            .on("mouseenter", (ev: any) => {
                                this.showTooltip(`${update.name}'s commander was here at ${iter.frame / 30}s`);
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

            addFactoryPositions: function(): void {
                if (this.svg == null) { return console.warn(`cannot add factories: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add factories: root is null`); }
                if (this.output.state != "loaded") { return console.warn(`cannot add factories: output is not loaded (is ${this.output.state})`); }
                if (this.match.state != "loaded") { return console.warn(`cannot add factories: match is not loaded (is ${this.match.state})`); }

                console.log(`adding factories for ${this.computedData.factories.length} players`);

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
                                    console.warn(`Match> failed to find factory ${factoryID}`);
                                    return;
                                }

                                this.showTooltip(`Factory: ${factory.name}<br>`
                                    + `Created at: ${Math.floor(factory.createdAt / 30)}s<br>`
                                    + ((factory.destroyedAt != null) ? `Destroyed at: ${Math.floor(factory.destroyedAt! / 30)}s<br>` : ``)
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
                            .style("stroke-width", "2px")
                            .style("stroke", "black")
                            .style("paint-order", "stroke");

                        facGroup.append("image")
                            .attr("x", this.toImgX(fac.position.x - fac.size.x))
                            .attr("y", this.toImgZ(fac.position.z - fac.size.z))
                            .attr("width", fac.size.x).attr("height", fac.size.z)
                            .attr("href", `/image-proxy/UnitIcon?defName=${fac.factoryDefinitionName}&color=${player.colorInt}`);
                    }
                }
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
                if (pos[0] <= this.imgW / 2) {
                    this.tooltip.style("left", `${pos[0]}px`);
                } else {
                    this.tooltip.style("right", `${this.imgW - pos[0]}px`);
                }

                if (pos[1] <= this.imgH / 2) {
                    this.tooltip.style("top", `${pos[1]}px`);
                } else {
                    this.tooltip.style("bottom", `${this.imgH - pos[1]}px`);
                }
            },

            addRadars: function(): void {
                if (this.svg == null) { return console.warn(`cannot add radars: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add radars: root is null`); }
                if (this.output.state != "loaded") { return console.warn(`cannot add radars: output is not loaded (is ${this.output.state})`); }
                if (this.match.state != "loaded") { return console.warn(`cannot add radars: match is not loaded (is ${this.match.state})`); }

                for (const ev of this.output.data.unitsCreated) {

                    const unitDef: GameEventUnitDef | undefined = this.output.data.unitDefinitions.get(ev.definitionID);
                    if (unitDef == undefined) {
                        console.warn(`Match> missing unit definition [defID=${ev.definitionID}]`);
                        continue;
                    }

                    if (unitDef.unitGroup != "util" || unitDef.speed != 0 || unitDef.weaponCount != 0) {
                        continue;
                    }

                    const player: BarMatchPlayer | undefined = this.match.data.players.find(iter => iter.teamID == ev.teamID);
                    if (player == undefined) {
                        console.warn(`Match> missing player ${ev.teamID} from unit created ${ev.unitID}`);
                        continue;
                    }

                    //const x: number = this.toImgX(ev.unitX - unitDef.sizeX * 2);
                    //const z: number = this.toImgZ(ev.unitZ - unitDef.sizeZ * 2);

                    this.createHoverRange(`radar-range-${ev.unitID}`, ev.unitID, "map-radar", "#00FF0044", ev.unitX - unitDef.sizeX * 2, ev.unitZ - unitDef.sizeZ * 2,
                        unitDef.radarDistance, unitDef.sizeX * 2, unitDef.sizeZ * 2, player.hexColor, player.color, unitDef.definitionName);
                }

            },

            addStaticDefense: function(): void {
                if (this.svg == null) { return console.warn(`cannot add static defense: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add static defense: root is null`); }
                if (this.output.state != "loaded") { return console.warn(`cannot add static defense: output is not loaded (is ${this.output.state})`); }
                if (this.match.state != "loaded") { return console.warn(`cannot add static defense: match is not loaded (is ${this.match.state})`); }

                for (const ev of this.output.data.unitsCreated) {
                    const unitDef: GameEventUnitDef | undefined = this.output.data.unitDefinitions.get(ev.definitionID);
                    if (unitDef == undefined) {
                        console.warn(`Match> missing unit definition [defID=${ev.definitionID}]`);
                        continue;
                    }

                    if (unitDef.unitGroup != "weapon" || unitDef.speed != 0 || unitDef.weaponCount == 0) {
                        continue;
                    }

                    const player: BarMatchPlayer | undefined = this.match.data.players.find(iter => iter.teamID == ev.teamID);
                    if (player == undefined) {
                        console.warn(`Match> missing player ${ev.teamID} from unit created ${ev.unitID}`);
                        continue;
                    }

                    const x: number = this.toImgX(ev.unitX - unitDef.sizeX * 2);
                    const z: number = this.toImgZ(ev.unitZ - unitDef.sizeZ * 2);

                    const g = this.createHoverRange(`static-defense-${ev.unitID}`, ev.unitID, "map-static-defense", "#FF000022", ev.unitX - unitDef.sizeX * 2, ev.unitZ - unitDef.sizeZ * 2,
                        unitDef.attackRange, unitDef.sizeX * 2, unitDef.sizeZ * 2, player.hexColor, player.color, unitDef.definitionName, {
                            mouseenter: (ev: any) => {
                                const unitID: number = Number.parseInt(ev.target.dataset.unitId);
                                console.log(`moused over static defense ${unitID}`);

                                if (this.output.state != "loaded") {
                                    return console.warn(`Match> cannot show static defense ${unitID} tooltip: output.state is not loaded`);
                                }

                                const defID: number | undefined = this.unitIdToDefId.get(unitID);
                                if (defID == undefined) {
                                    return console.warn(`Match> cannot show static defense ${unitID} tooltip: failed to find defID for ${unitID}`);
                                }

                                const unitDef: GameEventUnitDef | undefined = this.output.data.unitDefinitions.get(defID);
                                if (unitDef == undefined) {
                                    return console.warn(`Match> cannot show static defense ${unitID} tooltip: failed to find unit def ${defID}`);
                                }

                                let count: number = 0;
                                for (const ev of this.output.data.unitsKilled) {
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

        watch: {
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
            }

        },

        components: {
            GexMenu, InfoHover, ApiError, ToggleButton,
            MatchOpener, MatchFactories, UnitDefView, MatchWindGraph, MatchUnitStats, TeamStatsChart, MatchResourceProduction,
            ProcessingStep
        }
    });
    export default Match;
</script>