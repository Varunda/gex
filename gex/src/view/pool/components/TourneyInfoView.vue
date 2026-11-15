<template>
    <div>

        <h2>
            {{ TournamentInfo.name }}
        </h2>

        <a :href="'https://apm.bar/tournament/' + TournamentInfo.slug" target="_blank" ref="nofollow" class="btn btn-link border rounded">
            <img src="/img/apm_icon.svg" height="18" width="18" class="me-1">{{ TournamentInfo.name }}
        </a>

        <div>
            <h3>
                Teams ({{ TournamentInfo.teams.length }})
            </h3>

            <div v-for="team in TournamentInfo.teams" :key="team.id" class="mb-3">
                <h4 class="mb-0">
                    {{ team.name }}
                </h4>

                <div>
                Members: 
                    <span v-for="member in team.members" :key="member.id">
                        {{ member.name }}
                    </span>
                </div>
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import TourneyInfoRound from "./TourneyInfoRound.vue";

    import { ApmTournamentInfo, ApmTournamentMatch } from "model/apm_tournament/ApmTournamentInfo";
    import { RoundMatches, RoundMatchGame } from "../common";

    import { TournamentMatchTeamNames, TournamentMatchTeamNamesEntry } from "components/app/TournamentCommon";

    export const TourneyInfoView = Vue.extend({
        props: {
            TournamentInfo: { type: Object as PropType<ApmTournamentInfo>, required: false }
        },

        data: function() {
            return {

            }
        },

        methods: {

        },

        computed: {

            roundMatches: function(): RoundMatches[] {
                let rounds: RoundMatches[] = [];

                for (const round of this.TournamentInfo.rounds) {
                    const r: RoundMatches = new RoundMatches();

                    r.roundID = round.id;
                    r.round = round.round;

                    const games: ApmTournamentMatch[] = this.TournamentInfo.matches.filter(iter => iter.roundID == r.roundID);

                    r.matches = games.map(iter => {
                        const i: RoundMatchGame = new RoundMatchGame();

                        i.id = iter.id;
                        i.matchNumber = iter.matchNumber;
                        i.winnerID = iter.winnerEntryID;
                        i.teams = this.TournamentInfo.teams.filter(t => iter.entries.map(i2 => i2.entryID).indexOf(t.id) > -1);
                        i.winnerID = iter.winnerEntryID;
                        i.games = iter.games;
                        i.matchInfo = iter;
                        i.winner = this.TournamentInfo.teams.find(t => t.id == iter.winnerEntryID) || null;

                        return i;
                    });

                    rounds.push(r);
                }

                return rounds;
            }
        },

        components: {
            TourneyInfoRound
        }

    });
    export default TourneyInfoView;
</script>