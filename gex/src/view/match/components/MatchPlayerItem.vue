<template>
    <fragment>
        <a :href="'/user/' + player.userID" style="text-decoration: none" class="name">
            <span style="text-shadow: 1px 1px 1px #000000" :style="{ color: player.hexColor }">
                <img v-if="player.faction == 'Armada'" src="/img/armada.png" height="16" title="icon for armada" />
                <img v-else-if="player.faction == 'Cortex'" src="/img/cortex.png" height="16" title="icon for cortex" />
                <img v-else-if="player.faction == 'Legion'" src="/img/legion.png" height="16" title="icon for legion" />
                <img v-else-if="player.faction == 'Random'" src="/img/random.png" height="16" title="icon for random" />
                <span v-else> ? </span>
                {{ player.username }}
            </span>
        </a>
        <span class="os">
            [<span class="font-monospace">{{ player.skill | locale(2) }}</span>]
            <span v-if="player.handicap != 0" class="handicap">
                <span v-if="player.handicap > 0" style="color: var(--bg-green)">
                    (+{{ player.handicap }}%)
                </span>
                <span v-else> ({{ player.handicap }}%) </span>
            </span>
        </span>
    </fragment>
</template>

<style scoped>
    .name {
        text-align: right;
    }

    .os {
        text-align: left;
        font-size: 0.9rem;
    }
</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { Fragment } from "vue-fragment";

    export const MatchPlayerItem = Vue.extend({
        props: {
            player: { type: Object as PropType<BarMatchPlayer>, required: true }
        },

        components: {
            Fragment,
        }
    })

    export default MatchPlayerItem;
</script>