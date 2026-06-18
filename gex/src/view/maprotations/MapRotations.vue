<template>
    <div>
        <h2 class="wt-header">
            Map rotations
        </h2>

        <div class="border rounded p-2 mb-4">
            <toggle-button v-model="hideAllMapRotations">Hide rotations with all maps</toggle-button>

            <toggle-button v-model="filter.hideCertified">Hide 'certified' rotation</toggle-button>

            <div>
                <label>Filter rotation name</label>
                <input v-model="filter.rotationName" type="text" class="form-control">
            </div>

            <div>
                <label>Filter by map in rotation</label>
                <input v-model="filter.mapName" type="text" class="form-control">
            </div>
        </div>

        <div v-if="rotations.state == 'idle'"></div>

        <div v-else-if="rotations.state == 'loading'">
            Loading...
        </div>

        <div v-else-if="rotations.state == 'loaded'">
            <div v-for="rotation in filteredRotations" :key="rotation.name" class="mb-5">
                <h3 class="wt-header">
                    <div>
                        {{ rotation.name }}
                    </div>

                    <h6 class="d-inline-block">
                        Command to set this rotation:
                        <code>rotationtype map;{{ rotation.name }}</code>
                    </h6>
                </h3>

                <div class="d-flex flex-wrap">
                    <div v-for="map in rotation.maps" :key="map" class="mb-3 text-start" style="height: 32px; width: 416px;">
                        <span v-if="map == '.*'">
                            All maps
                        </span>

                        <template v-else>
                            <div class="img-overlay max-width"></div>

                            <div class="position-absolute max-width img-map-parent" style="z-index: 0; font-size: 0">
                                <div :style="mapBackground(map)" class="img-map-side img-map-left"></div>
                                <div :style="mapBackground(map)" class="img-map-center"></div>
                                <div :style="mapBackground(map)" class="img-map-side img-map-right"></div>
                            </div>

                            <div style="z-index: 10; position: relative; top: 50%; transform: translateY(-50%); left: 20px;">
                                <div class="d-inline-flex flex-column align-items-start" style="vertical-align: top;">
                                    <h4 class="mb-0">
                                        <a :href="'/mapname/' + map" class="text-white" style="text-decoration: none;">
                                            {{ map }}
                                        </a>
                                    </h4>
                                </div>
                            </div>
                        </template>
                    </div>
                </div>
            </div>
        </div>


    </div>
</template>

<style scoped>
    .max-width {
        max-width: calc(100vw - (var(--bs-gutter-x) * 0.5)) !important;
    }

    .img-map-parent {
        max-width: 100vw;
        white-space: nowrap;
        overflow: hidden;
    }

    .img-overlay {
        width: 416px;
        height: 32px;
        position: absolute;
        background: #0005;
        background: linear-gradient(to right, #0005, var(--bs-body-bg));
        z-index: 1;
    }

    .img-map-side {
        display: inline-block;
        width: 116px;
        height: 32px;
        transform: scaleX(-1);
        background-repeat: no-repeat !important;
        background-size: 150% !important;
    }

    .img-map-left {
        background-position: left -36px !important;
    }

    .img-map-center {
        display: inline-block;
        width: 172px;
        height: 32px;
        background-position: center -36px !important;
        background-size: cover !important;
        background-repeat: no-repeat !important;
    }

    .img-map-right {
        background-position: right -36px !important;
    }
</style>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { BarMapRotationApi } from "api/BarMapRotationApi";

    import { BarMapRotation } from "model/BarMapRotation";

    import ToggleButton from "components/ToggleButton";

    export const MapRotations = Vue.extend({
        props: {

        },

        data: function() {
            return {
                rotations: Loadable.idle() as Loading<BarMapRotation[]>,

                filter: {
                    rotationName: "" as string,
                    mapName: "" as string,
                    hideCertified: true as boolean
                },

                hideAllMapRotations: true as boolean
            }
        },

        mounted: function(): void {
            this.bind();
        },

        methods: {
            bind: async function(): Promise<void> {
                this.rotations = Loadable.loading();
                this.rotations = await BarMapRotationApi.getAll();
            },

            mapBackground: function(map: string): any {
                return {
                    "background": `url("/image-proxy/MapNameBackground?map=${map}&size=texture-thumb")`
                };
            },
        },

        computed: {

            filteredRotations: function(): BarMapRotation[] {
                if (this.rotations.state != "loaded") {
                    return [];
                }

                return this.rotations.data.filter(iter => {
                    return (this.hideAllMapRotations == false || (iter.maps.length > 1 && iter.maps[0] != '.*'))
                        && (this.filter.hideCertified == false || iter.name != 'certified')
                        && (this.filter.rotationName == "" || iter.name.indexOf(this.filter.rotationName) > -1)
                        && (this.filter.mapName == "" || (iter.maps.find(m => m.indexOf(this.filter.mapName) > -1)) != undefined);
                });
            }

        },

        components: {
            ToggleButton
        }

    });
    export default MapRotations;

</script>