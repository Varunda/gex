<template>
    <div style="max-width: 100vw">
        <div class="d-flex align-items-center">
            <gex-menu class="flex-grow-1"></gex-menu>
        </div>

        <hr class="border" />

        <div>
            <div v-if="recent.state == 'idle'"></div>

            <div v-else-if="recent.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="recent.state == 'loaded'" class="d-flex flex-wrap justify-content-center">
                <div v-for="match in recent.data" :key="match.id" style="width: 18rem; height: 18rem;" class="me-3 mb-3">
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

                        <div class="d-grid text-center p-2 tile-teams">
                            <template v-if="isFFA(match) == false">
                                <div style="grid-column: 1; grid-row: 1; text-align: end;" class="tile-team-title">
                                    Team 1
                                    <span class="dot blue">&nbsp;</span>
                                </div>

                                <div style="grid-column: 2; grid-row: 1; text-align: start" class="tile-team-title">
                                    <span class="dot red">&nbsp;</span>
                                    Team 2
                                </div>
                            </template>

                            <template v-for="allyTeam in match.allyTeams">
                                <div v-for="(player, index) in getMatchAllyPlayers(match, allyTeam.allyTeamID)" :key="allyTeam.allyTeamID + '-' + player.teamID"
                                    :style=" {
                                        'grid-column': ((allyTeam.allyTeamID + 1) % 2) + 1,
                                        'grid-row': index + (Math.floor((allyTeam.allyTeamID + 0) / 2)) + 2,
                                        'text-align': (((allyTeam.allyTeamID) % 2 == 0) ? 'start' : 'end'),
                                        'text-shadow': '1px 1px 1px #000000'
                                    }">

                                    {{ player.username }}
                                </div>
                            </template>

                            <div class="tile-ranked" :style="{ 'background-color': match.gameSettings.ranked_game == '1' ? '#800080' : '#ffa500'}">
                                {{ match.gameSettings.ranked_game == "1" ? "Ranked" : "Unranked" }}
                            </div>
                        </div>
                    </a>
                </div>
            </div>
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
        grid-template-columns: 1fr 1fr;
        column-gap: 1rem;
        row-gap: 0.25rem;
        text-align: center;
        background-color: #00000066;
    }

    .tile-team-title {
        font-size: 1.1rem;
        font-weight: bold;
    }

    .dot {
        display: inline-block;
        width: 1em;
        height: 1em;
        border-radius: 1em;
        position: relative;
    }

    .dot.blue {
        background-color: rgb(11, 62, 243);
    }

    .dot.red {
        background-color: rgb(255, 16, 5);
    }

</style>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import { GexMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchApi } from "api/BarMatchApi";

    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import "filters/MomentFilter";

    export const Mainpage = Vue.extend({
        props: {

        },

        created: function(): void {
            document.title = "Gex";
        },

        beforeMount: function(): void {
            this.loadRecent();
        },

        data: function() {
            return {
                recent: Loadable.idle() as Loading<BarMatch[]>
            }
        },

        methods: {
            loadRecent: async function(): Promise<void> {
                this.recent = Loadable.loading();
                this.recent = await BarMatchApi.getRecent();
            },

            getMapThumbnail: function(map: string): string {
                return `/image-proxy/MapBackground?mapName=${map.replace(/ /g, "%20")}&size=texture-thumb`;
            },

            isFFA: function(match: BarMatch): boolean {
                return match.allyTeams.length > 2 && Math.max(...match.allyTeams.map(iter => iter.playerCount)) == 1;
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
                };
            }
        },

        components: {
            InfoHover, GexMenu,
        }
    });

    export default Mainpage;
</script>