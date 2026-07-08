<template>
    <div>
        <h2 class="wt-header border-0">
            Player encounters
        </h2>

        <div class="btn-group mb-2">
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
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import { BarUserApi } from "api/BarUserApi";

    import { BarUserInteractions } from "model/BarUserInteractions";
    import { BarUser } from "model/BarUser";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";

    import "filters/LocaleFilter";

    export const UserInteractions = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true }
        },

        data: function() {
            return {
                interactions: Loadable.idle() as Loading<BarUserInteractions[]>,
                selectedGamemode: null as number | null
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.bind();
            });
        },

        methods: {
            bind: async function(): Promise<void> {
                this.interactions = Loadable.loading();
                this.interactions = await BarUserApi.getInteractions(this.user.userID);
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
            }

        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
        }
    });
    export default UserInteractions;
</script>