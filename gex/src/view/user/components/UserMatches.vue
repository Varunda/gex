<template>
    <div>
        <a-table :entries="matches"
            :show-filters="true"
            :default-page-size="10"
            default-sort-field="startTime" default-sort-order="desc">

            <a-col sort-field="startTime">
                <a-header>
                    <b>Timestamp</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.startTime | moment }}
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Map</b>
                </a-header>

                <a-filter field="map" type="string" method="input"
                    :conditions="[ 'contains', 'equals' ]">
                </a-filter>

                <a-body v-slot="entry">
                    {{ entry.map }}
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Teams</b>
                </a-header>

                <a-body v-slot="entry">
                    <span v-if="isFFA(entry)">
                        {{ entry.allyTeams.length }} way FFA
                    </span>
                    <span v-else>
                        {{ entry.allyTeams.map(iter => iter.playerCount).join(" v ") }}
                    </span>
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Gamemode</b>
                </a-header>

                <a-filter field="gamemode" type="number" method="dropdown" :source="source.gamemode"
                    :conditions="[ 'equals' ]">
                </a-filter>

                <a-body v-slot="entry">
                    {{ entry.gamemode | gamemode }}
                </a-body>
            </a-col>

            <a-col sort-field="durationMs">
                <a-header>
                    <b>Duration</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.durationMs / 1000 | mduration }}
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Outcome</b>
                </a-header>

                <a-body v-slot="entry">
                    <span :class="'outcome-' + outcome(entry)">
                        {{ outcome(entry) }}
                    </span>
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Link</b>
                </a-header>

                <a-body v-slot="entry">
                    <a :href="'/match/' + entry.id">
                        View
                    </a>
                </a-body>
            </a-col>
        </a-table>
    </div>
    
</template>

<style scoped>

    .outcome-Won {
        color: var(--bs-green);
    }

    .outcome-Tie {
        color: var(--bs-blue);
    }

    .outcome-Lost {
        color: var(--bs-red);
    }

</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import { GamemodeUtil } from "util/Gamemode";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";

    import "filters/BarGamemodeFilter";

    export const UserMatches = Vue.extend({
        props: {
            data: { type: Array as PropType<BarMatch[]>, required: true },
            UserId: { type: Number, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {
            isFFA: function(match: BarMatch): boolean {
                return match.allyTeams.length > 2 && Math.max(...match.allyTeams.map(iter => iter.playerCount)) == 1;
            },

            outcome: function(match: BarMatch): string {
                const winner: BarMatchAllyTeam[] = match.allyTeams.filter(iter => iter.won == true);
                if (winner.length == 0) {
                    return "Tie";
                }

                const player = match.players.find(iter => iter.userID == this.UserId);
                if (player == undefined) {
                    console.error(`UserMatches> user was not a player? ${this.UserId}`);
                    return "Unknown";
                }

                if (winner.find(iter => iter.allyTeamID == player.allyTeamID) != undefined) {
                    return "Won";
                }

                return "Lost";
            }
        },

        computed: {
            matches: function(): Loading<BarMatch[]> {
                return Loadable.loaded(this.data);
            },

            source: function() {
                return {
                    gamemode: [
                        { key: "All", value: null },
                        { key: "Duel", value: GamemodeUtil.DUEL },
                        { key: "Small team", value: GamemodeUtil.SMALL_TEAM },
                        { key: "Large team", value: GamemodeUtil.LARGE_TEAM },
                        { key: "FFA", value: GamemodeUtil.FFA },
                        { key: "Team FFA", value: GamemodeUtil.TEAM_FFA }
                    ]
                }
            }


        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            InfoHover, ToggleButton 
        }
    });
    export default UserMatches;
</script>