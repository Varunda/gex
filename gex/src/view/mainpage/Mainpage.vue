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

            <div v-else-if="recent.state == 'loaded'" class="d-flex flex-wrap justify-content-center">
                <div v-for="match in recent.data" :key="match.id" class="card me-2 mb-2" style="width: 18rem;">
                    <img :src="getMapThumbnail(match.map)" class="card-img-top">
                    <div class="card-body">
                        <h5>
                            <a :href="'/match/' + match.id">
                                {{ match.map }}
                            </a>
                        </h5>
                        {{ match.id }}
                    </div>
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

    import { BarMatch } from "model/BarMatch";
    import { BarMatchApi } from "api/BarMatchApi";

    import "filters/MomentFilter";

    export const Mainpage = Vue.extend({
        props: {

        },

        created: function(): void {
            document.title = "Gex";
        },

        beforeMount: function(): void {
            this.loadRecent();
        },

        data: function() {
            return {
                recent: Loadable.idle() as Loading<BarMatch[]>

            }
        },

        methods: {
            loadRecent: async function(): Promise<void> {
                this.recent = Loadable.loading();
                this.recent = await BarMatchApi.getRecent();
            },

            getMapThumbnail: function(map: string): string {
                return `https://api.bar-rts.com/maps/${map.replace(/ /gm, "_").toLowerCase()}/texture-thumb.jpg`;
            }
        },

        components: {
            InfoHover,
            GexMenu,
        }
    });

    export default Mainpage;
</script>