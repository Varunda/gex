
<template>
    <div>

        <h2 class="wt-header">Unit stats</h2>

        <div class="btn-group w-100 mb-2">
            <button v-for="player in match.players" :key="player.teamID" class="btn" :style=" {
                    'background-color': (selectedTeam == player.teamID) ? player.hexColor : 'var(--bs-secondary)',
                    'color': (selectedTeam == player.teamID) ? 'white' : player.hexColor
                }" @click="selectedTeam = player.teamID">

                {{ player.username }}
            </button>
        </div>

        <h6 v-if="selectedPlayer">
            Viewing unit stats for
            <span :style="{ 'color': selectedPlayer.hexColor }">
                {{ selectedPlayer.username }}
            </span>
        </h6>

        <a-table :entries="data" display-type="table" :show-filters="true" default-sort-field="produced" default-sort-order="desc">
            <a-col sort-field="name">
                <a-header>
                    <b>Unit</b>
                </a-header>

                <a-body v-slot="entry">
                    <img :src="'/image-proxy/UnitIcon?defName=' + entry.defName" height="24" width="24">
                    {{ entry.name }}
                </a-body>
            </a-col>

            <a-col sort-field="produced">
                <a-header>
                    <b>Produced</b>
                    <info-hover text="How many of this unit were produced"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.produced }}
                </a-body>
            </a-col>

            <a-col sort-field="kills">
                <a-header>
                    <b>Kills</b>
                    <info-hover text="How many kills these units got"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.kills }}
                </a-body>
            </a-col>

            <a-col sort-field="lost">
                <a-header>
                    <b>Lost</b>
                    <info-hover text="How many of this unit were lost"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.lost }}
                </a-body>
            </a-col>

            <a-col sort-field="metalKilled">
                <a-header>
                    <b>Metal killed</b>
                    <info-hover text="The total metal cost of units killed by this type of unit"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.metalKilled }}
                </a-body>
            </a-col>

            <a-col sort-field="energyKilled">
                <a-header>
                    <b>Energy killed</b>
                    <info-hover text="The total energy cost of units killed by this type of unit"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.energyKilled }}
                </a-body>
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

    import { UnitStats } from "../compute/UnitStatData";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import "filters/LocaleFilter";

    export const MatchUnitStats = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            UnitStats: { type: Array as PropType<UnitStats[]>, required: true }
        },

        data: function() {
            return {
                selectedTeam: 0 as number,
            }
        },

        methods: {

        },

        computed: {
            data: function(): Loading<UnitStats[]> {
                return Loadable.loaded(this.UnitStats.filter(iter => iter.teamID == this.selectedTeam));
            },

            selectedPlayer: function(): BarMatchPlayer | null {
                return this.match.players.find(iter => iter.teamID == this.selectedTeam) || null;
            }
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            Collapsible, InfoHover
        }

    });
    export default MatchUnitStats;
</script>