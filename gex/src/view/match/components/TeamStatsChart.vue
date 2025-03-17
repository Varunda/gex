
<template>
    <div class="d-flex flex-column">
        <div class="btn-group btn-group-vertical flex-grow-0">
            <button v-for="stat in statNames" :key="stat" @click="showedStat = stat" class="btn">
                {{ stat }}
            </button>
        </div>

        <div style="height: 500px" class="flex-grow-1">
            <canvas id="team-stats-chart" height="500"></canvas>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { GameEventTeamsStats } from "model/GameEventTeamStats";

    import Chart from "chart.js/auto/auto.esm";

    const STATS: string[] = [
        "damageDealt",
        "damageReceived",

        "energyExcess",
        "energyProduced",
        "energyReceived",
        "energySent",
        "energyUsed",

        "metalExcess",
        "metalProduced",
        "metalReceived",
        "metalSent",
        "metalUsed",

        "unitsCaptured",
        "unitsKilled",
        "unitsOutCaptured",
        "unitsProduced",
        "unitsSent"
    ];

    export const TeamStatsChart = Vue.extend({
        props: {
            stats: { type: Array as PropType<GameEventTeamsStats[]>, required: true }
        },

        data: function() {
            return {
                chart: null as Chart | null,

                showedStat: ""
            }
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
                        datasets: []
                    }
                });

            },
        },

        computed: {
            statNames: function(): string[] {
                return STATS;
            }

        },

        components: {

        }
    });
    export default TeamStatsChart;

</script>