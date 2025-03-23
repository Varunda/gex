
<template>
    <div>
        <h2 class="wt-header bg-primary">
            Teams
        </h2>

        <div class="d-grid" :style="gridStyle">

            <h4 v-for="allyTeam in match.allyTeams" :key="allyTeam.allyTeamID" class="ally-team-header mb-0"
                :style="getTeamNameStyle(allyTeam)">

                Team {{ allyTeam.allyTeamID + 1 }}
            </h4>

            <template v-for="allyTeam in match.allyTeams">
                <div v-for="(player, index) in playersByTeam(allyTeam.allyTeamID)" :key="allyTeam.allyTeamID + '-' + player.teamID"
                    :style="getPlayerStyle(allyTeam, player, index)" class="player-name">

                    <a :href="'/user/' + player.userID" style="text-decoration: none;">
                        <span style="text-shadow: 1px 1px 1px #000000;">
                            <img v-if="player.faction == 'Armada'" src="/img/armada.png" height="16">
                            <img v-else-if="player.faction == 'Cortex'" src="/img/cortex.png" height="16">
                            <img v-else-if="player.faction == 'Legion'" src="/img/legion.png" height="16">
                            <span v-else>
                                ?
                            </span>
                            {{ player.username }}
                        </span>
                    </a>
                </div>

                <div v-for="(player, index) in playersByTeam(allyTeam.allyTeamID)" :key="allyTeam.allyTeamID + '-os' + player.teamID"
                    :style="getPlayerOsStyle(allyTeam, player, index)" class="player-os">

                     [<span class="font-monospace">{{ player.skill | locale(2) }}</span>]
                </div>
            </template>
        </div>

        <h4>
            Spectators ({{ match.spectators.length }})
        </h4>

        <div class="d-flex flex-wrap">
            <span v-for="spec in match.spectators" :key="spec.playerID" class="m-2">
                {{ spec.username }}
            </span>

        </div>
    </div>
</template>

<style scoped>

    .ally-team-header {
        justify-self: stretch;
        text-align: center;
        border-radius: 0.5rem;
        padding: 0.25rem;
        -webkit-text-stroke: #000 3px;
        paint-order: stroke fill;
    }

    .player-name {
        justify-self: stretch;
        text-align: end;
        margin-bottom: 0.5rem;
    }

    .player-os {
        justify-self: stretch;
        text-align: start;
        margin-bottom: 0.5rem;
    }

</style>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";

    type GroupedPlayers = {
        allyTeamID: number,
        players: BarMatchPlayer[]
    }

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
                    'grid-column': `${((allyTeam.allyTeamID * 2) % 8) + 1} / span 2`,
                    'grid-row': this.getTeamNameRow(allyTeam),
                    'background-color': this.allyTeamColor(allyTeam.allyTeamID)
                };
            },

            getPlayerStyle(allyTeam: BarMatchAllyTeam, player: BarMatchPlayer, index: number) {
                return {
                    "grid-column": ((player.allyTeamID * 2) % 8) + 1,
                    "grid-row": this.getPlayerRow(allyTeam, player, index),
                };
            },

            getPlayerOsStyle(allyTeam: BarMatchAllyTeam, player: BarMatchPlayer, index: number) {
                return {
                    "grid-column": ((player.allyTeamID * 2) % 8) + 2,
                    "grid-row": this.getPlayerRow(allyTeam, player, index),
                };
            },

            getTeamNameRow(allyTeam: BarMatchAllyTeam): number {
                return (Math.floor(allyTeam.allyTeamID / 4) * 4) + 2;
            },

            getPlayerRow(allyTeam: BarMatchAllyTeam, player: BarMatchPlayer, index: number): number {
                const v = (Math.floor(allyTeam.allyTeamID / 4) * 4) + 1 + Math.floor(index % 8) + 2;
                //console.log(`MatchTeams> player ${player.username} on all ${allyTeam.allyTeamID} at index ${index} goes to ${v}`);
                return v;
            }
        },

        computed: {
            playersByAllyTeam: function(): GroupedPlayers[] {
                return this.match.allyTeams.map(iter => {
                    return {
                        allyTeamID: iter.allyTeamID,
                        players: this.match.players.filter(p => p.allyTeamID == iter.allyTeamID)
                    }
                });
            },

            gridStyle: function() {
                // show at most 8 teams per row
                const count: number = Math.min(8, this.match.allyTeams.length * 2);
                return {
                    "grid-template-columns": `repeat(${count}, 1fr)`,
                    "justify-items": "center",
                    "column-gap": "0.5rem"
                };
            },
        },

        components: {

        }
    });
    export default MatchTeams;

</script>