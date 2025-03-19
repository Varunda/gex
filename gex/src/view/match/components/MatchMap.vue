
<template>
    <div>
        <h2 class="wt-header bg-primary">
            Map
        </h2>

        <div class="d-flex justify-content-center mb-2" style="gap: 0.5rem;">
            <toggle-button v-model="map.startingBox">
                Starting box
            </toggle-button>

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

    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import * as d3 from "d3";
    import * as d3s from "d3-scale";
    import * as d3z from "d3-zoom";

    import ToggleButton from "components/ToggleButton";
    import InfoHover from "components/InfoHover.vue";

    import { BarMatch } from "model/BarMatch";
    import { GameOutput } from "model/GameOutput";
    import { BarMap } from "model/BarMap";

    import { GameEventUnitDef } from "model/GameEventUnitDef";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import { CommanderData } from "../compute/ComputeCommanderData";
    import { FactoryData, PlayerFactories } from "../compute/FactoryData";

    export const MatchMap = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: { type: Object as PropType<GameOutput>, required: true }

        },

        data: function() {
            return {
                
                svg: null as d3.Selection<d3.BaseType, unknown, HTMLElement, unknown> | null,
                root: null as d3.Selection<SVGGElement, unknown, HTMLElement, unknown> | null,
                tooltip: null as any | null,
                zoom: {} as any,

                mapW: 0 as number,
                mapH: 0 as number,
                imgW: 0 as number,
                imgH: 0 as number,

                computedData: {
                    commander: [] as CommanderData[],
                    factories: [] as PlayerFactories[],
                },

                unitIdToDefId: new Map as Map<number, number>,

                map: {
                    startingBox: true as boolean,
                    commanderPositions: true as boolean,
                    radars: true as boolean,
                    staticDefense: true as boolean,
                    factories: true as boolean,
                }

            }
        },

        mounted: function(): void {
            const mapData: BarMap | null = this.match.mapData;
            if (mapData == null) {
                console.warn(`cannot add player start pos: map data is missing!`);
            } else {
                this.mapW = mapData.width * 512;
                this.mapH = mapData.height * 512;
            }

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

                    for (const ev of this.output.unitsCreated) {
                        this.unitIdToDefId.set(ev.unitID, ev.definitionID);
                    }

                    this.computedData.commander = CommanderData.compute(this.output, this.match);
                    this.computedData.factories = PlayerFactories.compute(this.match, this.output);

                    this.addStartingBoxes();
                    this.addCommanderPositionUpdates();
                    this.addFactoryPositions();
                    this.addPlayerStartingPositions();
                    this.addRadars();
                    this.addStaticDefense();
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

            addPlayerStartingPositions: function(): void {
                if (this.svg == null) { return console.warn(`cannot add start pos: svg is null`); }
                if (this.root == null) { return console.warn(`cannot add start pos: root is null`); }

                console.log(`MatchMap> adding starting dots for ${this.match.players.length} players`);

                for (const player of this.match.players) {
                    this.root.append("circle")
                        .attr("cx", `${player.startingPosition.x / this.mapW * 100}%`)
                        .attr("cy", `${player.startingPosition.z / this.mapH * 100}%`)
                        .attr("r", "10px")
                        .style("pointer-events", "none")
                        .style("fill", player.hexColor);

                    this.root.append("text")
                        .attr("x", (this.toImgX(player.startingPosition.x)) + 16)
                        .attr("y", this.toImgZ(player.startingPosition.z))
                        .style("fill", player.hexColor)
                        .style("font-size", "1.3rem")
                        .style("paint-order", "stroke")
                        .style("stroke", "black")
                        .style("stroke-width", "2px")
                        .style("pointer-events", "none")
                        .text(player.username);
                }
            },

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
                        .attr("id", `map-starting-box-${allyTeam.allyTeamID}`)
                        .style("fill", (firstPlayer?.hexColor ?? "#333333") + "33");
                }
            },

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
                return `/image-proxy/MapBackground?mapName=${this.match.mapName}&size=texture-mq`;
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
            }

        },

        components: {
            ToggleButton, InfoHover
        }
    });

    export default MatchMap;
</script>