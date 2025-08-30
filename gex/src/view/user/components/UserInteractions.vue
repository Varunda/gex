<template>
    <div>
        <h2 class="wt-header bg-white text-dark mb-2">
            Player encounters
        </h2>

        <a-table :entries="interactions" :show-filters="true" default-sort-field="total" default-sort-order="desc" :paginate="true" :default-page-size="10" :overflow-wrap="true">
            <a-col sort-field="unitName">
                <a-header>
                    <b>Player</b>
                </a-header>

                <a-filter field="unitName" type="string" method="input"
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

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";

    import "filters/LocaleFilter";

    export const UserInteractions = Vue.extend({
        props: {
            UserId: { type: Number, required: true }
        },

        data: function() {
            return {
                interactions: Loadable.idle() as Loading<BarUserInteractions[]>
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
                this.interactions = await BarUserApi.getInteractions(this.UserId);
            }
        },

        computed: {

        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
        }
    });
    export default UserInteractions;
</script>