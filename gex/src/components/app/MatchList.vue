<template>
    <div class="d-flex flex-wrap justify-content-center" style="font-size: 14px; line-height: 1">
        <div v-for="match in matches" :key="match.id" class="me-3 mb-3">
            <div>
                <a :href="'/match/' + match.id" :style="getMatchStyle(match)" class="tile">
                    <div class="position-absolute" style="width: 18rem; height: 18rem; background-color: #0005; z-index: 1; border-radius: 0.75rem"></div>

                    <h5 class="tile-title">
                        {{ mapNameWithoutVersion(match.map) }}
                    </h5>

                    <div class="flex-grow-1 align-content-center w-100" style="z-index: 10">
                        <div class="d-flex text-center p-2 tile-teams flex-wrap" style="max-height: 80%; overflow-y: auto">
                            <div
                                v-for="allyTeam in match.allyTeams"
                                :key="allyTeam.allyTeamID"
                                style="min-width: 47%; max-width: 47%; background-color: #00000077; border-radius: 0.25rem"
                            >
                                <div
                                    class="tile-team"
                                    :style="{
                                        background:
                                            'linear-gradient(' +
                                            (allyTeam.allyTeamID % 2 == 0 ? '90deg' : '270deg') +
                                            ', #00000000 0%, ' +
                                            getAllyTeamColor(match, allyTeam) +
                                            '66 100%)',
                                        'border-right': allyTeam.allyTeamID % 2 == 1 ? 'unset' : getAllyTeamColor(match, allyTeam) + ' 1px solid',
                                        'border-left': allyTeam.allyTeamID % 2 == 0 ? 'unset' : getAllyTeamColor(match, allyTeam) + ' 1px solid',
                                    }"
                                >
                                    <div
                                        v-for="player in getMatchAllyPlayers(match, allyTeam.allyTeamID)"
                                        :key="allyTeam.allyTeamID + '-' + player.teamID"
                                        :title="player.username"
                                        :style="{
                                            'text-shadow': '1px 1px 1px #000000',
                                            'text-align': match.allyTeams.length == 2 ? (allyTeam.allyTeamID % 2 == 0 ? 'end' : 'start') : 'auto',
                                            overflow: 'clip',
                                            'text-overflow': 'ellipsis',
                                            margin: '0.25rem 0',
                                        }"
                                    >
                                        {{ player.username }}
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </a>
            </div>

            <div class="tile-time-ago">
                Ended {{ match.endTime | compactTimeAgo }} ago &middot;
                <span :title="match.endTime | moment('YYYY-MM-DD hh:mm:ss A')">
                    {{ match.endTime | moment("hh:mm A") }}
                </span>

                <span
                    v-if="match.processing == null || match.processing.actionsParsed == null"
                    class="bi bi-cone text-warning"
                    title="This game has not been fully processed!"
                >
                </span>
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
        background-color: #000000aa;
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
        background-color: #000000aa;
        border-end-start-radius: 0.75rem;
    }

    .tile-teams {
        text-align: center;
        justify-content: center;
        gap: 0.75rem;
        z-index: 10;
        flex-grow: 1;
    }

    .tile-team {
        background-color: #00000066;
        padding: 0.5rem;
        border-radius: 0.25rem;
        min-width: 47%;
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
            matches: { type: Array as PropType<BarMatch[]>, required: true },
        },

        data: function () {
            return {};
        },

        methods: {
            mapNameWithoutVersion: function (name: string): string {
                const m = name.match(/^([a-zA-Z\s]*)[vV_\s][\d\.]*/);
                if (m == null) {
                    return name;
                }
                if (m.length < 2) {
                    return name;
                }
                return m[1];
            },

            getMapThumbnail: function (map: string): string {
                return `/image-proxy/MapBackground?mapName=${map.replace(/ /g, "%20")}&size=texture-thumb`;
            },

            isFFA: function (match: BarMatch): boolean {
                return match.allyTeams.length > 2 && Math.max(...match.allyTeams.map((iter) => iter.playerCount)) == 1;
            },

            getAllyTeamColor: function (match: BarMatch, allyTeam: BarMatchAllyTeam): string {
                return match.players.find((iter) => iter.allyTeamID == allyTeam.allyTeamID)?.hexColor ?? `#333333`;
            },

            getMatchMaxPlayers: function (match: BarMatch): number {
                return Math.max(...match.allyTeams.map((iter) => iter.playerCount));
            },

            getMatchAllyPlayers: function (match: BarMatch, allyTeamID: number): BarMatchPlayer[] {
                return match.players.filter((iter) => iter.allyTeamID == allyTeamID);
            },

            getMatchStyle: function (match: BarMatch) {
                return {
                    "background-image": `url(${this.getMapThumbnail(match.mapName)})`,
                };
            },
        },
    });
    export default MatchList;
</script>
