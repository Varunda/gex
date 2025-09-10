<template>
    <div class="container">

        <div v-if="mapList.state == 'idle'"></div>

        <div v-else-if="mapList.state == 'loading'">
            Loading...
        </div>

        <div v-else-if="mapList.state == 'loaded'">

            <a-table :entries="mapList" :show-filters="true" :paginate="false" default-sort-field="name" default-sort-order="asc">

                <a-col sort-field="name">
                    <a-header>
                        <b>Map</b>
                    </a-header>

                    <a-filter field="name" type="string" method="input"
                        :conditions="[ 'contains', 'equals' ]">
                    </a-filter>

                    <a-body v-slot="entry">
                        <a :href="'/map/' + encodeURIComponent(entry.fileName)">
                            {{ entry.name }}
                        </a>
                    </a-body>
                </a-col>

                <a-col sort-field="author">
                    <a-header>
                        <b>Author</b>
                    </a-header>

                    <a-filter field="author" type="string" method="input"
                        :conditions="[ 'contains', 'equals' ]">
                    </a-filter>

                    <a-body v-slot="entry">
                        {{ entry.author }}
                    </a-body>
                </a-col>

                <a-col sort-field="width">
                    <a-header>
                        <b>Width</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.width }}
                    </a-body>
                </a-col>

                <a-col sort-field="height">
                    <a-header>
                        <b>Height</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.height }}
                    </a-body>
                </a-col>

                <a-col>
                    <a-header>
                        <b>View</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <a :href="'/map/' + encodeURIComponent(entry.fileName)">
                            View
                        </a>
                    </a-body>
                </a-col>
            </a-table>

        </div>

        <div v-else-if="mapList.state == 'error'">
            <api-error :problem="mapList.problem"></api-error>
        </div>
    </div>

</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import ApiError from "components/ApiError";

    import "filters/BarGamemodeFilter";
    import "filters/BarFactionFilter";
    import "filters/MomentFilter";
    import "filters/LocaleFilter";
    import "filters/DefNameFilter";

    import { BarMap } from "model/BarMap";

    import { MapApi } from "api/MapApi";

    export const MapList = Vue.extend({
        props: {

        },

        data: function() {
            return {
                mapList: Loadable.idle() as Loading<BarMap[]>
            }
        },

        mounted: function(): void {
            document.title = "Gex / Maps";
            this.bind();
        },

        methods: {
            bind: async function(): Promise<void> {
                this.mapList = Loadable.loading();
                this.mapList = await MapApi.getAll();
            }
        },

        computed: {

        },

        components: {
            ApiError,
            ATable, AHeader, ABody, AFooter, AFilter, ACol
        }
    });
    export default MapList;

</script>