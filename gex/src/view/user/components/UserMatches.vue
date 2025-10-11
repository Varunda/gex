<template>
    <div>
        <toggle-button v-model="showOutcome" class="mb-2">
            Show outcome
        </toggle-button>

        <a-table :entries="matches"
            :show-filters="true"
            :default-page-size="10" :overflow-wrap="true"
            default-sort-field="startTime" default-sort-order="desc">

            <a-col sort-field="startTime">
                <a-header>
                    <b>Timestamp</b>
                </a-header>

                <a-body v-slot="entry">
                    <a :href="'/match/' + entry.id">
                        {{ entry.startTime | moment }}
                    </a>
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
                    <span v-if="showOutcome" :class="'outcome-' + outcome(entry)">
                        {{ outcome(entry) }}
                    </span>
                    <span v-else @click="showOutcome = true" title="Outcome hidden, click here to show, or click the 'Show outcome' button">
                        ??
                    </span>
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Avg. Rating</b>
                    <info-hover text="Average rating of all the players in the lobby, and the difference between this user's rating and the average"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    <user-match-rating-cell :user-id="UserId" :match="entry"></user-match-rating-cell>
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
        color: var(--bs-success-text-emphasis);
    }

    .outcome-Tie {
        color: var(--bs-blue);
    }

    .outcome-Lost {
        color: var(--bs-danger-text-emphasis);
    }
</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import "filters/BarGamemodeFilter";

    import LocaleUtil from "util/Locale";
    import { GamemodeUtil } from "util/Gamemode";

    const UserMatchRatingCell = Vue.extend({
        props: {
            UserId: { type: Number, required: true },
            match: { type: Object as PropType<BarMatch>, required: true }
        },

        computed: {
            avgSkill: function(): number {
                const totalSkill: number = this.match.players.reduce((acc, iter) => acc += iter.skill, 0);
                const avgSkill: number = totalSkill / this.match.players.length;

                return avgSkill;
            },

            userSkill: function(): number {
                const player: BarMatchPlayer | undefined = this.match.players.find(iter => iter.userID == this.UserId);
                return player?.skill ?? 0;
            },

            skillDiff: function(): number {
                return Math.abs(this.avgSkill - this.userSkill);
            },

            titleText: function(): string {
                return `User's skill was ${LocaleUtil.locale(this.skillDiff, 2)} ${this.avgSkill > this.userSkill ? "below" : "above"} the average skill in the lobby`;
            },

            cssStyle: function(): object {
                return {
                    "color": ((this.userSkill > this.avgSkill) ? "var(--bs-warning-text-emphasis)" : "var(--bs-info-text-emphasis)") + " !important",
                    //"text-decoration": "underline"
                };
            }
        },

        template: `
            <span class="font-monospace">
                {{ avgSkill | locale(2) }}

                <small class="text-muted" :title="titleText" :style="cssStyle">
                    <span v-if="avgSkill > userSkill">-</span><span v-else>+</span>{{ Math.abs(avgSkill - userSkill) | locale(2) }}
                </small>
            </span>
        `
    });

    export const UserMatches = Vue.extend({
        props: {
            data: { type: Array as PropType<BarMatch[]>, required: true },
            UserId: { type: Number, required: true }
        },

        data: function() {
            return {
                showOutcome: false as boolean
            }
        },

        created: function(): void {

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
            UserMatchRatingCell,
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            InfoHover, ToggleButton 
        }
    });
    export default UserMatches;
</script>