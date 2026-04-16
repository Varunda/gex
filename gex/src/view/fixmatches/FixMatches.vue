<template>
    <div>

        <button class="btn btn-primary" @click="bind()">
            refresh
        </button>

        <a-table :entries="pending" default-sort-field="replayDownloaded" default-sort-order="desc" :show-filters="true">

            <a-col sort-field="gameID">
                <a-header>
                    <b>game id</b>
                </a-header>

                <a-body v-slot="entry">
                    <span class="font-monospace">
                        {{ entry.gameID }}
                    </span>
                </a-body>
            </a-col>

            <a-col sort-field="replayDownloaded">
                <a-header>
                    <b>replay dl</b>
                </a-header>

                <a-filter field="replayDownloaded" type="has"></a-filter>

                <a-body v-slot="entry">
                    {{ entry.replayDownloaded | moment }}
                </a-body>
            </a-col>

            <a-col sort-field="replayDownloadedMs">
                <a-header>
                    <b>dl ms</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.replayDownloadedMs }}ms
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>replay parse</b>
                </a-header>

                <a-filter field="replayParsed" type="has"></a-filter>

                <a-body v-slot="entry">
                    {{ entry.replayParsed | moment }}
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>parse ms</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.replayParsedMs }}ms
                </a-body>
            </a-col>

            <a-col sort-field="replaySimulated">
                <a-header>
                    <b>replay sim</b>
                </a-header>

                <a-filter field="replaySimulated" type="has"></a-filter>

                <a-body v-slot="entry">
                    {{ entry.replaySimulated | moment }}
                </a-body>
            </a-col>

            <a-col sort-field="replaySimulatedMs">
                <a-header>
                    <b>sim ms</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.replaySimulatedMs | duration }}
                </a-body>
            </a-col>

            <a-col sort-field="actionsParsed">
                <a-header>
                    <b>actions</b>
                </a-header>

                <a-filter field="actionsParsed" type="has"></a-filter>

                <a-body v-slot="entry">
                    {{ entry.actionsParsed | moment }}
                </a-body>
            </a-col>

            <a-col sort-field="actionsParsedMs">
                <a-header>
                    <b>actions ms</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.actionsParsedMs }}ms
                </a-body>
            </a-col>

        </a-table>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";

    import { BarMatchProcessing } from "model/BarMatchProcessing";

    import { BarMatchProcessingApi } from "api/BarMatchProcessingApi";

    import "filters/MomentFilter";

    export const FixMatches = Vue.extend({
        props: {

        },

        data: function() {
            return {
                pending: Loadable.idle() as Loading<BarMatchProcessing[]>,
            }
        },

        beforeMount: function(): void {
            this.bind();
        },

        methods: {
            bind: async function(): Promise<void> {
                this.pending = Loadable.loading();
                this.pending = await BarMatchProcessingApi.getPending();
            }
        },

        computed: {

        },

        components: {
            InfoHover,
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
        }

    });
    export default FixMatches;

</script>