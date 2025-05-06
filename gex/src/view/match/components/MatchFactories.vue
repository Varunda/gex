<template>
    <div>
        <h1 class="wt-header bg-secondary">
            Factories
            <info-hover text="Factories and what units they produced"></info-hover>
        </h1>

        <div v-for="player in data" :key="player.teamID" :style="playerStyle(player.color)" class="mb-3 p-2">
            <h3 :style="{ color: player.color }">
                {{ player.name }}
            </h3>

            <div class="list-group">
                <div v-for="(b, index) in player.factories" :key="index">
                    <a class="btn btn-link" data-bs-toggle="collapse" :href="'#factory-units-' + b.factoryID"> {{ b.name }} - {{ b.totalMade }} </a>

                    <div :id="'factory-units-' + b.factoryID" class="collapse">
                        <div v-for="unit in b.units" :key="b.factoryID + '-' + unit.defID">{{ unit.name }} - {{ unit.count }}</div>
                    </div>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import InfoHover from "components/InfoHover.vue";

    import { PlayerFactories } from "../compute/FactoryData";

    export const MatchFactories = Vue.extend({
        props: {
            data: { type: Array as PropType<PlayerFactories[]>, required: true },
        },

        data: function () {
            return {};
        },

        methods: {
            playerStyle: function (color: string): object {
                return {
                    "background-color": color + "22",
                    border: `${color} solid 1px`,
                    "border-radius": "0.25rem",
                };
            },
        },

        components: {
            InfoHover,
        },
    });
    export default MatchFactories;
</script>
