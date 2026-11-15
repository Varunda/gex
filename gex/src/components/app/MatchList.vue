
<template>
    <div class="" style="font-size: 14px; line-height: 1;">

        <div v-for="(group, index) in matchGroups" :key="index" class="me-3 mb-3">
            <h2 v-if="group.name != null" class="text-center">
                {{ group.name }}
            </h2>

            <div class="d-flex flex-wrap justify-content-center">
                <div v-for="match in group.matches" :key="match.id" class="me-3 mb-3">
                    <match-info :match="match" :show-time="true" :team-names="getMatchTeamNames(match)"></match-info>
                </div>
            </div>

            <hr v-if="group.name != null" class="border w-100">
        </div>

    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import MatchInfo from "./MatchInfo.vue";

    import "filters/TimeAgoFilter";
    import "filters/CompactTimeAgoFilter";
    import "filters/MomentFilter";

    import { TournamentMatchTeamNames, TournamentMatchTeamNamesEntry } from "./TournamentCommon";

    import { BarMatch } from "model/BarMatch";
    import { ApmTournamentTeam } from "model/apm_tournament/ApmTournamentInfo";

    class MatchGroup {
        public name: string | null = null;
        public matches: BarMatch[] = [];
        public lastMatchTime: Date | null = null;
    }

    export const MatchList = Vue.extend({
        props: {
            matches: { type: Array as PropType<BarMatch[]>, required: true },
            GroupMatchOverride: { type: Boolean, required: false, default: false },
            TeamNames: { type: Object as PropType<TournamentMatchTeamNames | undefined>, required: false },
        },

        data: function() {
            return {

            }
        },

        methods: {

            getMatchTeamNames: function(match: BarMatch): ApmTournamentTeam[] | null {
                if (this.TeamNames == undefined) {
                    return null;
                }

                const entry: TournamentMatchTeamNamesEntry | undefined = this.TeamNames.entries.find(iter => iter.gameID == match.id);
                if (entry == undefined) {
                    return null;
                }

                return [...entry.teams];
            }

        },

        computed: {
            matchGroups: function(): MatchGroup[] {
                const hasNames: boolean = this.matches.find(iter => iter.matchPoolEntryNote != null) != null;

                if (hasNames == false || this.GroupMatchOverride == true) {
                    return [
                        {
                            name: null,
                            matches: this.matches,
                            lastMatchTime: null
                        }
                    ];
                }

                const map: Map<string, BarMatch[]> = new Map();
                for (const match of this.matches) {
                    const key: string = match.matchPoolEntryNote ?? "";

                    if (map.has(key) == false) {
                        map.set(key, []);
                    }

                    map.get(key)!.push(match);
                }

                const groups: MatchGroup[] = [];
                const values: BarMatch[][] = Array.from(map.values());
                for (const v of values) {
                    groups.push({
                        name: v[0].matchPoolEntryNote ?? "",
                        matches: v,
                        lastMatchTime: new Date(Math.max(...v.map(iter => iter.startTime.getTime())))
                    });
                }

                groups.sort((a, b) => {
                    return (a.lastMatchTime?.getTime() ?? 0) - (b.lastMatchTime?.getTime() ?? 0);
                });

                return groups;
            },
        },

        components: {
            MatchInfo
        }
    });
    export default MatchList;
</script>
