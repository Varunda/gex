

<template>
    <div>
        <h2 class="wt-header">Recent matches</h2>

        <a-table :entries="matches"
            :show-filters="true"
            default-sort-field="startTime" default-sort-order="desc">

            <a-col sort-field="startTime">
                <a-header>
                    <b>Timestamp</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.startTime | moment }}
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Map</b>
                </a-header>

                <a-filter field="map" type="string" method="dropdown"
                    :conditions="[ 'equals' ]">
                </a-filter>

                <a-body v-slot="entry">
                    {{ entry.map }}
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Teams</b>
                </a-header>

                <a-body v-slot="entry">
                    <span v-if="isFFA(entry)">
                        {{ entry.allyTeams.length }} way FFA
                    </span>
                    <span v-else>
                        {{ entry.allyTeams.map(iter => iter.playerCount).join(" v ") }}
                    </span>
                </a-body>
            </a-col>

            <a-col sort-field="durationMs">
                <a-header>
                    <b>Duration</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.durationMs / 1000 | mduration }}
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>Link</b>
                </a-header>

                <a-body v-slot="entry">
                    <a :href="'/match/' + entry.id">
                        View
                    </a>
                </a-body>
            </a-col>
        </a-table>
    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";

    import { BarMatch } from "model/BarMatch";

    export const UserMatches = Vue.extend({
        props: {
            data: { type: Array as PropType<BarMatch[]>, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {
            isFFA: function(match: BarMatch): boolean {
                return match.allyTeams.length > 2 && Math.max(...match.allyTeams.map(iter => iter.playerCount)) == 1;
            },
        },

        computed: {
            matches: function(): Loading<BarMatch[]> {
                return Loadable.loaded(this.data);
            }

        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            InfoHover, ToggleButton 
        }
    });
    export default UserMatches;
</script>