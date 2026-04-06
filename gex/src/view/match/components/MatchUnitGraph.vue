<template>
    <div>
        <collapsible header-text="Units over time">
            <div class="d-flex flex-row">
                <div class="flex-grow-0 me-2" style="text-wrap: nowrap">

                </div>

                <div style="height: 600px" class="flex-grow-1">
                    <h2>Viewing aaa</h2>
                    <canvas id="unit-count-chart" height="600"></canvas>
                </div>

                <div class="d-flex align-items-center flex-grow-0">

                </div>
            </div>
        </collapsible>
    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import Collapsible from "components/Collapsible.vue";

    import { GameEventTeamsStats } from "model/GameEventTeamStats";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import MergedStats from "../compute/MergedStats";

    import Chart, { ChartDataset, LegendItem, Plugin } from "chart.js/auto/auto.esm";

    import TimeUtils from "util/Time";
    import TableUtils from "util/Table";
    import CompactUtils from "util/Compact";

    import EventBus from "EventBus";

    export const MatchUnitGraph = Vue.extend({
        props: {
            stats: { type: Array as PropType<MergedStats[]>, required: true },
            match: { type: Object as PropType<BarMatch>, required: true },
            ShowMobile: { type: Boolean, required: true }
        },

        data: function() {
            return {
                chart: null as Chart | null,

                unitDefs: [] as string[]

            }
        },

        mounted: function(): void {

        },

        methods: {
            makeChart: function(): void {
                if (this.chart != null) {
                    this.chart.destroy();
                    this.chart = null;
                }

                const canvas: HTMLElement | null = document.getElementById("unit-count-chart");
                if (canvas == null) {
                    return console.error(`MatchUnitGraph> missing #unit-count-chart`);
                }

                const ctx = (canvas as HTMLCanvasElement).getContext("2d");
                if (ctx == null) {
                    return console.error(`MatchUnitGraph> no 2d context?`);
                }

                this.chart = new Chart(ctx, {
                    type: "line",
                    data: {
                        labels: this.stats.map(iter => iter.frame).filter((v, i, arr) => arr.indexOf(v) == i).map(iter => `${TimeUtils.duration(iter / 30)}`),
                        datasets: []
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
                                        return label.split("{")[0];
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
                    plugins: [ htmlLegendPlugin ]
                });
            },

            makeDatasets: function(): void {
                if (this.chart == null) {
                    console.log(`TeamStatsChart> chart is null, creating`);
                    this.makeChart();
                }

                if (this.chart == null) {
                    throw `why is chart still null`;
                }

                this.datasets.clear();

                for (const stat of this.statNames) {
                    const statName: keyof MergedStats = stat[0];

                    const map: Map<number, StatEntry[]> = new Map();
                    for (const i of this.stats) {
                        if (i.id.startsWith("ally-team-")) {
                            continue;
                        }

                        const v: number | string = i[statName];
                        if (typeof v == "string") {
                            throw `cannot create dataset on ${stat}, this is a string field!`;
                        }

                        const a: StatEntry[] = map.get(i.teamID) ?? [];
                        a.push({ frame: i.frame, value: v });

                        map.set(i.teamID, a);

                        const allyTeamID: number | undefined = this.match.players.find(iter => iter.teamID == i.teamID)?.allyTeamID;
                        if (allyTeamID == undefined) {
                            console.warn(`TeamStatsChart> missing allyTeamID for player [teamID=${i.teamID}]`);
                        } else {
                            const datasetID: number = -1 * (allyTeamID + 1);
                            //console.log(`TeamStatsChart> player ${i.teamID} is on ally team ${allyTeamID} (which is going to ${datasetID})`);
                            const allyTeamStats: StatEntry[] = map.get(datasetID) ?? [];

                            const frameStats: StatEntry | undefined = allyTeamStats.find(iter => iter.frame == i.frame);
                            if (frameStats == undefined) {
                                allyTeamStats.push({ frame: i.frame, value: v });
                            } else {
                                frameStats.value += v;
                            }

                            map.set(datasetID, allyTeamStats);
                        }
                    }

                    for (const entry of map.entries()) {
                        const teamID: number = entry[0];
                        let values: StatEntry[] = entry[1];

                        if (this.perSecond == true) {
                            const diff: StatEntry[] = [];
                            let prev: StatEntry = values[0];
                            for (let i = 0; i < values.length; ++i) {
                                const d = values[i].value - prev.value;
                                const dt = Math.max(1, values[i].frame - prev.frame);

                                diff.push({ frame: dt, value: d / dt * 30 }); // 30 fps
                                prev = values[i];
                            }
                            values = diff;
                        }

                        const teamIdFromAlly: number = -1 * (teamID + 1);

                        const team: BarMatchPlayer | undefined = (teamID >= 0) 
                            ? this.match.players.find(iter => iter.teamID == teamID)
                            : this.match.players.find(iter => iter.allyTeamID == teamIdFromAlly);

                        //console.log(`TeamStatsChart> teamID ${teamID}, name ${team?.username}`);

                        const ds = {
                            data: values.map(i => i.value),
                            label: (teamID >= 0 ? `${team?.username ?? `<missing ${teamID}>`}` : `Team ${teamIdFromAlly + 1}`) + `{#${teamID}}`,
                            borderColor: team?.hexColor ?? "#333333",
                            backgroundColor: team?.hexColor ?? "#333333",
                            fill: false,
                            hidden: true,
                            lineTension: (this.perSecond == true) ? 0.5 : 0.1
                        };

                        //console.log(`created dataset ${teamID}-${stat[0]}`);
                        this.datasets.set(`${teamID}-${stat[0]}`, ds);

                        this.chart.data.datasets.push(ds);
                    }
                }
            },

            showDataset: function(field: StatKey): void {
                if (this.chart == null) {
                    console.log(`TeamStatsChart> chart is null, creating`);
                    this.makeChart();
                }

                if (this.chart == null) {
                    throw `why is chart still null`;
                }

                this.showedStat = field;

                for (const i of this.chart.data.datasets) {
                    i.hidden = true;
                }
                this.chart.data.datasets.length = 0;

                const keys: Set<string> = new Set(Array.from(this.validDatasetIds.values()).map(iter => `${iter}-${field}`));
                console.log("TeamStatsChart> keys to add:", keys);

                for (const iter of this.datasets) {
                    if (keys.has(iter[0]) == false) {
                        continue;
                    }

                    const keyParts: string[] = iter[0].split("-");
                    const statName: string = keyParts[keyParts.length - 1];
                    let datasetId: string = keyParts.slice(0, keyParts.length - 1).join("");

                    if (keyParts.length == 3) {
                        datasetId = "-" + datasetId;
                    }

                    const did: number = Number.parseInt(datasetId);

                    const dataset: ChartDataset = iter[1];
                    console.log(`TeamStatsChart> adding dataset ${iter[0]} [datasetId=${datasetId}/${did}] [hidden=${!this.shownStats.has(did)}] [statName=${statName}]`);
                    dataset.hidden = !this.shownStats.has(did);
                    this.chart.data.datasets.push(dataset);

                    this.chart.data.datasets.sort((a, b) => {
                        // a=ally team, b=ally team => smaller ally team
                        // a=ally team, b=team      => if b is in team a, then a>b, else b>a
                        // a=team,      b=ally team => if a is in team b, then b>a, else a>b
                        // a=team,      b=team      => if a and b are on the same team, sort by label, else smaller team

                        const aId: number = getDatasetIdFromLabel(a.label ?? "");
                        const bId: number = getDatasetIdFromLabel(b.label ?? "");

                        let aAtId: number = -1 * (aId + 1);
                        let bAtId: number = -1 * (bId + 1);

                        const aIsAt: boolean = aId < 0;
                        const bIsAt: boolean = bId < 0;

                        let res = 0;

                        if (aIsAt == true && bIsAt == true) {
                            res = aAtId - bAtId;
                        } else if (aIsAt == true && bIsAt == false) {
                            const bTeam: number = this.match.players.find(iter => iter.teamID == bId)?.allyTeamID ?? NaN;
                            bAtId = bTeam;

                            if (bTeam == aAtId) {
                                res = -1; // a is an ally team, and b is part of this team, so a is smaller
                            } else {
                                // a is an ally team, but b is not part of this team, so smaller team wins
                                // if a is ally team 1, and b is on ally team 2, b goes after a (1)
                                //res = bTeam - aAtId;
                                res = aAtId - bTeam;
                            }
                        } else if (aIsAt == false && bIsAt == true) {
                            const aTeam: number = this.match.players.find(iter => iter.teamID == aId)?.allyTeamID ?? NaN;
                            aAtId = aTeam;

                            if (aTeam == bAtId) {
                                res = 1; // b is an ally team, and a is part of this team, so it comes after b
                            } else {
                                // b is an ally team, but a is not part of this team, so smaller team wins
                                // if b is ally team 1, and a is on ally team 2, then b goes after a (1)
                                res = aTeam - bAtId;
                            }
                        } else if (aIsAt == false && bIsAt == false) {
                            const aTeam: number = this.match.players.find(iter => iter.teamID == aId)?.allyTeamID ?? NaN;
                            const bTeam: number = this.match.players.find(iter => iter.teamID == bId)?.allyTeamID ?? NaN;
                            aAtId = aTeam;
                            bAtId = bTeam;

                            if (aTeam == bTeam) {
                                res = a.label?.localeCompare(b.label ?? "") ?? 0;
                            } else {
                                res = aTeam - bTeam;
                            }
                        } else {
                            throw `logic error! team-stats-chart-dataset-sort`;
                        }

                        //console.log(`TeamStatsChart> a=${aId},${aIsAt},${aAtId} ||| b=${bId},${bIsAt},${bAtId} => ${res}`);

                        return res;
                    });
                }

                this.chart.update();
            },
        },

        computed: {
            statNames: function(): [StatKey, string][] {
                return STATS;
            },

            statGroups: function() {
                return STAT_GROUPS;
            },

            selectedStatName: function(): string {
                return (this.statNames.find(iter => iter[0] == this.showedStat) ?? ["", ""])[1];
            },

            teamIds: function(): number[] {
                //return [...this.match.players.map(iter => iter.teamID), ...this.match.allyTeams.map(iter => -1 * (iter.allyTeamID + 1))];
                return [...this.match.players.map(iter => iter.teamID)];
            },

            allyTeamIdsAsDatasetIds: function(): number[] {
                return [...this.match.allyTeams.map(iter => -1 * (iter.allyTeamID + 1))];
            },

            datasetIds: function(): number[] {
                return [...this.teamIds, ...this.allyTeamIdsAsDatasetIds];
            }
        },

        watch: {
            perSecond: function(): void {
                this.makeDatasets();
                this.showDataset(this.showedStat);
            }
        },

        components: {
            Collapsible
        }
    });
    export default MatchUnitGraph;

</script>