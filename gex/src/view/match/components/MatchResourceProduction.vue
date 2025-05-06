<template>
    <div>
        <h2 class="wt-header bg-primary">Unit resource production</h2>

        <div class="d-flex flex-wrap mb-3">
            <button
                v-for="player in match.players"
                :key="player.teamID"
                class="btn m-1 flex-grow-0"
                :style="{
                    'background-color': selectedTeam == player.teamID ? player.hexColor : 'var(--bs-secondary)',
                    color: selectedTeam == player.teamID ? 'white' : player.hexColor,
                }"
                @click="selectedTeam = player.teamID"
            >
                <span style="text-shadow: 1px 1px 1px black">
                    {{ player.username }}
                </span>
            </button>
        </div>

        <h4 v-if="selectedPlayer" class="text-center">
            Viewing unit stats for
            <span :style="{ color: selectedPlayer.hexColor }">
                {{ selectedPlayer.username }}
            </span>
        </h4>

        <a-table
            :entries="entries"
            :show-footer="true"
            :show-filters="true"
            default-sort-field="count"
            default-sort-order="desc"
            :page-sizes="[5, 10, 25, 50, 100]"
            :default-page-size="5"
        >
            <a-col sort-field="name">
                <a-header>
                    <b>Unit</b>
                </a-header>

                <a-filter field="name" type="string" method="input" :conditions="['contains', 'equals']"> </a-filter>

                <a-body v-slot="entry">
                    <img :src="'/image-proxy/UnitIcon?defName=' + entry.defName" height="24" width="24" />
                    {{ entry.name }}
                </a-body>
            </a-col>

            <a-col sort-field="count">
                <a-header>
                    <b>Produced</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.count }}
                </a-body>

                <a-footer>
                    <b>Total</b>
                </a-footer>
            </a-col>

            <a-col sort-field="metalMade">
                <a-header>
                    <b>Metal made</b>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="{ 'text-muted': entry.metalMade == 0 }">
                        {{ entry.metalMade | compact }}
                    </span>
                </a-body>

                <a-footer>
                    <b>{{ sumMetalMade | compact }}</b>
                </a-footer>
            </a-col>

            <a-col sort-field="metalUsed">
                <a-header>
                    <b>Metal used</b>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="{ 'text-muted': entry.metalUsed == 0 }">
                        {{ entry.metalUsed | compact }}
                    </span>
                </a-body>

                <a-footer>
                    <b>{{ sumMetalUsed | compact }}</b>
                </a-footer>
            </a-col>

            <a-col sort-field="energyMade">
                <a-header>
                    <b>Energy made</b>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="{ 'text-muted': entry.energyMade == 0 }">
                        {{ entry.energyMade | compact }}
                    </span>
                </a-body>

                <a-footer>
                    <b>{{ sumEnergyMade | compact }}</b>
                </a-footer>
            </a-col>

            <a-col sort-field="energyUsed">
                <a-header>
                    <b>Energy used</b>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="{ 'text-muted': entry.energyUsed == 0 }">
                        {{ entry.energyUsed | compact }}
                    </span>
                </a-body>

                <a-footer>
                    <b>{{ sumEnergyUsed | compact }}</b>
                </a-footer>
            </a-col>
        </a-table>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol, ARank, ATableType } from "components/ATable";
    import Collapsible from "components/Collapsible.vue";
    import InfoHover from "components/InfoHover.vue";

    import { ResourceProductionData, ResourceProductionEntry } from "../compute/ResourceProductionData";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import "filters/LocaleFilter";
    import "filters/CompactFilter";

    export const MatchResourceProduction = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            data: { type: Array as PropType<ResourceProductionData[]>, required: true },
        },

        data: function () {
            return {
                selectedTeam: 0 as number,
            };
        },

        methods: {},

        computed: {
            entries: function (): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.data.find((iter) => iter.teamID == this.selectedTeam)?.units ?? []);
            },

            selectedPlayer: function (): BarMatchPlayer | null {
                return this.match.players.find((iter) => iter.teamID == this.selectedTeam) || null;
            },

            sumMetalMade: function (): number {
                if (this.entries.state != "loaded") {
                    throw `what 3245712`;
                }
                return this.entries.data.reduce((acc, val) => (acc += val.metalMade), 0);
            },

            sumMetalUsed: function (): number {
                if (this.entries.state != "loaded") {
                    throw `what 324572`;
                }
                return this.entries.data.reduce((acc, val) => (acc += val.metalUsed), 0);
            },

            sumEnergyMade: function (): number {
                if (this.entries.state != "loaded") {
                    throw `what 324571`;
                }
                return this.entries.data.reduce((acc, val) => (acc += val.energyMade), 0);
            },

            sumEnergyUsed: function (): number {
                if (this.entries.state != "loaded") {
                    throw `what 324712`;
                }
                return this.entries.data.reduce((acc, val) => (acc += val.energyUsed), 0);
            },
        },

        components: {
            ATable,
            AHeader,
            ABody,
            AFooter,
            AFilter,
            ACol,
            Collapsible,
            InfoHover,
        },
    });
    export default MatchResourceProduction;
</script>
