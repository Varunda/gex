<template>
    <div>
        <h2 class="wt-header bg-light text-dark">
            Units created
        </h2>

        <div class="alert alert-info text-center">
            <div>
                <b>This data is only an estimate, and is not exact</b>
            </div>

            <div>
                This data only includes public PvP games after Gex started, and only when a game is fully processed. 
                See what conditions Gex processes a game <a href="/faq">here</a>
            </div>
        </div>

        <a-table :entries="unitsMade" :show-filters="true" default-sort-field="count" default-sort-order="desc" :paginate="true" :default-page-size="10">

            <a-col sort-field="unitName">
                <a-header>
                    <b>Unit name</b>
                </a-header>

                <a-filter field="unitName" type="string" method="input"
                    :conditions="[ 'contains', 'equals' ]">
                </a-filter>

                <a-body v-slot="entry">
                    <unit-icon :name="entry.definitionName" :color="getUnitColor(entry.definitionName)"></unit-icon>
                    {{ entry.unitName }}
                </a-body>
            </a-col>

            <a-col sort-field="count">
                <a-header>
                    <b>Created</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.count | locale(0) }}
                </a-body>
            </a-col>
        </a-table>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import { BarUser } from "model/BarUser";
    import { BarUserUnitsMade } from "model/BarUserUnitsMade";

    import UnitIcon from "components/app/UnitIcon.vue";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";

    import "filters/LocaleFilter";

    import ColorUtils from "util/Color";

    export const UserUnitsMade = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true }
        },

        methods: {
            getUnitColor: function(defName: string): string | null {
                if (defName.startsWith("arm")) {
                    return ColorUtils.Armada;
                } else if (defName.startsWith("cor")) {
                    return ColorUtils.Cortex;
                } else if (defName.startsWith("leg")) {
                    return ColorUtils.Legion;
                } else {
                    return null;
                }
            }
        },

        computed: {
            unitsMade: function(): Loading<BarUserUnitsMade[]> {
                return Loadable.loaded(this.user.unitsMade);
            }
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            UnitIcon
        }

    });
    export default UserUnitsMade;
</script>