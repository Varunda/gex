<template>
    <div>
        <div style="height: 400px">
            <canvas id="match-unit-value-graph" height="400"></canvas>
        </div>

        <select v-model="selectedUnitDef" class="form-control">
            <option :value="null">none</option>
            <option v-for="unitDef of unitDefs" :key="unitDef.definitionName" :value="unitDef.definitionName">
                {{ unitDef.name }}
            </option>
        </select>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarMatch } from "model/BarMatch";
    import { GameOutput } from "model/GameOutput";

    import Chart, { ChartDataset, LegendItem, Plugin } from "chart.js/auto/auto.esm";

    import { GameEventUnitCreated } from "model/GameEventUnitCreated";
    import { GameEventUnitDef } from "model/GameEventUnitDef";

    import { StatEntity } from "../compute/common";

    import TimeUtils from "util/Time";
    import TableUtils from "util/Table";
    import CompactUtils from "util/Compact";
import ColorUtils, { RGB } from "util/Color";

    type UnitValueEntry = {
        defName: string;
        alive: number;
        metalCost: number;
        energyCost: number;
    };

    type UnitValueInterval = {
        entityID: string;
        frame: number;
        entries: UnitValueEntry[];
    };

    type UnitEntry = {
        unitID: number;
        teamID: number;
        defName: string;
        definition: GameEventUnitDef;
        createdFrame: number;
        destroyedFrame: number;
    };

    export const MatchUnitValueGraph = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: { type: Object as PropType<GameOutput>, required: true },
            SelectedEntity: { type: String, required: true },
            entities: {type: Array as PropType<StatEntity[]>, required: true },
            ShowMobile: { type: Boolean, required: true }
        },

        data: function() {
            return {
                intervals: [] as UnitValueInterval[],

                selectedUnitDef: null as string | null,

                intervalSize: 900 as number, // how many frames in between intervals

                chart: null as Chart | null,
            }
        },

        created: function(): void {
            this.makeData();
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeChart();
            });
        },

        methods: {
            makeData: function(): void {
                this.intervals = [];

                const lastFrame: number = Math.max(
                    ...this.output.unitsCreated.map(iter => iter.frame),
                    ...this.output.unitsKilled.map(iter => iter.frame)
                );

                const unitDefByName: Map<string, GameEventUnitDef> = new Map();
                for (const unitDef of this.output.unitDefinitions) {
                    unitDefByName.set(unitDef[1].definitionName, unitDef[1]);
                }

                console.time(`match-unit-value-graph: all`);
                console.time(`match-unit-value-graph: make units created`);
                const unitsMap: Map<number, UnitEntry> = new Map();
                for (const unitCreated of this.output.unitsCreated) {
                    if (unitsMap.has(unitCreated.unitID) == true) {
                        console.warn(`MatchUnitValueGraph> colliding unit ID found [unitID=${unitCreated.unitID}]`);
                        continue;
                    }

                    const def: GameEventUnitDef | undefined = this.output.unitDefinitions.get(unitCreated.definitionID);
                    if (def == undefined) {
                        console.warn(`MatchUnitValueGraph> missing definition for defID [definitionID=${unitCreated.definitionID}] [unitID=${unitCreated.unitID}]`);
                        continue;
                    }

                    unitsMap.set(unitCreated.unitID, {
                        unitID: unitCreated.unitID,
                        teamID: unitCreated.teamID,
                        definition: def,
                        defName: def.definitionName,
                        createdFrame: unitCreated.frame,
                        destroyedFrame: -1
                    });
                }
                console.timeEnd(`match-unit-value-graph: make units created`);

                console.time(`match-unit-value-graph: make units destroyed`);
                for (const unitKilled of this.output.unitsKilled) {
                    const unit: UnitEntry | undefined = unitsMap.get(unitKilled.unitID);
                    if (unit == undefined) {
                        console.warn(`MatchUnitValueGraph> unit created event did not happen? [unitID=${unitKilled.unitID}]`);
                        continue;
                    }

                    unit.destroyedFrame = unitKilled.frame;
                }

                for (const key of Array.from(unitsMap.keys())) {
                    const unit: UnitEntry | undefined = unitsMap.get(key);
                    if (unit == undefined) {
                        throw `why does the map have a key that returned undefined HUH`;
                    }

                    if (unit.destroyedFrame == -1) {
                        unit.destroyedFrame = lastFrame;
                    }
                }
                console.timeEnd(`match-unit-value-graph: make units destroyed`);

                const units: UnitEntry[] = Array.from(unitsMap.values());

                for (const entity of this.entities) {
                    if (entity.id == "newline") {
                        continue;
                    }

                    console.time(`match-unit-value-graph: entity ${entity.id}`);
                    for (let i = 0; i <= lastFrame; i += this.intervalSize) {
                        const intervalLastFrame: number = i + this.intervalSize;

                        const interestedTeamIds: Set<number> = this.getInterestedTeams(entity.id);

                        const entities: UnitEntry[] = units.filter(iter => {
                            return interestedTeamIds.has(iter.teamID)
                                && i >= iter.createdFrame
                                && intervalLastFrame <= iter.destroyedFrame;
                        });

                        const defNames: Set<string> = new Set([ ...entities.map(iter => iter.defName) ]);

                        this.intervals.push({
                            entityID: entity.id,
                            frame: i,
                            entries: Array.from(defNames) //.filter(iter => iter != "cor_hat_viking")
                                .map(iter => {
                                    const count: number = entities.filter(e => e.defName == iter).length;
                                    const def: GameEventUnitDef | undefined = unitDefByName.get(iter);
                                    
                                    return {
                                        defName: iter,
                                        alive: count,
                                        energyCost: (def?.energyCost ?? 0) * count,
                                        metalCost: (def?.metalCost ?? 0) * count
                                    };
                                }
                            )
                        });
                    }
                    console.timeEnd(`match-unit-value-graph: entity ${entity.id}`);
                }

                console.timeEnd(`match-unit-value-graph: all`);
            },

            makeChart: function(): void {
                this.chart?.destroy();
                this.chart = null;

                const canvas: HTMLElement | null = document.getElementById("match-unit-value-graph");
                if (canvas == null) {
                    return console.error(`MatchUnitValueGraph> missing #match-unit-value-graph`);
                }

                const ctx = (canvas as HTMLCanvasElement).getContext("2d");
                if (ctx == null) {
                    return console.error(`MatchUnitValueGraph> no 2d context?`);
                }

                const dataset = this.intervals.filter(iter => iter.entityID == this.SelectedEntity);

                const rgbColors: RGB[] = ColorUtils.colorGradients(dataset.length,
                    ColorUtils.randomRGB(),
                    ColorUtils.randomRGB()
                );
                
                const colors: string[] = rgbColors.map(iter => ColorUtils.rgbToHex(iter));

                this.chart = new Chart(ctx, {
                    type: "line",
                    data: {
                        labels: this.intervals.map(iter => iter.frame).filter((v, i, arr) => arr.indexOf(v) == i).map(iter => `${TimeUtils.duration(iter / 30)}`),
                        datasets: [
                            {
                                data: this.intervals.filter(iter => iter.entityID == this.SelectedEntity).map(iter => {
                                    return {
                                        x: iter.frame,
                                        y: iter.entries.filter(iter => this.selectedUnitDef == null || iter.defName == this.selectedUnitDef)
                                            .reduce((acc, iter) => acc += iter.metalCost, 0)
                                    }
                                }),
                                label: "Metal value",
                                borderColor: this.selectedEntity?.color ?? "#333333",
                                backgroundColor: this.selectedEntity?.color ?? "#333333",
                                fill: false,
                            },
                            ...this.unitDefs.map((iter, index) => {
                                return {
                                    data: this.intervals.filter(iter => iter.entityID == this.SelectedEntity).map(i => {
                                        return {
                                            x: i.frame,
                                            y: i.entries.filter(i2 => i2.defName == iter.definitionName)
                                                .reduce((acc, iter) => acc += iter.metalCost, 0)
                                        }
                                    }),
                                    label: `${iter.name}`,
                                    borderColor: colors[index],
                                    backgroundColor: colors[index],
                                }
                            })
                        ]
                    },
                    options: {
                        scales: {
                            x: {
                                reverse: false,
                                ticks: {
                                    color: "#fff",
                                },
                                grid: {
                                    color: "#666",
                                    display: true,
                                },
                            },
                            y: {
                                ticks:{
                                    color: "#fff"
                                },
                                grid: {
                                    color: "#666"
                                }
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,
                        plugins: {
                            'html-legend': {
                                containerID: "team-stat-legend"
                            },
                            tooltip: {
                                enabled: false,
                                mode: "index",
                                position: "nearest",
                                intersect: false,
                                external: (ctx) => TableUtils.chart("team-stats-chart-tooltip", ctx,
                                    TableUtils.defaultValueFormatter,
                                    (label: string): string => {
                                        return label;
                                    }
                                )
                            },
                            legend: {
                                display: false,
                                labels: {
                                    color: "#fff",
                                    filter: (item) => item.hidden != true
                                },
                                position: "right"
                            }
                        },
                        hover: {
                            mode: "index",
                            intersect: false
                        }
                    },
                });
            },

            getInterestedTeams: function(entityID: string): Set<number> {
                if (entityID.startsWith("ally-team-")) {
                    const allyTeamID: number = Number.parseInt(entityID.split("-")[2]);

                    return new Set([
                        ...this.match.players.filter(iter => iter.allyTeamID == allyTeamID).map(iter => iter.teamID)
                    ]);
                } else if (entityID.startsWith("team-")) {
                    const teamID: number = Number.parseInt(entityID.split("-")[1]);

                    return new Set([teamID]);
                } else {
                    throw `unchecked value of entityID: '${entityID}'`;
                }
            },

            interestedUnitCreated: function(entityID: string): GameEventUnitCreated[] {
                if (entityID.startsWith("ally-team-")) {
                    const allyTeamID: number = Number.parseInt(entityID.split("-")[2]);

                    const interestedTeamIds: Set<number> = new Set([
                        ...this.match.players.filter(iter => iter.allyTeamID == allyTeamID).map(iter => iter.teamID)
                    ]);

                    return this.output.unitsCreated.filter(iter => {
                        return interestedTeamIds.has(iter.teamID);
                    });
                } else if (entityID.startsWith("team-")) {
                    const teamID: number = Number.parseInt(entityID.split("-")[1]);

                    return this.output.unitsCreated.filter(iter => {
                        return iter.teamID == teamID;
                    });
                } else {
                    throw `unchecked value of entityID: '${entityID}'`;
                }
            },
        },

        computed: {
            unitDefs: function(): GameEventUnitDef[] {
                const unitDefByName: Map<string, GameEventUnitDef> = new Map();
                for (const unitDef of this.output.unitDefinitions) {
                    unitDefByName.set(unitDef[1].definitionName, unitDef[1]);
                }

                const defNames: Set<string> = new Set(this.intervals
                    .filter(iter => iter.entityID == this.SelectedEntity)
                    .map(iter => [...iter.entries.map(e => e.defName)])
                    .reduce((acc, iter) => { acc.push(...iter); return acc; }, [])
                );
                
                const ret: GameEventUnitDef[] = [];
                for (const defName of defNames) {
                    const def = unitDefByName.get(defName);
                    if (def != undefined) {
                        ret.push(def);
                    }
                }

                return ret;
            },

            selectedEntity: function(): StatEntity | undefined {
                return this.entities.find(iter => iter.id == this.SelectedEntity);
            }
        },

        watch: {
            SelectedEntity: function(): void {
                this.makeChart();
            },

            selectedUnitDef: function(): void {
                this.makeChart();
            }
        },

        components: {

        }
    });
    export default MatchUnitValueGraph;

</script>