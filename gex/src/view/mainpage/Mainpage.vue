<template>
    <div style="max-width: 100vw">
        <div class="d-flex align-items-center">
            <gex-menu class="flex-grow-1"></gex-menu>
        </div>

        <hr class="border" />

        <div>
            <div v-if="recent.state == 'idle'"></div>

            <div v-else-if="recent.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="recent.state == 'loaded'">

                <match-list :matches="recent.data"></match-list>

                <div class="d-flex">
                    <a v-if="offset > 24" href="/" class="btn btn-primary me-2">
                        First
                    </a>

                    <a :href="'/?offset=' + (offset - 24)" v-if="offset >= 24" class="btn btn-primary">
                        Newer
                    </a>

                    <div class="flex-grow-1"></div>

                    <a :href="'/?offset=' + (offset + 24)" class="btn btn-primary">
                        Older
                    </a>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import { GexMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import MatchList from "components/app/MatchList.vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchApi } from "api/BarMatchApi";

    import "filters/MomentFilter";

    export const Mainpage = Vue.extend({
        props: {

        },

        created: function(): void {
            document.title = "Gex / Recent matches";
        },

        beforeMount: function(): void {

            const search: URLSearchParams = new URLSearchParams(location.search);
            if (search.has("offset")) {
                this.offset = Number.parseInt(search.get("offset")!);
            }

            this.loadRecent();
        },

        data: function() {
            return {
                offset: 0 as number,
                limit: 24 as number,
                recent: Loadable.idle() as Loading<BarMatch[]>
            }
        },

        methods: {
            loadRecent: async function(): Promise<void> {
                this.recent = Loadable.loading();
                this.recent = await BarMatchApi.getRecent(this.offset);
            },
        },

        components: {
            InfoHover, GexMenu,
            MatchList
        }
    });

    export default Mainpage;
</script>