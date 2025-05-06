<template>
    <div>
        <collapsible header-text="Openers" bg-color="bg-light" size-class="h1">
            <template v-slot:header>
                <info-hover text="Buildings the first 8 buildings made in the first 90 seconds"></info-hover>
            </template>

            <template v-slot:default>
                <div v-for="player in openers" :key="player.teamID" :style="playerStyle(player.color)" class="mb-3 p-2">
                    <h3 :style="{ color: player.color }">
                        {{ player.playerName }}
                    </h3>

                    <div>
                        <span v-for="(b, index) in player.buildings" :key="index" class="units">
                            <span class="ms-1"></span>

                            <span v-if="b.amount > 1"> {{ b.amount }}x </span>

                            <img :src="'/image-proxy/UnitIcon?defName=' + b.defName" height="16" />
                            <strong v-if="b.isFactory">
                                {{ b.name }}
                            </strong>
                            <span v-else>
                                {{ b.name }}
                            </span>
                        </span>
                    </div>
                </div>
            </template>
        </collapsible>
    </div>
</template>

<style scoped>
    .units:not(:last-child)::after {
        content: "\02192";
    }
</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import InfoHover from "components/InfoHover.vue";
    import Collapsible from "components/Collapsible.vue";

    import { PlayerOpener } from "../compute/PlayerOpenerData";

    export const MatchOpener = Vue.extend({
        props: {
            openers: { type: Array as PropType<PlayerOpener[]>, required: true },
        },

        data: function () {
            return {};
        },

        methods: {
            playerStyle: function (color: string): object {
                return {
                    //"background-color": color + "22",
                    border: `${color} solid 1px`,
                    "border-radius": "0.25rem",
                };
            },
        },

        components: {
            InfoHover,
            Collapsible,
        },
    });
    export default MatchOpener;
</script>
