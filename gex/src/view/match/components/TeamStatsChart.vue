
<template>
    <div>
        <collapsible header-text="Player stats" bg-color="bg-light">
            <div class="d-flex flex-row">

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
                                <div class="btn-group btn-group-vertical w-100">
                                    <button v-for="stat in group.values" :key="stat[0]" @click="showDataset(stat[0])" class="btn" :class="[ showedStat == stat[0] ? 'btn-primary' : 'btn-dark border' ]">
                                        {{ stat[1] }}
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>

                <div style="height: 600px" class="flex-grow-1">
                    <h2>Viewing {{ showedStat }}</h2>
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

    import Chart, { ChartDataset, Plugin } from "chart.js/auto/auto.esm";

    import TimeUtils from "util/Time";
    import TableUtils from "util/Table";
    import CompactUtils from "util/Compact";

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

            const items = chart.options.plugins?.legend?.labels?.generateLabels == undefined ? [] : chart.options.plugins.legend.labels.generateLabels(chart);

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
                    }
                    chart.update();
                });

                const check = document.createElement("input");
                check.type = "checkbox";
                check.checked = !(iter.hidden ?? true);
                check.classList.add("form-check-input", "mt-0");

                // Color box
                const boxSpan = document.createElement('span');
                boxSpan.style.background = iter.fillStyle?.toString() ?? "";
                boxSpan.style.borderColor = iter.strokeStyle?.toString() ?? "";
                boxSpan.style.borderWidth = iter.lineWidth + "px";
                boxSpan.style.display = 'inline-block';
                boxSpan.style.flexShrink = "0";
                boxSpan.style.height = "1em";
                boxSpan.style.width = "1em";

                // Text
                const textContainer = document.createElement("p");
                textContainer.style.color = iter.fontColor?.toString() ?? "";
                textContainer.style.margin = "0";
                textContainer.style.padding = "0";
                textContainer.style.textDecoration = iter.hidden ? "line-through" : "";
                textContainer.style.userSelect = "none";

                const text = document.createTextNode(iter.text);
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
        ["metalExcess", "Metal excess"],
        ["metalReceived", "Metal receieved"],
        ["metalSent", "Metal sent"],
        //["metalUsed", "Metal used"],

        ["energyProduced", "Energy produced"],
        ["energyExcess", "Energy excess"],
        ["energyReceived", "Energy receieved"],
        ["energySent", "Energy sent"],
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
                ["energyProduced", "Energy produced"],
                ["energyExcess", "Energy excess"],
                ["energyReceived", "Energy receieved"],
                ["energySent", "Energy sent"],
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

    export const TeamStatsChart = Vue.extend({
        props: {
            stats: { type: Array as PropType<MergedStats[]>, required: true },
            match: { type: Object as PropType<BarMatch>, required: true },
        },

        data: function() {
            return {
                chart: null as Chart | null,

                datasets: new Map() as Map<string, any>,

                perSecond: false as boolean,

                showedStat: "armyValue" as StatKey
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
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
                                external: (ctx) => TableUtils.chart("team-stats-chart-tooltip", ctx, TableUtils.defaultValueFormatter)
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
                    const map: Map<number, [number, number][]> = new Map();
                    for (const i of this.stats) {
                        const v: number | string = i[stat[0]];
                        if (typeof v == "string") {
                            throw `cannot create dataset on ${stat}, this is a string field!`;
                        }
                        const a: [number, number][] = map.get(i.teamID) ?? [];
                        a.push([v, i.frame]);

                        map.set(i.teamID, a);
                    }

                    for (const entry of map.entries()) {
                        const teamID: number = entry[0];
                        let values: [number, number][] = entry[1];

                        if (this.perSecond == true) {
                            const diff: [number, number][] = [];
                            let prev: [number, number] = values[0];
                            for (let i = 0; i < values.length; ++i) {
                                const d = values[i][0] - prev[0];
                                const dt = Math.max(1, values[i][1] - prev[1]);

                                diff.push([d / dt * 30, dt]); // 30 fps
                                prev = values[i];
                            }
                            values = diff;
                        }

                        const team: BarMatchPlayer | undefined = this.match.players.find(iter => iter.teamID == teamID);

                        const ds = {
                            data: values.map(i => i[0]),
                            label: `${team?.username ?? `<missing ${teamID}>`}`,
                            borderColor: team?.hexColor ?? "#333333",
                            backgroundColor: team?.hexColor ?? "#333333",
                            fill: false,
                            hidden: true,
                            lineTension: (this.perSecond == true) ? 0.5 : 0
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

                const keys: Set<string> = new Set(this.teamIds.map(iter => `${iter}-${field}`));
                console.log("TeamStatsChart> keys to add:", keys);

                for (const iter of this.datasets) {
                    if (keys.has(iter[0]) == false) {
                        continue;
                    }

                    const dataset: ChartDataset = iter[1];
                    if (dataset.hidden == true) {
                        console.log(`TeamStatsChart> adding dataset ${iter[0]}`);
                        dataset.hidden = false;
                        this.chart.data.datasets.push(dataset);
                    }
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

            teamIds: function(): number[] {
                return this.match.players.map(iter => iter.teamID);
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