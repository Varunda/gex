<template>
    <div class="container">
        <div v-if="hasCreatePoolPermission" class="mb-3">
            <span>create pool</span>
            <div class="input-group mb-3">
                <input v-model="newPoolName" class="form-control" type="text" placeholder="new match pool name"/>
                <button class="btn btn-primary " @click="createPool">
                    create
                </button>
            </div>
        </div>

        <div>
            <h2 class="wt-header bg-light text-dark">Pools</h2>

            <a-table :entries="pools" display-type="table" :show-filters="true" default-sort-field="timestamp" default-sort-order="desc">

                <a-col sort-field="name">
                    <a-header>
                        <b>Name</b>
                    </a-header>

                    <a-filter field="name" type="string" method="input"
                        :conditions="[ 'contains', 'equals' ]">
                    </a-filter>

                    <a-body v-slot="entry">
                        <a :href="'/pool/' + entry.id">
                            {{ entry.name }}
                        </a>
                    </a-body>
                </a-col>

                <a-col sort-field="timestamp">
                    <a-header>
                        <b>Timestamp</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.timestamp | moment }}
                    </a-body>
                </a-col>

            </a-table>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { MatchPool } from "model/MatchPool";

    import { MatchPoolApi } from "api/MatchPoolApi";

    import AccountUtil from "util/Account";

    import "filters/MomentFilter";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";

    export const MatchPools = Vue.extend({
        props: {

        },

        data: function() {
            return {
                pools: Loadable.idle() as Loading<MatchPool[]>,

                newPoolName: "" as string
            }
        },

        mounted: function(): void {
            document.title = "Gex / Pools";
            this.loadPools();
        },

        methods: {

            loadPools: async function(): Promise<void> {
                this.pools = Loadable.loading();
                this.pools = await MatchPoolApi.getAll();
            },
            
            createPool: async function(): Promise<void> {
                await MatchPoolApi.create(this.newPoolName);
                this.newPoolName = "";
                await this.loadPools();
            }

        },

        computed: {
            hasCreatePoolPermission: function(): boolean {
                return AccountUtil.hasPermission("Gex.MatchPool.Create");
            }

        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
        }
    });
    export default MatchPools;

</script>