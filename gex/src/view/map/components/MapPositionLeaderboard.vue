<template>
    <div>
        <h2 class="wt-header">Role leaderboard</h2>

        <div class="mb-3">
            Position select
            <select class="form-control mb-2" v-model="selectedPosition">
                <option :value="null">Select a position</option>
                <option v-for="pos in positions" :key="pos" :value="pos">
                    {{ pos }}
                </option>
            </select>

            <span>
                Score is calculated by <code>win rate * average enemy skill</code>. Must have at least 50 games to be included.
            </span>
        </div>

        <div>
            <span v-if="selectedPosition == null">
                No position selected
            </span>

            <table v-else-if="selectedGroup != null" class="table table-hover table-sm">

                <thead>
                    <tr class="table-secondary">
                        <th>Rank</th>
                        <th>User</th>
                        <th>Score</th>
                        <th>Win rate</th>
                        <th>Avg enemy skill</th>
                    </tr>
                </thead>

                <tbody>
                    <tr v-for="(entry, index) in selectedGroup.entries" :key="entry.userID">
                        <td>
                            {{ index + 1 }}
                        </td>

                        <td>
                            <a :href="'/user/' + entry.userID" target="blank" ref="nofollow">
                                <span v-if="entry.user != null">
                                    {{ entry.user.username }}
                                </span>
                                <span v-else>
                                    &lt;missing {{ entry.userID }}&gt;
                                </span>
                            </a>
                        </td>

                        <td>
                            <span :title="entry.score">
                                {{ entry.score | locale(2) }}
                            </span>
                        </td>

                        <td>
                            {{ entry.winCount / entry.playCount * 100 | locale(2) }}%
                            ({{ entry.winCount | locale(0) }} / {{ entry.playCount | locale(0) }})
                        </td>

                        <td>
                            {{ entry.averageEnemySkill | locale(2) }}
                        </td>
                    </tr>
                </tbody>

            </table>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { MapStats } from "model/map_stats/MapStats";
    import { MapPositionLeaderboardEntry } from "model/map_stats/MapPositionLeaderboardEntry";

    import "filters/LocaleFilter";

    type PositionGroup = {
        role: string;
        entries: MapPositionLeaderboardEntry[]
    }

    export const MapPositionLeaderboard = Vue.extend({
        props: {
            stats: { type: Object as PropType<MapStats>, required: true }
        },

        data: function() {
            return {
                selectedPosition: null as string | null
            }
        },

        methods: {

        },

        computed: {
            positions: function(): string[] {
                return Array.from(new Set<string>(this.stats.positionLeaderboard.map(iter => iter.positionLabel)).values());
            },

            entries: function(): PositionGroup[] {
                const map: Map<string, MapPositionLeaderboardEntry[]> = new Map();

                for (const entry of this.stats.positionLeaderboard) {
                    const entries: MapPositionLeaderboardEntry[] = map.get(entry.positionLabel) ?? [];
                    entries.push(entry);

                    map.set(entry.positionLabel, entries);
                }

                const ret: PositionGroup[] = Array.from(map.entries()).map(iter => {
                    return {
                        role: iter[0],
                        entries: iter[1].sort((a, b) => {
                            return b.score - a.score;
                        })
                    }
                });

                return ret;
            },

            selectedGroup: function(): PositionGroup | null {
                if (this.selectedPosition == null) {
                    return null;
                }

                return this.entries.find(iter => iter.role == this.selectedPosition) ?? null;
            }
        },

        components: {

        }
    });
    export default MapPositionLeaderboard;
</script>