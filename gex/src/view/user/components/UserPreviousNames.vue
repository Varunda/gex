<template>
    <div>
        <h2 class="wt-header border-0">Previous names</h2>

        <span>
            Only public PvP games are used to display previous names
        </span>

        <a-table :entries="previousNames" :show-filters="false" default-sort-field="" default-sort-order="asc" row-padding="compact" :hover="true" :default-page-size="10" :hide-paginate="true">
            <a-col>
                <a-header>
                    <b>User name</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.userName }}
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Timestamp</b>
                    <info-hover text="When Gex first saw a game with this name"></info-hover>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.timestamp | moment }}
                </a-body>
            </a-col>
        </a-table>
    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";

    import { BarMatch } from "model/BarMatch";
    import { BarUser } from "model/BarUser";
    import { UserPreviousName } from "model/UserPreviousName";

    export const UserPreviousNames = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            matches: { type: Array as PropType<BarMatch[]>, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {

        },

        computed: {
            previousNames: function(): Loading<UserPreviousName[]> {
                return Loadable.loaded(this.user.previousNames);
            },
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
        }
    });
    export default UserPreviousNames;
</script>