
<template>
    <div>
        <collapsible header-text="Unit defs" :show="false">

            <toggle-button v-model="onlySeen">Show only used units</toggle-button>

            <toggle-button v-model="debug">debug view</toggle-button>

            <table class="table">
                <thead>
                    <tr>
                        <td>id</td>
                        <td>def name</td>
                        <td>name</td>
                        <td>unit group</td>
                        <td>speed</td>
                        <td>weapon count</td>
                        <td>cost</td>
                    </tr>
                </thead>

                <tbody>
                    <tr v-for="unitDef in shownUnits" :key="unitDef.definitionID">
                        <td>
                            {{ unitDef.definitionID }}
                            <img :src="'/image-proxy/UnitIcon?defName=' + unitDef.definitionName" width="24" height="24">
                        </td>
                        <td>{{ unitDef.definitionName }}</td>
                        <td>{{ unitDef.name }}</td>
                        <td>{{ unitDef.unitGroup }}</td>
                        <td>{{ unitDef.speed }}</td>
                        <td>{{ unitDef.weaponCount }}</td>
                        <td>{{ unitDef.metalCost | compact }} m / {{ unitDef.energyCost | compact }} e / {{ unitDef.buildTime | compact }} B</td>
                    </tr>
                </tbody>
            </table>

            <table class="table table-sm" v-if="debug">
                <thead>
                    <tr>
                        <th v-for="key in Object.keys(UnitDefs[0])" :key="key">
                            {{ key }}
                        </th>
                    </tr>
                </thead>

                <tbody>
                    <tr v-for="unitDef in shownUnits" :key="unitDef.definitionID">
                        <td v-for="key in Object.keys(unitDef)" :key="unitDef + '-' + key">
                            {{ unitDef[key] }}
                        </td>
                    </tr>
                </tbody>

            </table>
        </collapsible>
    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import Collapsible from "components/Collapsible.vue";
    import ToggleButton from "components/ToggleButton";

    import { GameEventUnitDef } from "model/GameEventUnitDef";
    import { GameOutput } from "model/GameOutput";

    import "filters/CompactFilter";

    export const UnitDefView = Vue.extend({
        props: {
            UnitDefs: { type: Array as PropType<GameEventUnitDef[]>, required: true },
            output: {type: Object as PropType<GameOutput>, required: true }
        },

        data: function() {
            return {
                onlySeen: true as boolean,
                seen: new Set() as Set<number>,
                debug: false as boolean
            }
        },

        created: function(): void {
            this.findSeen();
        },

        methods: {

            findSeen: function(): void {
                for (const ev of this.output.unitsCreated) {
                    this.seen.add(ev.definitionID);
                }

                for (const ev of this.output.unitsKilled) {
                    this.seen.add(ev.definitionID);
                }
            }
        },

        computed: {

            shownUnits: function(): GameEventUnitDef[] {
                if (this.onlySeen == false) {
                    return this.UnitDefs;
                }
                return this.UnitDefs.filter(iter => this.seen.has(iter.definitionID));
            }

        },

        components: {
            Collapsible, ToggleButton
        }

    });
    export default UnitDefView;
</script>