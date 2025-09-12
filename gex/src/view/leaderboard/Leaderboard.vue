<template>
    <div class="container container-remove-padding">
        <h2 class="wt-header bg-light text-dark">Leaderboard</h2>

        <div v-if="seasons.state == 'loaded'">
            <div class="btn-group w-100 mb-3">
                <button v-for="season in seasons.data" :key="season.season" class="btn" @click="loadSeason(season.season)"
                    :class="[ selectedSeason == season.season ? 'btn-primary' : 'btn-secondary' ]">

                    Season {{ season.season }}
                </button>
            </div>
        </div>

        <div v-if="leaderboard.state == 'loading'" class="text-center">
            <busy class="busy busy-sm"></busy>
            Loading...
        </div>

        <div v-else-if="leaderboard.state == 'loaded'" class="d-flex flex-wrap justify-content-around" style="gap: 1rem;">
            <div v-for="group in skillLeaderboardGrouped" :key="group.gamemode">
                <h3 class="text-center mb-0">
                    {{ group.gamemode | gamemode }}
                </h3>

                <table class="table table-sm table-hover">
                    <thead class="table-ligh text-center">
                        <tr>
                            <th></th>
                            <th>Player</th>
                            <th>
                                Rating
                                <info-hover text="Leaderboard rating is (skill - (3 * uncertainty)), which is different from the match rating"></info-hover>
                            </th>
                        </tr>
                    </thead>

                    <tbody>
                        <tr v-for="(entry, index) in group.entries" :key="entry.userID">
                            <td>
                                <span v-if="index + 1 == 1">
                                    <span class="bi bi-trophy-fill" style="color: var(--bs-yellow)"></span>
                                    1st
                                </span>
                                <span v-else-if="index + 1 == 2">
                                    <span class="bi bi-trophy-fill" style="color: silver"></span>
                                    2nd
                                </span>
                                <span v-else-if="index + 1 == 3">
                                    <span class="bi bi-trophy-fill" style="color: coral"></span>
                                    3rd
                                </span>
                                <span v-else>
                                    {{ index + 1 }}th
                                </span>
                            </td>
                            <td class="px-2">
                                <a :href="'/user/' + entry.userID">
                                    {{ entry.username }}
                                </a>
                            </td>
                            <td class="px-2">
                                {{ entry.skill | locale(2) }}
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import InfoHover from "components/InfoHover.vue";
    import Busy from "components/Busy.vue";

    import { BarLeaderboardApi } from "api/BarLeaderboardApi";

    import { BarLeaderboardSkillEntry } from "model/BarLeaderboardSkillEntry";
    import { BarSeason } from "model/BarSeason";

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";

    type GroupedSkillLeaderboard = {
        gamemode: number;
        entries: BarLeaderboardSkillEntry[];
    };

    export const Leaderboard = Vue.extend({
        props: {

        },

        data: function() {
            return {
                seasons: Loadable.idle() as Loading<BarSeason[]>,
                leaderboard: Loadable.idle() as Loading<BarLeaderboardSkillEntry[]>,
                selectedSeason: -1 as number,
            }
        },

        mounted: function(): void {
            document.title = "Gex / Leaderboard";
            this.loadData();
        },

        methods: {
            loadData: async function(): Promise<void> {
                this.seasons = Loadable.loading();
                this.seasons = await BarLeaderboardApi.getSeasons();

                if (this.seasons.state == "loaded") {
                    this.selectedSeason = Math.max(...this.seasons.data.map(iter => iter.season));
                    this.loadSeason(this.selectedSeason);
                }
            },

            loadSeason: async function(season: number): Promise<void> {
                this.selectedSeason = season;
                this.leaderboard = Loadable.loading();
                this.leaderboard = await BarLeaderboardApi.getSkillLeaderboard(100, season);
            }
        },

        computed: {

            skillLeaderboardGrouped: function(): GroupedSkillLeaderboard[] {
                if (this.leaderboard.state != "loaded") {
                    return [];
                }

                return this.leaderboard.data.reduce((acc: GroupedSkillLeaderboard[], iter: BarLeaderboardSkillEntry) => {
                    const g = acc.find(i => i.gamemode == iter.gamemode);

                    if (g == undefined) {
                        acc.push({
                            gamemode: iter.gamemode,
                            entries: [iter]
                        });
                    } else {
                        g.entries.push(iter);
                    }

                    return acc;
                }, []).sort((a, b) => a.gamemode - b.gamemode);
            },
        },

        components: {
            InfoHover, Busy
        }
    });

    export default Leaderboard;
</script>