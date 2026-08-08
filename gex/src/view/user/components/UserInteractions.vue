<template>
    <div>
        <h2 class="wt-header border-0">
            Player encounters
        </h2>

        <div class="row mb-3">
            <div class="col-12 col-lg-6 btn-group mb-2 align-items-center">
                <button class="btn btn-outline-light" :class="[ selectedGamemode == null ? 'btn-primary' : 'btn-secondary']" @click="selectedGamemode = null">
                    All
                </button>
                <button class="btn btn-outline-light" :class="[ selectedGamemode == 1 ? 'btn-primary' : 'btn-secondary']" @click="selectedGamemode = 1">
                    Duel
                </button>
                <button class="btn btn-outline-light" :class="[ selectedGamemode == 2 ? 'btn-primary' : 'btn-secondary']" @click="selectedGamemode = 2">
                    Small team
                </button>
                <button class="btn btn-outline-light" :class="[ selectedGamemode == 3 ? 'btn-primary' : 'btn-secondary']" @click="selectedGamemode = 3">
                    Large team
                </button>
                <button class="btn btn-outline-light" :class="[ selectedGamemode == 4 ? 'btn-primary' : 'btn-secondary']" @click="selectedGamemode = 4">
                    FFA
                </button>
                <button class="btn btn-outline-light" :class="[ selectedGamemode == 5 ? 'btn-primary' : 'btn-secondary']" @click="selectedGamemode = 5">
                    Team FFA
                </button>
                <button class="btn btn-outline-light" :class="[ selectedGamemode == 0 ? 'btn-primary' : 'btn-secondary']" @click="selectedGamemode = 0">
                    Other
                </button>
            </div>

            <div class="col-12 col-lg-6 mb-3 row">
                <div class="col-12 col-lg-6">
                    <label class="form-label mb-0">Include games after</label>
                    <date-time-input v-model="filter.periodStart" :allow-null="true"></date-time-input>
                </div>

                <div class="col-12 col-lg-6">
                    <label class="form-label mb-0">Include games before</label>
                    <date-time-input v-model="filter.periodEnd" :allow-null="true"></date-time-input>
                </div>
            </div>

            <div class="col-12">
                <button class="btn" :class="[ needsRebind == true ? 'btn-primary' : 'btn-secondary' ]" @click="bind">
                    Load
                </button>
            </div>
        </div>

        <a-table :entries="selected" :show-filters="true" default-sort-field="total" default-sort-order="desc" :paginate="true" :default-page-size="10" :overflow-wrap="true">
            <a-col sort-field="unitName">
                <a-header>
                    <b>Player</b>
                </a-header>

                <a-filter field="targetUsername" type="string" method="input"
                    :conditions="[ 'contains', 'equals' ]">
                </a-filter>

                <a-body v-slot="entry">
                    <a :href="'/user/' + entry.targetUserID">
                        {{ entry.targetUsername }}
                    </a>
                </a-body>
            </a-col>
            
            <a-col sort-field="total">
                <a-header>
                    <b>Total plays</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.total | locale(0) }}
                </a-body>
            </a-col>

            <a-col sort-field="withCount">
                <a-header>
                    <b>As ally</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.withCount | locale(0) }}
                </a-body>
            </a-col>

            <a-col sort-field="withWin">
                <a-header>
                    <b>As ally (win/loss)</b>
                </a-header>

                <a-body v-slot="entry">
                    <span v-if="entry.withCount == 0" class="text-muted">
                        --
                    </span>
                    
                    <span v-else>
                        <span style="color: var(--bs-success-text-emphasis)">
                            {{ entry.withWin | locale(0) }}
                        </span>
                        /
                        <span style="color: var(--bs-danger-text-emphasis)">
                            {{ entry.withCount - entry.withWin | locale(0) }}
                        </span>
                        ({{ entry.withWin / Math.max(1, entry.withCount) * 100 | locale(2) }}%)
                    </span>
                </a-body>
            </a-col>

            <a-col sort-field="againstCount">
                <a-header>
                    <b>As enemy</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.againstCount | locale(0) }}
                </a-body>
            </a-col>

            <a-col sort-field="againstWin">
                <a-header>
                    <b>As enemy (win/loss)</b>
                </a-header>

                <a-body v-slot="entry">
                    <span v-if="entry.againstCount == 0" class="text-muted">
                        --
                    </span>
                    
                    <span v-else>
                        <span style="color: var(--bs-success-text-emphasis)">
                            {{ entry.againstWin | locale(0) }}
                        </span>
                        /
                        <span style="color: var(--bs-danger-text-emphasis)">
                            {{ entry.againstCount - entry.againstWin | locale(0) }}
                        </span>
                        ({{ entry.againstWin / Math.max(1, entry.againstCount) * 100 | locale(2) }}%)
                    </span>
                </a-body>
            </a-col>
        </a-table>

        <div>
            <span v-if="searched.periodStart == null && searched.periodEnd == null">
                Showing player interactions from all games
            </span>

            <span v-else>
                Showing player interactions for games
                <span v-if="searched.periodStart != null">
                    after {{ searched.periodStart | moment }}
                </span>
                <span v-if="searched.periodStart != null && searched.periodEnd != null">
                    and
                </span>
                <span v-if="searched.periodEnd != null">
                    before {{ searched.periodEnd | moment }}
                </span>
            </span>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import { BarUserApi } from "api/BarUserApi";

    import { BarUserInteractions } from "model/BarUserInteractions";
    import { BarUser } from "model/BarUser";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import DateTimeInput from "components/DateTimeInput.vue";

    import "filters/LocaleFilter";

    export const UserInteractions = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true }
        },

        data: function() {
            return {
                interactions: Loadable.idle() as Loading<BarUserInteractions[]>,
                selectedGamemode: null as number | null,

                filter: {
                    periodStart: null as Date | null,
                    periodEnd: null as Date | null
                },

                searched: {
                    periodStart: null as Date | null,
                    periodEnd: null as Date | null
                }
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.bind();
            });
        },

        methods: {
            bind: async function(): Promise<void> {
                this.searched.periodStart = this.filter.periodStart;
                this.searched.periodEnd = this.filter.periodEnd;

                this.interactions = Loadable.loading();
                this.interactions = await BarUserApi.getInteractions(this.user.userID, this.filter.periodStart, this.filter.periodEnd);
            }
        },

        computed: {
            selected: function(): Loading<BarUserInteractions[]> {
                if (this.interactions.state != "loaded") {
                    return this.interactions;
                }

                return Loadable.loaded(this.interactions.data.filter(iter => {
                    return iter.gamemode == this.selectedGamemode
                }));
            },

            needsRebind: function(): boolean {
                return this.searched.periodStart?.getTime() != this.filter.periodStart?.getTime()
                    || this.searched.periodEnd?.getTime() != this.filter.periodEnd?.getTime();
            }
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            DateTimeInput
        }
    });
    export default UserInteractions;
</script>