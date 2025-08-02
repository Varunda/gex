
<template>
    <div>
        <collapsible header-text="Teams" size-class="h1" bg-color="bg-light">
            <div v-if="isFunkyTeams == false">
                <div class="teams">
                    <div class="team" v-for="allyTeam in allyTeamsSorted" :key="allyTeam.allyTeamID">
                        <div>
                            <h4 class="ally-team-header mb-0" :style="getTeamNameStyle(allyTeam)">
                                Team {{ allyTeam.allyTeamID + 1 }}

                                <span v-if="showWinner == true && IsFfa && match.teamDeaths.length > 0">
                                    - {{ teamPlacement(allyTeam.allyTeamID) }}
                                </span>

                                <span v-else-if="showWinner == true && allyTeam.won" class="bi bi-trophy-fill text-warning"
                                    title="This team won the match!">
                                </span>
                            </h4>
                        </div>
                        
                        <div class="players">
                            <template v-for="(player, index) in playersByTeam(allyTeam.allyTeamID)"> 
                                <match-player-item :player="player" :key="index" />
                            </template>
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
                                <span v-if="showWinner == true && allyTeam.won" class="bi bi-trophy-fill text-warning"
                                    title="This team won the match!">
                                </span>
                            </th>
                        </tr>
                    </thead>

                    <tbody>
                        <tr v-for="index in maxTeamSize" :key="index">
                            <td v-for="allyTeam in match.allyTeams" :key="allyTeam.allyTeamID">
                                <match-player-item v-if="index <= allyTeam.playerCount+1" :player="playersByTeam(allyTeam.allyTeamID)[index - 1]" />
                                <span v-else>
                                    <!-- uneven team placeholder -->
                                </span>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <toggle-button class="btn-small" v-model="showWinner">
                Show winner
            </toggle-button>

            <div v-if="match.spectators.length > 0" class="mt-2">
                <collapsible header-text="Spectators" size-class="h5" :show="false">
                    <div class="d-flex flex-wrap">
                        <span v-for="spec in match.spectators" :key="spec.playerID" class="m-2">
                            <a :href="'/user/' + spec.userID">
                                {{ spec.username }}
                            </a>
                        </span>
                    </div>
                </collapsible>
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

    .players {
        display: grid;
        grid-template-columns: 1fr 1fr;
        grid-template-rows: 1fr;
        white-space: nowrap;
        column-gap: 0.5rem;
        row-gap: 0.25rem;
    }

    @media only screen and (max-width: 800px) {
        .team {
            flex: 1 2 50%;
        }
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
    import ToggleButton from "components/ToggleButton";

    import MatchPlayerItem from "./MatchPlayerItem.vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";

    type GroupedPlayers = {
        allyTeamID: number,
        players: BarMatchPlayer[]
    }

    export const MatchTeams = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            IsFfa: { type: Boolean, required: true }
        },

        data: function() {
            return {
                showWinner: false as boolean

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

            teamPlacement: function(teamID: number): string {
                const index = this.match.teamDeaths.findIndex(iter => iter.teamID == teamID) + 2;
                if (index == 1) {
                    return "1st";
                } else if (index == 2) {
                    return "2nd";
                } else if (index == 3) {
                    return "3rd";
                } else {
                    return `${index}th`;
                }
            }
        },

        computed: {

            allyTeamsSorted: function(): BarMatchAllyTeam[] {
                if (this.showWinner == true && this.IsFfa && this.match.teamDeaths.length > 0) {
                    const teamDeaths = [...this.match.teamDeaths].sort((a, b) => b.gameTime - a.gameTime);
                    return [...this.match.allyTeams].sort((a, b) => {
                        return teamDeaths.findIndex(i => i.teamID == a.allyTeamID) - teamDeaths.findIndex(i => i.teamID == b.allyTeamID);
                    });
                }

                return this.match.allyTeams;
            },

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
            MatchPlayerItem, ToggleButton
        }
    });
    export default MatchTeams;

</script>