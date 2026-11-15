<template>
    <div>
        <h2>Gex import</h2>

        <div class="mb-5">
            <h3>Gex</h3>

            <input type="text" class="form-control mb-2" placerholder="Gex url" v-model="gexUrl">

            <button class="btn btn-info" @click="testGexUrl">
                Test
            </button>
        </div>

        <div class="mb-5">
            <h3>Match</h3>

            <textarea class="form-control" v-model="matches" placeholder="Match IDs, seperated by new lines"></textarea>

            <div>
                seperate with newlines
            </div>

            <button class="btn btn-primary" @click="importMatches">
                Import
            </button>
        </div>

        <div>
            <h3>Pool</h3>

        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";

    import { GexImportApi } from "api/GexImportApi";

    export const GexImport = Vue.extend({
        props: {

        },

        data: function() {
            return {
                gexUrl: "" as string,
                validGexUrl: false as boolean,

                matches: "" as string

            }
        },

        methods: {

            testGexUrl: function(): void {

            },

            importMatches: async function(): Promise<void> {
                const ids: string[] = this.matches.split("\n");

                for (const id of ids) {
                    console.log(`GexImport> match ${id} from ${this.gexUrl}`);
                    await GexImportApi.match(this.gexUrl, id.trim());
                }
            }

        },

        components: {

        }
    });
    export default GexImport;

</script>