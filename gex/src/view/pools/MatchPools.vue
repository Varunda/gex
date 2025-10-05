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

            <div v-if="pools.state == 'idle'"></div>

            <div v-else-if="pools.state == 'loading'">
                loading...
            </div>

            <div v-else-if="pools.state == 'loaded'">

                <div v-for="pool in pools.data" :key="pool.id">
                    <a :href="'/pool/' + pool.id">
                        {{ pool.name }}
                    </a>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { MatchPool } from "model/MatchPool";

    import { MatchPoolApi } from "api/MatchPoolApi";

    import AccountUtil from "util/Account";

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

        }
    });
    export default MatchPools;

</script>