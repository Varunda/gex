<template>
    <div class="container">
        <div v-if="hasAddRemovePermission" class="mb-3">
            <toggle-button v-model="showPoolEdit">show edit</toggle-button>
        </div>

        <div class="mb-3">
            <h2 class="wt-header bg-light text-dark">
                Pool: 
                <span v-if="pool.state == 'loaded'">
                    {{ pool.data.name }}
                </span>
            </h2>

            <div v-if="matches.state == 'idle'"></div>

            <div v-else-if="matches.state == 'loading'" class="text-center">
                <busy class="busy busy-sm"></busy>
                Loading...
            </div>

            <div v-else-if="matches.state == 'loaded'">
                <match-list :matches="matches.data"></match-list>

                <div v-if="matches.data.length == 0">
                    No matches found!
                </div>
            </div>

            <div v-else-if="matches.state == 'error'">
                <api-error :error="matches.problem"></api-error>
            </div>
        </div>

        <div v-if="showPoolEdit">
            <h2 class="wt-header bg-light text-dark">edit pool</h2>

            <label class="d-block">match IDs to add, newline seperate</label>
            <textarea v-model="addMatch" class="form-control"></textarea>
            <button class="btn btn-primary mt-2" @click="addMatchesToPool">add</button>

            <label class="d-block">remove matches</label>

            <div v-if="matches.state == 'loaded'">
                <table class="table table-sm table-border">
                    <tr v-for="match in matches.data" :key="match.id">
                        <td class="font-monospace">
                            {{ match.id }} 
                        </td>
                        <td>
                            <button class="btn btn-danger px-2 btn-sm" @click="removeMatchFromPool(match.id)">
                                &times;
                            </button>
                        </td>
                    </tr>
                </table>
            </div>

        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import InfoHover from "components/InfoHover.vue";
    import MatchList from "components/app/MatchList.vue";
    import ToggleButton from "components/ToggleButton";
    import ApiError from "components/ApiError";
    import Busy from "components/Busy.vue";

    import { MatchPool } from "model/MatchPool";
    import { BarMatch } from "model/BarMatch";

    import { MatchPoolApi } from "api/MatchPoolApi";
    import { BarMatchApi } from "api/BarMatchApi";

    import AccountUtil from "util/Account";

    export const MatchPoolView = Vue.extend({
        props: {

        },

        data: function() {
            return {
                poolID: 0 as number,

                pool: Loadable.idle() as Loading<MatchPool>,
                matches: Loadable.idle() as Loading<BarMatch[]>,

                showPoolEdit: false as boolean,
                addMatch: "" as string
            }
        },

        mounted: function(): void {
            this.poolID = Number.parseInt(location.pathname.split("/")[2]);
            console.log(`MatchPoolView> parsed poolID [poolID=${this.poolID}]`);
            this.loadPool();
        },

        methods: {
            loadPool: async function(): Promise<void> {
                this.pool = Loadable.loading();
                this.pool = await MatchPoolApi.getByID(this.poolID);

                if (this.pool.state != "loaded") {
                    return;
                }

                this.matches = Loadable.loading();
                this.matches = await BarMatchApi.search(0, 100, "start_time", "desc", {
                    poolID: this.poolID
                });
            },

            addMatchesToPool: async function(): Promise<void> {
                const matchIds: string [] = this.addMatch.split("\n");

                let added: number = 0;
                let total: number = matchIds.length;

                for (const matchId of matchIds) {
                    if (matchId == "") {
                        continue;
                    }
                    let mID: string = matchId.trim();
                    console.log(`adding match ${mID} to pool ${this.poolID}`);
                    const res: Loading<void> = await MatchPoolApi.addMatchToPool(this.poolID, mID);
                    if (res.state != "loaded") {
                        console.error(`failed to add match ${mID} to pool ${this.poolID}`);
                    } else {
                        ++added;
                    }
                }

                Toaster.add(`Matches added`, `Added ${added}/${total} matches to pool ${this.poolID}`, "success");

                await this.loadPool();
            },

            removeMatchFromPool: async function(matchId: string): Promise<void> {
                await MatchPoolApi.removeMatchFromPool(this.poolID, matchId);
                await this.loadPool();
            }

        },

        computed: {
            hasAddRemovePermission: function(): boolean {
                return AccountUtil.hasPermission("Gex.MatchPoolEntry.AddRemove");
            }

        },

        components: {
            InfoHover, MatchList, ToggleButton, ApiError, Busy
        }
    });
    export default MatchPoolView;
</script>