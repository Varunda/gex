<template>
    <div style="max-width: 100vw">
        <div class="d-flex align-items-center">
            <gex-menu class="flex-grow-1"></gex-menu>
        </div>

        <hr class="border" />

        <div>

        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import { GexMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";

    import "filters/MomentFilter";

    export const Mainpage = Vue.extend({
        props: {

        },

        created: function(): void {
            document.title = "Gex";
        },

        data: function() {
            return {

            }
        },

        methods: {

        },

        components: {
            InfoHover,
            GexMenu,
        }
    });

    export default Mainpage;
</script>