
<template>
    <div>
        <collapsible header-text="Teams" size-class="h1" bg-color="bg-light">
            <div v-if="isFunkyTeams == false">
                <div class="teams">
                    <div class="team" v-for="allyTeam in match.allyTeams" :key="allyTeam.allyTeamID">
                        <div>
                            <h4 class="ally-team-header mb-0" :style="getTeamNameStyle(allyTeam)">
                                Team {{ allyTeam.allyTeamID + 1 }}
                            </h4>
                        </div>

                        <div class="player" v-for="(player, index) in playersByTeam(allyTeam.allyTeamID)" :key="index"> 
                            <player-item :player="player" />
                        </div>
                    </div>
                </div>
            </div>
            <div v-else>
                <small class="text-muted">These are not usual team size! Gex is not programmed to handle teams of this size, so a fallback is being used instead</small>

                <table class="table table-sm" style="table-layout: fixed">
                    <thead>
                        <tr>
                            <th v-for="allyTeam in match.allyTeams" :key="allyTeam.allyTeamID" :style="{ 'background-color': allyTeamColor(allyTeam.allyTeamID) }">
                                Team {{ allyTeam.allyTeamID + 1 }}
                            </th>
                        </tr>
                    </thead>

                    <tbody>
                        <tr v-for="index in maxTeamSize" :key="index">
                            <td v-for="allyTeam in match.allyTeams" :key="allyTeam.allyTeamID">
                                <player-item v-if="index <= allyTeam.playerCount+1" :player="playersByTeam(allyTeam.allyTeamID)[index - 1]" />
                                <span v-else>
                                    <!-- uneven team placeholder -->
                                </span>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
            <div v-if="match.spectators.length > 0" class="mt-2">
                <h5>
                    Spectators ({{ match.spectators.length }})
                </h5>

                <div class="d-flex flex-wrap">
                    <span v-for="spec in match.spectators" :key="spec.playerID" class="m-2">
                        <a :href="'/user/' + spec.userID">
                            {{ spec.username }}
                        </a>
                    </span>
                </div>
            </div>
        </collapsible>
    </div>
</template>

<style scoped>

    .teams {
        display: flex;
        justify-content: space-between;
        flex-wrap: wrap;
        row-gap: 0.5rem;
    }

    .team-player {
        display: flex;
        align-items: center;
    }

    .team {
        flex: 1 4 25%;
        border-spacing: 0.5rem;
        border-collapse: separate;
        text-align: center;
    }

    .team h4 {
        display: inline-block;
    }

    @media only screen and (max-width: 800px) {
        .team {
            flex: 1 2 50%;
        }
    }

    .player {
       white-space: nowrap;
    }

    .player--name {
        margin-right: 0.35rem;
    }

    .player--os, .player--handicap {
        font-size: 0.9rem;
    }

    .ally-team-header {
        text-align: center;
        border-radius: 0.5rem;
        padding: 0.25rem 2rem;
        -webkit-text-stroke: #000 3px;
        paint-order: stroke fill;
        flex: 0;
    }


</style>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import Collapsible from "components/Collapsible.vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";

    type GroupedPlayers = {
        allyTeamID: number,
        players: BarMatchPlayer[]
    }

    const PlayerItem = Vue.extend({
        props: {
            player: { type: Object as PropType<BarMatchPlayer>, required: true }
        },

        template: `
            <span>
                <a :href="'/user/' + player.userID" style="text-decoration: none;">
                    <span style="text-shadow: 1px 1px 1px #000000;" :style="{ 'color': player.hexColor }">
                        <img v-if="player.faction == 'Armada'" src="/img/armada.png" height="16">
                        <img v-else-if="player.faction == 'Cortex'" src="/img/cortex.png" height="16">
                        <img v-else-if="player.faction == 'Legion'" src="/img/legion.png" height="16">
                        <span v-else>
                            ?
                        </span>
                        {{ player.username }}
                    </span>
                </a>
                <span class="player--os">[<span class="font-monospace">{{ player.skill | locale(2) }}</span>]</span>
                <span v-if="player.handicap != 0" class="player--handicap">
                    <span v-if="player.handicap > 0" style="color: var(--bg-green)">
                        (+{{ player.handicap }}%)
                    </span>
                    <span v-else>
                        ({{ player.handicap }}%)
                    </span>
                </span>
            <span>
        `
    });

    export const MatchTeams = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {

            allyTeamColor: function(allyTeamID: number): string {
                return this.match.players.find(iter => iter.allyTeamID == allyTeamID)?.hexColor ?? `#333333`;
            },

            playersByTeam: function(allyTeamID: number): BarMatchPlayer[] {
                return [...this.match.players.filter(iter => iter.allyTeamID == allyTeamID)].sort((a, b) => b.skill - a.skill);
            },

            getTeamNameStyle(allyTeam: BarMatchAllyTeam) {
                return {
                    'background-color': this.allyTeamColor(allyTeam.allyTeamID)
                };
            },
        },

        computed: {
            maxTeamSize: function(): number {
                return Math.max(...this.match.allyTeams.map(iter => iter.playerCount));
            },

            isFunkyTeams: function(): boolean {
                return this.maxTeamSize > 8;
            },

            playersByAllyTeam: function(): GroupedPlayers[] {
                return this.match.allyTeams.map(iter => {
                    return {
                        allyTeamID: iter.allyTeamID,
                        players: this.match.players.filter(p => p.allyTeamID == iter.allyTeamID)
                    }
                });
            }
        },

        components: {
            Collapsible,
            PlayerItem
        }
    });
    export default MatchTeams;

</script>