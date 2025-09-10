
<template>
    <div>
        <collapsible header-text="Unit defs" :show="false">

            <button class="btn btn-primary" @click="loadUnitDefChanges()">
                Load unit definition changes
            </button>

            <div v-if="showUnitDiff == true" class="d-flex flex-wrap justify-content-between" style="gap: 0.75rem;">

                <div v-for="unit in unitDiffs" :key="unit.game.definitionName">
                    <h4>
                        {{ unit.game.disambiguatedName }}
                    </h4>

                    <div v-if="unit.default == null">
                        This unit is not a default unit!
                    </div>

                    <div v-else>
                        Changed unit
                    </div>

                    <table class="table table-sm">
                        <thead>
                            <tr>
                                <th>Field</th>
                                <th>Default</th>
                                <th>Game</th>
                            </tr>
                        </thead>

                        <tbody>
                            <tr is="DiffRow" name="Health" field="health" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Metal cost" field="metalCost" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Energy cost" field="energyCost" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Build time" field="buildTime" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Build power" field="buildPower" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Metal made" field="metalMake" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Metal storage" field="metalStorage" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Energy made" field="energyProduction" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Energy upkeep" field="energyUpkeep" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Extracts metal" field="extractsMetal" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Energy storage" field="energyStorage" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Wind generator" field="windGenerator" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Tidal generator" field="tidalGenerator" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="E conv capacity" field="energyConversionCapacity" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="E conv ratio" field="energyConversionEfficiency" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Sight distance" field="sightDistance" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Air sight distance" field="airSightDistance" :def="unit.default" :game="unit.game"></tr>
                            <tr is="DiffRow" name="Radar distance" field="radarDistance" :def="unit.default" :game="unit.game"></tr>
                        </tbody>
                    </table>
                </div>
            </div>

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

            <div v-if="debug" style="overflow-x: scroll;" >
                <table class="table table-sm">
                    <thead>
                        <tr>
                            <th v-for="(key, index) in Object.keys(UnitDefs[0])" :key="key" :class="{ 'sticky-column': index == 0 }">
                                {{ key }}
                            </th>
                        </tr>
                    </thead>

                    <tbody>
                        <tr v-for="unitDef in shownUnits" :key="unitDef.definitionID">
                            <td v-for="(key, index) in Object.keys(unitDef)" :key="unitDef + '-' + key" :class="{ 'sticky-column': index == 0 }">
                                {{ unitDef[key] }}
                            </td>
                        </tr>
                    </tbody>

                </table>
            </div>
        </collapsible>
    </div>
    
</template>

<style scoped>
    .sticky-column {
        position: sticky;
        width: 33ch;
        left: 0;
    }

</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading } from "Loading";

    import Collapsible from "components/Collapsible.vue";
    import ToggleButton from "components/ToggleButton";

    import { GameEventUnitDef } from "model/GameEventUnitDef";
    import { GameOutput } from "model/GameOutput";
    import { ApiBarUnit, BarUnit } from "model/BarUnit";

    import { BarUnitApi } from "api/BarUnitApi";

    import "filters/CompactFilter";

    export const DiffRow = Vue.extend({
        props: {
            name: { type: String, required: true },
            field: { type: String, required: true },
            def: { type: Object as PropType<ApiBarUnit>, required: false },
            game: { type: Object as PropType<GameEventUnitDef>, required: true },
        },

        computed: {

            defaultValue: function(): any {
                if (this.def) {
                    return (this.def as any)[this.field];
                }
                return null;
            },

            gameValue: function(): any {
                return (this.game as any)[this.field];
            }
        },

        template: `
            <tr v-if="def == null || defaultValue != gameValue">
                <td>{{ name }}</td>

                <td>
                    <span v-if="def != null">
                        {{ defaultValue }}
                    </span>
                    <span v-else>
                        --
                    </span>
                </td>

                <td>
                    {{ gameValue }}
                </td>
            </tr>
        `
    });

    const barUnitToDef = (unit: ApiBarUnit): GameEventUnitDef => {
        const def: GameEventUnitDef = new GameEventUnitDef();

        def.disambiguatedName = unit.displayName;

        def.metalCost =  unit.unit.metalCost;
        def.energyCost = unit.unit.energyCost;

        def.health = unit.unit.health;
        def.speed = unit.unit.speed;
        def.sizeX = unit.unit.sizeX;
        def.sizeZ = unit.unit.sizeZ;

        def.buildTime = unit.unit.buildTime;
        def.unitGroup =  "";
        def.buildPower = unit.unit.buildPower;
        def.metalMake = unit.unit.metalProduced;
        def.isMetalExtractor =  unit.unit.metalExtractor;
        def.extractsMetal = unit.unit.extractsMetal ? 1 : 0;
        def.metalStorage = unit.unit.metalStorage;
        def.energyStorage = unit.unit.energyStorage;
        def.windGenerator = unit.unit.windGenerator;
        def.tidalGenerator = unit.unit.tidalGenerator;
        def.energyProduction = unit.unit.energyProduced;
        def.energyUpkeep = unit.unit.energyUpkeep;
        def.energyConversionCapacity = unit.unit.energyConversionCapacity;
        def.energyConversionEfficiency = unit.unit.energyConversionEfficiency;
        def.sightDistance = unit.unit.sightDistance;
        def.airSightDistance = unit.unit.airSightDistance;
        def.radarDistance = unit.unit.radarDistance;
        def.attackRange = 0;

        def.isCommander = false;
        def.isReclaimer = unit.unit.canReclaim;
        def.isFactory = false;
        def.weaponCount = 0;

        return def;
    }

    type UnitDiffPair = {
        default: GameEventUnitDef | null;
        game: GameEventUnitDef;
    }
    
    export const UnitDefView = Vue.extend({
        props: {
            UnitDefs: { type: Array as PropType<GameEventUnitDef[]>, required: true },
            output: {type: Object as PropType<GameOutput>, required: true }
        },

        data: function() {
            return {
                onlySeen: true as boolean,
                seen: new Set() as Set<number>,
                debug: false as boolean,

                defaultUnitDefinitions: new Map() as Map<string, ApiBarUnit>,

                showUnitDiff: false as boolean,
                unitDiffs: [] as UnitDiffPair[]
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
            },

            loadUnitDefChanges: async function(): Promise<void> {
                this.showUnitDiff = true;

                const unitDefDict: Map<number, GameEventUnitDef> = new Map();
                for (const entry of this.UnitDefs) {
                    unitDefDict.set(entry.definitionID, entry);
                }

                for (const definitionID of this.seen) {
                    const unitDef: GameEventUnitDef | undefined = unitDefDict.get(definitionID);
                    if (unitDef == undefined) {
                        console.warn(`UnitDefView> missing unit definition [definitionID=${definitionID}]`);
                        continue;
                    }

                    if (this.defaultUnitDefinitions.has(unitDef.definitionName)) {
                        continue;
                    }

                    const unit: Loading<ApiBarUnit> = await BarUnitApi.getByDefinitionName(unitDef.definitionName);
                    if (unit.state != "loaded") {
                        console.warn(`UnitDefView> failed to load definition [definitionName=${unitDef.definitionName}]`);
                        continue;
                    }

                    this.defaultUnitDefinitions.set(unitDef.definitionName, unit.data);
                }

                this.unitDiffs = this.getUnitDiffs();
            },

            getUnitDiffs: function(): UnitDiffPair[] {
                const diff: UnitDiffPair[] = [];

                if (this.showUnitDiff == false) {
                    return diff;
                }

                const unitDefDict: Map<string, GameEventUnitDef> = new Map();
                for (const entry of this.UnitDefs) {
                    unitDefDict.set(entry.definitionName, entry);

                    if (this.defaultUnitDefinitions.has(entry.definitionName) == false && this.seen.has(entry.definitionID)) {
                        diff.push({
                            default: null,
                            game: entry
                        });
                    }
                }

                for (const entry of this.defaultUnitDefinitions) {
                    const key: string = entry[0];
                    const value: ApiBarUnit = entry[1];

                    const unitDef: GameEventUnitDef | undefined = unitDefDict.get(key);
                    if (unitDef == undefined) {
                        continue;
                    }

                    const u: GameEventUnitDef = barUnitToDef(value);

                    if (u.health != unitDef.health
                        || u.metalCost !=  unitDef.metalCost
                        || u.energyCost != unitDef.energyCost
                        || u.speed != unitDef.speed
                        || u.sizeX != unitDef.sizeX
                        || u.sizeZ != unitDef.sizeZ
                        || u.buildTime != unitDef.buildTime
                        || u.buildPower != unitDef.buildPower
                        || u.metalMake != unitDef.metalMake
                        || u.isMetalExtractor !=  unitDef.isMetalExtractor
                        || u.metalStorage != unitDef.metalStorage
                        || u.energyStorage != unitDef.energyStorage
                        || u.windGenerator != unitDef.windGenerator
                        || u.tidalGenerator != unitDef.tidalGenerator
                        || u.energyProduction != unitDef.energyProduction
                        || u.energyUpkeep != unitDef.energyUpkeep
                        || u.energyConversionCapacity != unitDef.energyConversionCapacity
                        || u.energyConversionEfficiency != unitDef.energyConversionEfficiency
                        || u.sightDistance != unitDef.sightDistance
                        || u.airSightDistance != unitDef.airSightDistance
                        || u.radarDistance != unitDef.radarDistance
                    ) {
                        diff.push({
                            default: u,
                            game: unitDef
                        });
                    }
                }
                
                return diff;
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
            Collapsible, ToggleButton,
            DiffRow
        }

    });
    export default UnitDefView;
</script>