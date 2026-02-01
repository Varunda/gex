
<template>
    <div>
        <collapsible header-text="Player stats" bg-color="bg-light" size-class="h1">

            <div v-if="ShowMobile == true">
                <div class="flex-grow-0 me-2" style="text-wrap: nowrap">

                    <button class="btn w-100 mb-3" @click="perSecond = !perSecond" :class="[ perSecond ? 'btn-primary' : 'btn-dark border' ]">
                        Show per sec
                    </button>

                    <div class="accordion accordion-flush" id="stat-accordion-parent">
                        <div v-for="(group, index) of statGroups" class="accordion-item mb-2" :key="group.name">
                            <h2 class="accordion-header">
                                <button class="accordion-button me-2" :class="{ 'collapsed': index != 0 }" type="button" data-bs-toggle="collapse" :data-bs-target="'#stats-group-' + group.id">
                                    {{ group.name }}
                                </button>
                            </h2>

                            <div :id="'stats-group-' + group.id" class="accordion-collapse collapse" :class="{ 'show': index == 0 }" data-bs-parent="#stat-accordion-parent">
                                <div class="btn-group btn-group-vertical w-100 mb-2">
                                    <button v-for="stat in group.values" :key="stat[0]" @click="showDataset(stat[0])" class="btn ms-0" :class="[ showedStat == stat[0] ? 'btn-primary' : 'btn-dark border' ]">
                                        {{ stat[1] }}
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div class="flex-grow-1">
                    <h2>Viewing {{ selectedStatName }}</h2>
                    <div style="height: 600px">
                        <canvas id="team-stats-chart" height="600"></canvas>
                    </div>
                </div>

                <div class="d-flex align-items-center flex-grow-0">
                    <ul id="team-stat-legend" class="ps-0"></ul>
                </div>

            </div>

            <div v-else class="d-flex flex-row">
                <div class="flex-grow-0 me-2" style="text-wrap: nowrap">

                    <button class="btn w-100 mb-3" @click="perSecond = !perSecond" :class="[ perSecond ? 'btn-primary' : 'btn-dark border' ]">
                        Show per sec
                    </button>

                    <div class="accordion accordion-flush" id="stat-accordion-parent">
                        <div v-for="(group, index) of statGroups" class="accordion-item mb-2" :key="group.name">
                            <h2 class="accordion-header">
                                <button class="accordion-button me-2" :class="{ 'collapsed': index != 0 }" type="button" data-bs-toggle="collapse" :data-bs-target="'#stats-group-' + group.id">
                                    {{ group.name }}
                                </button>
                            </h2>

                            <div :id="'stats-group-' + group.id" class="accordion-collapse collapse" :class="{ 'show': index == 0 }" data-bs-parent="#stat-accordion-parent">
                                <div class="btn-group btn-group-vertical w-100 mb-2">
                                    <button v-for="stat in group.values" :key="stat[0]" @click="showDataset(stat[0])" class="btn ms-0" :class="[ showedStat == stat[0] ? 'btn-primary' : 'btn-dark border' ]">
                                        {{ stat[1] }}
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div style="height: 600px" class="flex-grow-1">
                    <h2>Viewing {{ selectedStatName }}</h2>
                    <canvas id="team-stats-chart" height="600"></canvas>
                </div>

                <div class="d-flex align-items-center flex-grow-0">
                    <ul id="team-stat-legend" class="ps-0"></ul>
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

    const getOrCreateLegendList = (chart: Chart, id: string) => {
        const container = document.getElementById(id);
        let list = container?.querySelector("ul");

        if (!list) {
            list = document.createElement("ul");
            list.style.display = "flex";
            list.style.flexDirection = "row";
            list.style.margin = "0";
            list.style.padding = "0";

            container?.appendChild(list);
        }

        return container;
    };

    // https://github.com/chartjs/Chart.js/blob/master/docs/samples/legend/html.md
    // HTML legend plugin that doesn't hide the player when hidden, and adds a check box to
    // make it clear that this is clickable
    const htmlLegendPlugin: Plugin = {
        id: "html-legend",

        afterUpdate(chart: Chart, args, options) {

            const containerID: string | null | undefined = (options as any).containerID;
            if (!containerID) {
                throw `missing containerID (put it in options.plugins.'html-legend'.containerID)`;
            }

            const ul = getOrCreateLegendList(chart, containerID);
            if (ul == null) {
                throw `missing element with container ID ${containerID}`;
            }

            while (ul.firstChild) {
                ul.firstChild.remove();
            }

            const items: LegendItem[] = chart.options.plugins?.legend?.labels?.generateLabels == undefined ? [] : chart.options.plugins.legend.labels.generateLabels(chart);

            items.forEach((iter) => {

                const li = document.createElement("li");
                li.style.alignItems = "center";
                li.style.cursor = "pointer";
                li.style.display = "flex";
                li.style.flexDirection = "row";
                li.style.marginLeft = "0.25rem";

                li.addEventListener("click", (ev) => {
                    const { type } = chart.config;
                    if (type == "pie" || type == "doughnut") {
                        chart.toggleDataVisibility(iter.datasetIndex);
                    } else {
                        chart.setDatasetVisibility(iter.datasetIndex, !chart.isDatasetVisible(iter.datasetIndex));

                        const datasetId: number = getDatasetIdFromLabel(iter.text);
                        if (Number.isNaN(datasetId)) {
                            console.warn(`TeamStatsChart> failed to convert dataset id string into a number! ${iter.text} => ${datasetId}`);
                            return;
                        }

                        if (chart.isDatasetVisible(iter.datasetIndex)) {
                            EventBus.$emit("enable-dataset-id", datasetId);
                        } else {
                            EventBus.$emit("disable-dataset-id", datasetId);
                        }
                    }
                    chart.update();
                });

                const check = document.createElement("input");
                check.type = "checkbox";
                check.checked = !(iter.hidden ?? true);
                check.classList.add("form-check-input", "mt-0", "me-1");

                // Color box
                const boxSpan = document.createElement('span');
                boxSpan.style.background = iter.fillStyle?.toString() ?? "";
                boxSpan.style.borderColor = iter.strokeStyle?.toString() ?? "";
                boxSpan.style.borderWidth = iter.lineWidth + "px";
                boxSpan.style.display = 'inline-block';
                boxSpan.style.flexShrink = "0";
                boxSpan.style.height = "1em";
                boxSpan.style.width = "1em";
                boxSpan.classList.add("me-1");

                // Text
                const textContainer = document.createElement("p");
                textContainer.style.color = iter.fontColor?.toString() ?? "";
                textContainer.style.margin = "0";
                textContainer.style.padding = "0";
                textContainer.style.textDecoration = iter.hidden ? "line-through" : "";
                textContainer.style.userSelect = "none";

                const text = document.createTextNode(iter.text.split("{")[0]);
                textContainer.appendChild(text);

                li.appendChild(check);
                li.appendChild(boxSpan);
                li.appendChild(textContainer);
                ul.appendChild(li);
            });
        }
    };

    type StatKey = keyof MergedStats;

    const STATS: [StatKey, string][] = [
        ["unitsProduced", "Units created"],
        ["unitsKilled", "Units killed"],

        ["damageDealt", "Damage dealt"],
        ["damageReceived", "Damage receieved"],
        ["actions", "Actions"],

        ["totalValue", "Total value"],
        ["defenseValue", "Defense value"],
        ["ecoValue", "Eco value"],
        ["utilValue", "Util value"],
        ["otherValue", "Other value"],
        ["armyValue", "Army value"],
        ["buildPowerAvailable", "Build power total"],
        ["buildPowerUsed", "Build power used"],
        ["buildPowerPercent", "Build power percent"],

        ["metalProduced", "Metal produced"],
        ["metalCurrent", "Metal Current"],
        ["metalExcess", "Metal excess"],
        ["metalReceived", "Metal receieved"],
        ["metalSent", "Metal sent"],
        ["metalExcessPercent", "Metal excess %"],
        //["metalUsed", "Metal used"],

        ["energyProduced", "Energy produced"],
        ["energyCurrent", "Energy Current"],
        ["energyExcess", "Energy excess"],
        ["energyReceived", "Energy receieved"],
        ["energySent", "Energy sent"],
        ["energyExcessPercent", "Energy excess %"],
        //["energyUsed", "Energy used"], // not useful? is always y=x

        ["unitsCaptured", "Units captured"],
        ["unitsSent", "Units sent"],
        ["unitsReceived", "Units received"],
        ["unitsOutCaptured", "Units out captured"]
    ];

    type StatGroup = {
        id: string;
        name: string;
        values: [StatKey, string][]
    };

    const STAT_GROUPS: StatGroup[] = [
        {
            id: "basic",
            name: "Basic",
            values: [
                ["armyValue", "Army value"],
                ["unitsProduced", "Units created"],
                ["unitsKilled", "Units killed"],
                ["actions", "Actions"],
                ["damageDealt", "Damage dealt"],
                ["damageReceived", "Damage receieved"],
            ]
        },
        {
            id: "unit-value",
            name: "Unit value",
            values: [
                ["totalValue", "Total value"],
                ["defenseValue", "Defense value"],
                ["ecoValue", "Eco value"],
                ["utilValue", "Util value"],
                ["otherValue", "Other value"],
            ]
        },
        {
            id: "build-power",
            name: "Build power",
            values: [
                ["buildPowerAvailable", "Build power total"],
                ["buildPowerUsed", "Build power used"],
                ["buildPowerPercent", "Build power usage"]
            ]
        },
        {
            id: "eco",
            name: "Eco",
            values: [
                ["metalProduced", "Metal produced"],
                ["metalExcess", "Metal excess"],
                ["metalReceived", "Metal receieved"],
                ["metalSent", "Metal sent"],
                ["metalCurrent", "Metal Current"],
                ["metalExcessPercent", "Metal excess %"],
                ["energyProduced", "Energy produced"],
                ["energyExcess", "Energy excess"],
                ["energyReceived", "Energy receieved"],
                ["energySent", "Energy sent"],
                ["energyCurrent", "Energy Current"],
                ["energyExcessPercent", "Energy excess %"],
            ]
        },
        {
            id: "unit",
            name: "Unit",
            values: [
                ["unitsCaptured", "Units captured"],
                ["unitsSent", "Units sent"],
                ["unitsReceived", "Units received"],
                ["unitsOutCaptured", "Units out captured"]
            ]
        }

    ];

    type StatSetEntry = {
        id: string;
        name: string;
        values: number[];
    }        

    type StatSet = {
        teamID: number | null;
        name: string;

        stats: StatSetEntry[];
    }

    type StatEntry = {
        frame: number;
        value: number;
    }

    /**
     * takes the label of a dataset item, and gets the ID of the "team id" (which can also be the ally team ID)
     * @param label label of the dataset from the chart
     */
    const getDatasetIdFromLabel = (label: string): number =>{
        const reg: RegExpMatchArray | null = label.match(/\{#(-?\d+)\}/);
        if (reg == null) {
            console.warn(`TeamStatsChart> failed to find dataset id from label ${label}`);
            return NaN;
        }

        if (reg.length != 2) {
            console.warn(`TeamStatsChart> expected regex match to be 2 elements, got ${reg.length} instead! ${reg}`);
            return NaN;
        }

        const datasetIdStr: string = reg.at(1)!;
        return Number.parseInt(datasetIdStr);
    }

    // 2025-04-13 TODO: ok, how i handle the teams and ally teams is fucking awful
    //      the "team id" for an ally team is just the negative +1 (because ally teams have a 0)
    //      then in order to persist the changes in teams shown, the dataset id is hidden within the label,
    //      but is displayed without the label

    export const TeamStatsChart = Vue.extend({
        props: {
            stats: { type: Array as PropType<MergedStats[]>, required: true },
            match: { type: Object as PropType<BarMatch>, required: true },
            ShowMobile: { type: Boolean, required: true }
        },

        data: function() {
            return {
                chart: null as Chart | null,

                datasets: new Map() as Map<string, any>,

                shownStats: new Set() as Set<number>,
                validDatasetIds: new Set() as Set<number>,

                perSecond: false as boolean,

                showedStat: "armyValue" as StatKey, 

                unitDefNameFilter: null as string | null
            }
        },

        mounted: function(): void {

            EventBus.$on("enable-dataset-id", (datasetId: number) => {
                console.log(`TeamStatsChart> enable-dataset-id ${datasetId}`);
                this.shownStats.add(datasetId);
            });

            EventBus.$on("disable-dataset-id", (datasetId: any) => {
                console.log(`TeamStatsChart> diable-dataset-id ${datasetId}`);
                this.shownStats.delete(datasetId);
            });

            this.$nextTick(() => {
                // by default, if the game isn't a 1v1, only show the team aggregate stats
                const largestTeam: number = Math.max(...this.match.allyTeams.map(iter => iter.playerCount));
                if (largestTeam == 1) {
                    this.shownStats = new Set(this.teamIds);
                    this.validDatasetIds = new Set(this.teamIds);
                } else {
                    this.shownStats = new Set(this.allyTeamIdsAsDatasetIds);
                    this.validDatasetIds = new Set(this.teamIds);
                    for (const iter of this.allyTeamIdsAsDatasetIds) {
                        this.validDatasetIds.add(iter);
                    }
                }

                this.makeChart();
                this.makeDatasets();
                this.showDataset("armyValue");
            });
        },

        methods: {
            makeChart: function(): void {
                if (this.chart != null) {
                    this.chart.destroy();
                    this.chart = null;
                }

                const canvas: HTMLElement | null = document.getElementById("team-stats-chart");
                if (canvas == null) {
                    return console.error(`TeamStatsChart> missing #team-stats-chart`);
                }

                const ctx = (canvas as HTMLCanvasElement).getContext("2d");
                if (ctx == null) {
                    return console.error(`TeamStatsChart> no 2d context?`);
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
    export default TeamStatsChart;

</script>