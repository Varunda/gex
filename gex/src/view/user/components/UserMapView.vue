<template>
    <div>
        <h2 class="wt-header">
            Viewing map stats for {{ user.username }} on {{ map }}
        </h2>

        <div>
            <h3 class="wt-header">
                Position win rate
            </h3>

            <collapsible header-text="Filters" :show="false" size-class="h6">
                <div class="mb-2">
                    <label>Min OS</label>
                    <input v-model.number="filter.minOS" type="number" class="form-control mb-2">

                    <label>Period start</label>
                    <date-time-input v-model="filter.periodStart" :allow-null="true" class="mb-2"></date-time-input>
                </div>

                <span v-if="hasFilters" class="mb-3">
                    Filtering for matches with:

                    {{ conditions.join(", ") }}
                </span>

            </collapsible>

            <a-table :entries="positionWinRates" default-sort-field="total" default-sort-order="desc">
                <a-col sort-field="position">
                    <a-header>
                        <b>Position</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.position }}
                    </a-body>
                </a-col>

                <a-col sort-field="wins">
                    <a-header>
                        <b>Win/loss</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <span style="color: var(--bs-success-text-emphasis)">
                            {{ entry.wins }}
                        </span>
                        /
                        <span style="color: var(--bs-danger-text-emphasis)">
                            {{ entry.total - entry.wins }}
                        </span>
                        <span>
                            ({{ entry.wins / Math.max(1, entry.total) * 100 | locale(0) }}%)
                        </span>
                    </a-body>
                </a-col>

                <a-col sort-field="total">
                    <a-header>
                        <b>Plays</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.total }}
                        ({{ entry.total / matches.length * 100 | locale(2) }}%)
                    </a-body>
                </a-col>
            </a-table>

        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import Busy from "components/Busy.vue";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";
    import Collapsible from "components/Collapsible.vue";
    import DateTimeInput from "components/DateTimeInput.vue";

    import { BarUser } from "model/BarUser";
    import { BarMatch } from "model/BarMatch";

    import "filters/MomentFilter";
    import "filters/LocaleFilter";

    import TimeUtils from "util/Time";

    type MapPositionWinRate = {
        map: string,
        position: string,
        wins: number,
        total: number
    }

    export const UserMapView = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            map: { type: String, required: true },
            matches: { type: Array as PropType<BarMatch[]>, required: true },
        },

        data: function() {
            return {

                filter: {
                    minOS: null as number | null,
                    periodStart: null as Date | null
                }
            }
        },

        methods: {

        },

        computed: {
            
            positionWinRates: function(): Loading<MapPositionWinRate[]> {
                const map: Map<string, MapPositionWinRate> = new Map();

                for (const match of this.matches) {
                    const player = match.players.find(iter => iter.userID == this.user.userID);
                    if (player == undefined) {
                        console.warn(`UserMapView> missing player in match [match=${match.id}] [user=${this.user.userID}]`)
                        continue;
                    }

                    if (this.filter.minOS != null) {
                        if (match.minOS < this.filter.minOS) {
                            continue;
                        }
                    }

                    const posWin: MapPositionWinRate = map.get(player.startSpotLabel ?? "<unknown>") ?? {
                        map: this.map,
                        position: player.startSpotLabel ?? "<unknown>",
                        total: 0,
                        wins: 0,
                    };

                    ++posWin.total;

                    const allyTeam = match.allyTeams.find(iter => iter.allyTeamID == player.allyTeamID);
                    if (allyTeam == undefined) {
                        console.warn(``);
                    }

                    if (allyTeam?.won == true) {
                        ++posWin.wins;
                    }

                    map.set(posWin.position, posWin);
                }

                return Loadable.loaded(Array.from(map.values()));
            },

            totalCount: function(): number {
                if (this.positionWinRates.state != "loaded") {
                    return 0;
                }

                return this.positionWinRates.data.reduce((acc, iter) => acc += iter.total, 0);
            },

            conditions: function(): string[] {
                const ret: string[] = [];

                if (this.filter.minOS != null && this.filter.minOS != "") {
                    ret.push(`minimum OS is above ${this.filter.minOS}`);
                }
                if (this.filter.periodStart != null) {
                    ret.push(`game started after ${TimeUtils.format(this.filter.periodStart)}`);
                }

                return ret;
            },

            hasFilters: function(): boolean {
                return (this.filter.minOS != null && this.filter.minOS != "") || this.filter.periodStart != null;
            }

        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            InfoHover, Busy, Collapsible, DateTimeInput
        }
    });

    export default UserMapView;
</script>