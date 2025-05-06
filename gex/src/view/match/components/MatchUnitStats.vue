<template>
    <div>
        <h1 class="wt-header bg-light">Unit stats</h1>

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

        <a-table :entries="data" display-type="table" :show-filters="true" default-sort-field="produced" default-sort-order="desc">
            <a-col sort-field="name">
                <a-header>
                    <h5><b>Units</b></h5>
                </a-header>

                <a-body v-slot="entry">
                    <img :src="'/image-proxy/UnitIcon?defName=' + entry.defName" height="24" width="24" />
                    {{ entry.name }}
                    <info-hover :text="entry.definition.tooltip"></info-hover>
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Type</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.definition.category }}
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
                    <span :class="{ 'text-muted': entry.kills == 0 }">
                        {{ entry.kills }}
                    </span>
                </a-body>
            </a-col>

            <a-col sort-field="lost">
                <a-header>
                    <b>Lost</b>
                    <info-hover text="How many of this unit were lost"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="{ 'text-muted': entry.lost == 0 }">
                        {{ entry.lost }}
                    </span>
                </a-body>
            </a-col>

            <a-col sort-field="damageDealt">
                <a-header>
                    <b>Damage dealt</b>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="{ 'text-muted': entry.damageDealt == 0 }">
                        {{ entry.damageDealt | compact }}
                    </span>
                </a-body>
            </a-col>

            <a-col sort-field="damageDealt">
                <a-header>
                    <b>Damage taken</b>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="{ 'text-muted': entry.damageTaken == 0 }">
                        {{ entry.damageTaken | compact }}
                    </span>
                </a-body>
            </a-col>

            <a-col sort-field="metalKilled">
                <a-header>
                    <b>Metal killed</b>
                    <info-hover text="The total metal cost of units killed by this type of unit"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="{ 'text-muted': entry.metalKilled == 0 }">
                        {{ entry.metalKilled }}
                    </span>
                </a-body>
            </a-col>

            <a-col sort-field="energyKilled">
                <a-header>
                    <b>Energy killed</b>
                    <info-hover text="The total energy cost of units killed by this type of unit"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="{ 'text-muted': entry.energyKilled == 0 }">
                        {{ entry.energyKilled }}
                    </span>
                </a-body>
            </a-col>

            <a-col sort-field="metalRatio">
                <a-header>
                    <b>Metal efficiency</b>
                    <info-hover text="Total metal worth of units killed by this type of unit"></info-hover>
                </a-header>

                <a-body v-slot="entry"> {{ (entry.metalRatio * 100) | locale(2) }}% </a-body>
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
    import "filters/CompactFilter";

    export const MatchUnitStats = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            UnitStats: { type: Array as PropType<UnitStats[]>, required: true },
        },

        data: function () {
            return {
                selectedTeam: 0 as number,
            };
        },

        methods: {},

        computed: {
            data: function (): Loading<UnitStats[]> {
                return Loadable.loaded(this.UnitStats.filter((iter) => iter.teamID == this.selectedTeam));
            },

            selectedPlayer: function (): BarMatchPlayer | null {
                return this.match.players.find((iter) => iter.teamID == this.selectedTeam) || null;
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
    export default MatchUnitStats;
</script>
