<template>
    <div>
        <div class="row">
            <div class="col-6">
                <app-accounts></app-accounts>
            </div>

            <div class="col-6">
                <app-groups></app-groups>
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import InfoHover from "components/InfoHover.vue";
    import Busy from "components/Busy.vue";
    import Collapsible from "components/Collapsible.vue";
    import ToggleButton from "components/ToggleButton";

    import "filters/MomentFilter";

    import AppAccounts from "./components/AppAccounts.vue";
    import AppGroups from "./components/AppGroups.vue";

    export const AccountManagement = Vue.extend({
        props: {

        },

        data: function() {
            return {

            }
        },

        created: function(): void {
            document.title = `Gex / Permissions`;
        },

        mounted: function(): void {

        },

        methods: {

        },

        computed: {

        },

        components: {
            InfoHover, Busy, Collapsible, ToggleButton,
            AppAccounts, AppGroups
        }
    });
    export default AccountManagement;
</script>
