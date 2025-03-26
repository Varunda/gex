
<template>
    <div class="d-flex flex-wrap justify-content-center" style="font-size: 14px; line-height: 1;">
        <div v-for="match in matches" :key="match.id" style="width: 18rem; height: 18rem;" class="me-3 mb-3">
            <a :href="'/match/' + match.id" :style="getMatchStyle(match)" class="tile">
                <h5 class="tile-title">{{ match.map }}</h5>
                <h5 class="tile-versus">
                    <span v-if="isFFA(match)">
                        {{ match.allyTeams.length }}-way FFA
                    </span>
                    <span v-else>
                        {{ match.allyTeams.map(iter => iter.playerCount).join(" v ") }}
                    </span>
                </h5>

                <div class="d-flex text-center p-2 tile-teams flex-wrap">
                    <div v-for="allyTeam in match.allyTeams" :key="allyTeam.allyTeamID" class="tile-team">
                        <div v-if="isFFA(match) == false" class="tile-team-title"
                            :style="{
                                'text-shadow': '1px 1px 1px #000000',
                                'text-align': match.allyTeams.length == 2 ? allyTeam.allyTeamID % 2 == 0 ? 'end' : 'start' : 'auto',
                            }">

                            <span>
                                Team {{ allyTeam.allyTeamID + 1 }}
                            </span>

                            <span class="dot" :style="{
                                'background-color': getAllyTeamColor(match, allyTeam)
                            }">
                                &nbsp;
                            </span>
                        </div>

                        <div v-for="player in getMatchAllyPlayers(match, allyTeam.allyTeamID)" :key="allyTeam.allyTeamID + '-' + player.teamID" :title="player.username"
                            :style="{
                                'text-shadow': '1px 1px 1px #000000',
                                'text-align': match.allyTeams.length == 2 ? allyTeam.allyTeamID % 2 == 0 ? 'end' : 'start' : 'auto',
                                'overflow': 'clip',
                                'text-overflow': 'ellipsis'
                            }">

                            {{ player.username }}
                        </div>
                    </div>

                    <div class="tile-ranked" :style="{ 'background-color': match.gameSettings.ranked_game == '1' ? '#800080' : '#ffa500'}">
                        {{ match.gameSettings.ranked_game == "1" ? "Ranked" : "Unranked" }}
                    </div>
                </div>
            </a>
        </div>
    </div>
</template>

<style scoped>

    .tile {
        width: 18rem;
        height: 18rem;
        display: flex;
        position: relative;
        background-position: center center;
        background-size: 100%;
        transition: background-size 0.2s;
        color: #efefef;
        text-decoration: none;
        justify-content: center;
        align-items: center;
        border-radius: 0.75rem;
        text-shadow: 1px 1px 1px #000000;
    }

    .tile:hover {
        outline: 3px solid #ffffff;
        background-size: 110%;
        transition: background-size 0.2s ease-in;
    }

    .tile-title {
        position: absolute;
        font-weight: bold;
        bottom: 0;
        left: 0;
        background-color: #000000AA;
        padding: 0.2rem 0.5rem;
        margin-bottom: 0;
        border-end-start-radius: 0.75rem;
        border-start-end-radius: 0.75rem;
        max-width: 100%;
        overflow: hidden;
        text-wrap: nowrap;
        text-overflow: clip;
    }

    .tile-versus {
        position: absolute;
        font-weight: bold;
        top: 0;
        left: 0;
        background-color: #000000AA;
        padding: 0.2rem 0.5rem;
        margin-top: 0;
        border-start-start-radius: 0.75rem;
        border-end-end-radius: 0.75rem;
    }

    .tile-ranked {
        position: absolute;
        font-weight: bold;
        font-size: 1rem;
        top: 0;
        right: 0;
        padding: 0.2rem 0.5rem;
        margin-top: 0;
        border-end-start-radius: 0.75rem;
        border-start-end-radius: 0.75rem;
    }

    .tile-teams {
        text-align: center;
        justify-content: center;
        gap: 0.5rem;
    }

    .tile-team {
        background-color: #00000066;
        padding: 0.5rem;
        border-radius: 0.25rem;
        max-width: 48%;
    }

    .tile-team-title {
        font-size: 1.1rem;
        font-weight: bold;
        margin-bottom: 0.25rem;
        width: max-content;
    }

    .dot {
        display: inline-block;
        width: 1em;
        height: 1em;
        border-radius: 1em;
        position: relative;
        margin-left: 0.25rem;
    }

</style>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";

    export const MatchList = Vue.extend({
        props: {
            matches: { type: Array as PropType<BarMatch[]>, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {
            getMapThumbnail: function(map: string): string {
                return `/image-proxy/MapBackground?mapName=${map.replace(/ /g, "%20")}&size=texture-thumb`;
            },

            isFFA: function(match: BarMatch): boolean {
                return match.allyTeams.length > 2 && Math.max(...match.allyTeams.map(iter => iter.playerCount)) == 1;
            },

            getAllyTeamColor: function(match: BarMatch, allyTeam: BarMatchAllyTeam): string {
                return match.players.find(iter => iter.allyTeamID == allyTeam.allyTeamID)?.hexColor ?? `#333333`;
            },

            getMatchMaxPlayers: function(match: BarMatch): number {
                return Math.max(...match.allyTeams.map(iter => iter.playerCount));
            },

            getMatchAllyPlayers: function(match: BarMatch, allyTeamID: number): BarMatchPlayer[] {
                return match.players.filter(iter => iter.allyTeamID == allyTeamID);
            },

            getMatchStyle: function(match: BarMatch) {
                return {
                    "background-image": `url(${this.getMapThumbnail(match.mapName)})`
                }
            }
        }
    });
    export default MatchList;
</script>
