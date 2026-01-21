
<template>
    <div>
        <collapsible header-text="Openers" bg-color="bg-light" size-class="h1">
            <template v-slot:header>
                <info-hover text="Buildings the first 8 buildings made in the first 90 seconds"></info-hover>
            </template>

            <template v-slot:default>
                <toggle-button v-model="showPictures" class="mb-2">
                    Use images
                </toggle-button>

                <div v-if="showPictures == true">
                    <div v-for="player in sorted" :key="player.teamID" :style="playerStyle(player.color)" class="mb-3 p-2">
                        <h3 :style="{ 'color': player.color }">
                            {{ player.playerName }}
                            ({{ player.playerFaction }})
                        </h3>

                        <h4 class="mb-0">Buildings</h4>
                        <div class="d-flex flex-wrap" style="gap: 1rem;">
                            <div v-for="(b, index) in player.buildings" :key="index" class="text-center border position-sticky" style="border-radius: 0.5rem;">
                                <div class="text-center border position-sticky" style="border-radius: 0.5rem 0.5rem 0 0;">
                                    <div class="text-outline image-parent">
                                        <span v-if="b.amount > 1">
                                            {{ b.amount }}x
                                        </span>
                                        {{ b.name | compactUnitName }}
                                    </div>

                                    <unit-pic :name="b.defName" :size="96" style="border-radius: 0.5rem 0.5rem 0 0;"></unit-pic>
                                </div>

                                {{ b.firstFrame / 30 | mduration }}
                            </div>
                        </div>

                        <hr class="border mt-2 mb-1"/>

                        <h4 class="mb-0">Units</h4>
                        <div class="d-flex flex-wrap" style="gap: 1rem;">
                            <div v-for="(u, index) in player.units" :key="index" class="text-center border position-sticky" style="border-radius: 0.5rem;">
                                <div class="text-center border position-sticky" style="border-radius: 0.5rem 0.5rem 0 0;">
                                    <div class="text-outline image-parent">
                                        <span v-if="u.amount > 1">
                                            {{ u.amount }}x
                                        </span>
                                        {{ u.name.replace("Construction", "Con") }}
                                    </div>

                                    <unit-pic :name="u.defName" :size="96" style="border-radius: 0.5rem 0.5rem 0 0;"></unit-pic>
                                </div>

                                {{ u.firstFrame / 30 | mduration }}
                            </div>
                        </div>
                    </div>
                </div>

                <div v-else>
                    <div v-for="player in sorted" :key="player.teamID" :style="playerStyle(player.color)" class="mb-3 p-2">
                        <h3 :style="{ 'color': player.color }">
                            {{ player.playerName }}
                            ({{ player.playerFaction }})
                        </h3>

                        <div>
                            <span v-for="(b, index) in player.buildings" :key="index" class="buildings">
                                <span class="ms-1"></span>

                                <span v-if="b.amount > 1">
                                    {{b.amount}}x
                                </span>

                                <unit-icon :name="b.defName" :size="16"></unit-icon>
                                <strong v-if="b.isFactory">
                                    {{ b.name }}
                                </strong>
                                <span v-else>
                                    {{ b.name }}
                                </span>
                            </span>
                        </div>

                        <div>
                            <span v-for="(u, index) in player.units" :key="index" class="units">
                                <span class="ms-1"></span>

                                <span v-if="u.amount > 1">
                                    {{u.amount}}x
                                </span>

                                <unit-icon :name="u.defName" :size="16"></unit-icon>
                                <span>
                                    {{ u.name }}
                                </span>
                            </span>
                        </div>
                    </div>
                </div>
            </template>
        </collapsible>
    </div>
</template>

<style scoped>
    .buildings:not(:last-child)::after {
        content: "\02192";
    }

    .units:not(:last-child)::after {
        content: "\02192";
    }

    .image-parent {
        width: 100%;
        text-align: center;
        position: absolute;
        bottom: 0;
        background-color: #00000066;
        padding-right: 0.25rem;
        padding-left: 0.25rem;

    }
</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import InfoHover from "components/InfoHover.vue";
    import Collapsible from "components/Collapsible.vue";
    import UnitIcon from "components/app/UnitIcon.vue";
    import UnitPic from "components/app/UnitPic.vue";
    import ToggleButton from "components/ToggleButton";

    import { BarMatch } from "model/BarMatch";

    import { PlayerOpener } from "../compute/PlayerOpenerData";

    import "filters/MomentFilter";
    import "filters/CompactUnitNameFilter";

    export const MatchOpener = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            openers: { type: Array as PropType<PlayerOpener[]>, required: true }
        },

        data: function() {
            return {
                showPictures: true as boolean
            }
        },

        created: function(): void {
            this.showPictures = this.match.players.length == 2;
        },

        methods: {
            playerStyle: function(color: string): object {
                return {
                    "background-color": color + "11",
                    "border": `${color} solid 1px`,
                    "border-radius": "0.25rem",
                };
            }
        },

        computed: {
            sorted: function(): PlayerOpener[] {
                return [...this.openers].sort((a, b) => {
                    return a.teamID - b.teamID;
                });
            }

        },

        components: {
            InfoHover, Collapsible, UnitIcon, UnitPic, ToggleButton
        }
    });
    export default MatchOpener;
</script>