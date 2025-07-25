
<template>
    <div class="d-flex flex-wrap justify-content-center" style="font-size: 14px; line-height: 1;">
        <div v-for="match in matches" :key="match.id" class="me-3 mb-3">
            <div>
                <a :href="'/match/' + match.id" :style="getMatchStyle(match)" class="tile">
                    <div class="position-absolute tile-overlay"></div>

                    <h5 class="tile-title">
                        {{ mapNameWithoutVersion(match.map) }}
                    </h5>

                    <div class="flex-grow-1 align-content-center w-100 mh-100" style="z-index: 10;">
                        <div class="d-flex text-center p-2 tile-teams flex-wrap" style="max-height: 80%; overflow-y: auto;">
                            <div v-for="allyTeam in matchAllyTeams(match)" :key="allyTeam.allyTeamID" class="tile-team-parent">

                                <div class="tile-team"
                                    :style="getTeamPanelStyle(match, allyTeam)">

                                    <div v-for="player in getMatchAllyPlayers(match, allyTeam.allyTeamID)" :key="allyTeam.allyTeamID + '-' + player.teamID" :title="player.username"
                                        :style="{
                                            'text-shadow': '1px 1px 1px #000000',
                                            'text-align': matchAllyTeams(match).length == 2 ? allyTeam.allyTeamID % 2 == 0 ? 'end' : 'start' : 'auto',
                                            'overflow': 'clip',
                                            'text-overflow': 'ellipsis',
                                            'margin': '0.25rem 0'
                                        }">

                                        {{ player.username }}
                                    </div>
                                </div>
                            </div>

                            <div v-if="match.allyTeams.length == 2" style="position: absolute; font-size: 0.75rem;" class="text-center">
                                VS
                            </div>
                        </div>
                    </div>
                </a>
            </div>

            <div class="tile-time-ago">
                <span v-if="(new Date().getTime()) - match.endTime.getTime() > (1000 * 60 * 60 * 12)">
                    Ended at {{ match.endTime | moment("yyyy-MM-dd hh:mma ZZZZ") }}
                </span>

                <span v-else :title="match.endTime | moment('yyyy-MM-dd hh:mm:ssa ZZZZ')">
                    Ended {{ match.endTime | compactTimeAgo }} ago
                    &middot;
                    <span>
                        {{ match.endTime | moment("hh:mma")}}
                    </span>
                </span>

                <span v-if="match.processing == null || match.processing.actionsParsed == null" class="bi bi-cone text-warning"
                    title="This game has not been fully processed!"></span>
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
        flex-direction: column;
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
        /*
        position: absolute;
        top: 0;
        */
        flex-grow: 0;
        background-color: #00000088;
        padding: 0.5rem 0.5rem;
        margin-bottom: 0;
        border-start-start-radius: 0.75rem;
        border-start-end-radius: 0.75rem;
        max-width: 100%;
        width: 100%;
        text-align: center;
        overflow: hidden;
        text-wrap: nowrap;
        text-overflow: clip;
        z-index: 10;
    }

    .tile-overlay {
        width: 18rem;
        height: 18rem;
        background-color: #0005;
        z-index: 1;
        border-radius: 0.75rem;
    }

    .tile-overlay:hover {
        background-color: #000F;
        transition: background-color 0.2s ease-in;
    }

    .tile-time-ago {
        font-size: 0.83rem;
        padding: 0.2rem 0.5rem;
        text-align: center;
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
        padding: 0.2rem 0.5rem;
        margin-top: 0;
        border-start-end-radius: 0.75rem;
    }

    .tile-top-right {
        position: absolute;
        font-weight: bold;
        font-size: 1rem;
        top: 0;
        right: 0;
    }

    .tile-processing {
        background-color: #000000AA;
        border-end-start-radius: 0.75rem;
    }

    .tile-teams {
        text-align: center;
        justify-content: center;
        /* 
        gap: 0.75rem;
        */
        gap: 1.25rem;
        z-index: 10;
        flex-grow: 1;
        align-items: center;
    }

    .tile-team {
        background-color: #00000066;
        padding: 0.5rem;
        border-radius: 0.25rem;
        min-width: 47%;
    }

    .tile-team-parent {
        max-width: 43%;
        width: 43%;
        background-color: #00000077;
        border-radius: 0.25rem;
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

    import "filters/TimeAgoFilter";
    import "filters/CompactTimeAgoFilter";

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

            matchAllyTeams: function(match: BarMatch): BarMatchAllyTeam[] {
                return match.allyTeams.filter(iter => iter.playerCount > 0);
            },

            mapNameWithoutVersion: function(name: string): string {
                name = name.replace(/ /g, " ");
                const m = name.match(/^([a-zA-Z\-_\d\s]*)[vV_\s][\d\.]*/);
                if (m == null) {
                    return name;
                }
                if (m.length < 2) {
                    return name;
                }
                return m[1].replace(/_/g, " ");
            },

            getTeamPanelStyle: function(match: BarMatch, allyTeam: BarMatchAllyTeam) {

                const allyTeamCount: number = this.matchAllyTeams(match).length;

                let background: string = 'linear-gradient(' + (allyTeam.allyTeamID % 2 == 0 ? '90deg' : '270deg') + ', #00000000 0%, ' + this.getAllyTeamColor(match, allyTeam) + '66 100%)';
                if (allyTeamCount != 2) {
                    background = `${this.getAllyTeamColor(match, allyTeam)}66`;
                }

                const border: string = this.getAllyTeamColor(match, allyTeam) + " 1px solid";

                const style: any = {
                    "background": background
                };

                if (allyTeamCount != 2) {
                    style["border"] = border;
                } else {
                    if (allyTeam.allyTeamID % 2 == 0) {
                        style["border-right"] = border;
                    } else {
                        style["border-left"] = border;
                    }
                }

                return style;
            },

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
