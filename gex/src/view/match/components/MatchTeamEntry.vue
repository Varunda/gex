<template>
    <fragment v-if="players.length == 1">
        <a :href="players.length == 1 ? ('/user/' + players[0].userID) : ''" style="text-decoration: none" class="name">
            <span v-if="team.startSpotLabel != null" class="role">
                ({{ team.startSpotLabel }})
            </span>

            <span style="text-shadow: 1px 1px 1px #000000" :style="{ color: team.hexColor }">
                <img v-if="team.faction == 'Armada'" src="/img/armada.png" height="16" title="icon for armada" />
                <img v-else-if="team.faction == 'Cortex'" src="/img/cortex.png" height="16" title="icon for cortex" />
                <img v-else-if="team.faction == 'Legion'" src="/img/legion.png" height="16" title="icon for legion" />
                <img v-else-if="team.faction == 'Random'" src="/img/random.png" height="16" title="icon for random" />
                <span v-else> ? </span>
                {{ team.name }}
            </span>

            <span v-if="ShowDebug">
                team id {{ team.teamID }}, player id {{ players[0].playerID }}
            </span>
        </a>

        <span class="os">
            [<span v-if="players.length > 0" class="font-monospace">{{ players[0].skill | locale(2) }}</span>]

            <span v-if="team.handicap != 0" class="handicap">
                <span v-if="team.handicap > 0" style="color: var(--bg-green)">
                    (+{{ team.handicap }}%)
                </span>
                <span v-else> ({{ team.handicap }}%) </span>
            </span>
        </span>

    </fragment>

    <fragment v-else>
        <span class="name">
            <span style="text-shadow: 1px 1px 1px #000000" :style="{ color: team.hexColor }">
                <img v-if="team.faction == 'Armada'" src="/img/armada.png" height="16" title="icon for armada" />
                <img v-else-if="team.faction == 'Cortex'" src="/img/cortex.png" height="16" title="icon for cortex" />
                <img v-else-if="team.faction == 'Legion'" src="/img/legion.png" height="16" title="icon for legion" />
                <img v-else-if="team.faction == 'Random'" src="/img/random.png" height="16" title="icon for random" />
                <span v-else> ? </span>

                Army {{ team.teamID + 1 }}
            </span>

            <span v-if="team.startSpotLabel != null" class="role">
                - {{ team.startSpotLabel }}
            </span>

            <span v-if="team.handicap != 0" class="handicap">
                <span v-if="team.handicap > 0" style="color: var(--bg-green)">
                    (+{{ team.handicap }}%)
                </span>
                <span v-else>
                    ({{ team.handicap }}%)
                </span>
            </span>

            <span v-if="ShowDebug">
                team ID {{ team.teamID }}
            </span>
        </span>

        <span class="os d-grid" style="grid-template-columns: max-content min-content; gap: 0.25rem">
            <template v-for="player in players">
                <a :href="'/user/' + player.userID" style="text-decoration: none;" target="_blank" ref="nofollow">
                    {{ player.username }}
                    <span v-if="ShowDebug">
                        player ID {{ player.playerID }}, team ID {{ player.teamID }}
                    </span>
                </a>
                <span class="text-align-end">
                    [<span class="font-monospace">{{ player.skill | locale(2) }}</span>]
                </span>
            </template>
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

    .role {

    }
</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Fragment } from "vue-fragment";

    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchTeam } from "model/BarMatchTeam";

    export const MatchTeamEntry = Vue.extend({
        props: {
            players: { type: Array as PropType<BarMatchPlayer[]>, required: true },
            team: { type: Object as PropType<BarMatchTeam>, required: true },
            ShowDebug: { type: Boolean, required: false }
        },

        components: {
            Fragment,
        }
    })

    export default MatchTeamEntry;
</script>