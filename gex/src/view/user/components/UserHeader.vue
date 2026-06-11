<template>
    <div>
        <h1 class="text-center border-bottom pb-2 mb-4 d-flex align-items-center justify-content-center" :style="usernameStyle">
            <img v-if="user.countryCode != null && user.countryCode != '??' && user.countryCode != 'ARM' && user.countryCode != 'COR'"
                :src="'/img/flags/' + user.countryCode.toLowerCase() + '.png'" width="32" height="24" class="mx-2"
                :title="'country flag for ' + user.countryCode"
            />

            {{ user.username }}
        </h1>

    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";
    Chart.defaults.font.family = "Atkinson Hyperlegible";
    import "chartjs-adapter-luxon";

    import { FactionIcon } from "components/app/FactionIcon";

    import { BarUser } from "model/BarUser";
    import { BarUserMapStats } from "model/BarUserMapStats";
    import { BarUserFactionStats } from "model/BarUserFactionStats";

    export const UserHeader = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
        },

        data: function() {
            return {
            }
        },

        mounted: function(): void {

        },

        methods: {

        },

        computed: {
            usernameStyle: function(): any {
                return {
                    //"background": `no-repeat url("/img/banner/${factionName}_large.jpg")`,
                    //"height": "120px",
                    //"color": "var(--bs-white)",
                    //"text-shadow": "-2px -2px 0 #000, 2px -2px 0 #000, -2px 2px 0 #000, 2px 2px 0 #000"
                };
            },
        },

        components: {
            FactionIcon
        }
    });
    export default UserHeader;
</script>