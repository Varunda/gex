<template>
    <div class="container-fluid remove-container-padding">

        <div class="container remove-container-padding">

            <h2 class="wt-header bg-light text-dark">
                Add unit to compare
            </h2>

            <div v-if="unitNames.state == 'loaded'">
                <input v-model="addUnitInput" @keyup.enter="addUnitWrapper" class="form-control" list="unit-names" autocomplete="on" @change="addUnitWrapper" @input="addUnitInputWrapper"/>

                <datalist id="unit-names" @select="addUnitWrapper">
                    <option v-for="unitName in unitNames.data" :key="unitName.definitionName" :value="unitName.definitionName">
                        {{ unitName.displayName }}
                    </option>
                </datalist>
            </div>

        </div>

        <hr class="border my-3"/>

        <div v-if="rootLoad.state == 'idle'"></div>

        <div v-else-if="rootLoad.state == 'loading'">
            <busy class="busy busy-sm"></busy>
            Loading...
        </div>

        <div v-else-if="rootLoad.state == 'loaded'" class="d-flex flex-wrap justify-content-center" style="gap: 0.5rem;">
            <div v-if="firstUnit == null">
                No units given
            </div>

            <template v-else>
                <unit :api-unit="firstUnit" @weaponindexchange="selectedWeaponIndexChanged" @close="removeUnit($event)" @updatedamagetypes="updateDamageTypes"
                    :damage-types="damageTypeSet"
                    :show-shield-data="showShieldData" :show-carrier-data="showCarrierData">
                </unit>

                <unit v-for="otherUnit in otherUnits" class="border-start ps-2" :key="otherUnit.definitionName" :api-unit="otherUnit"
                    @close="removeUnit($event)"
                    @updatedamagetypes="updateDamageTypes"
                    @changeshowshield="changeShowShieldData" @changeshowcarrier="changeShowCarrierData"
                    :show-shield-data="showShieldData" :show-carrier-data="showCarrierData" :damage-types="damageTypeSet"
                    :compare="firstUnit" :compare-weapon="firstUnitSelectedWeapon">
                </unit>
            </template>
        </div>

        <div v-else-if="rootLoad.state == 'error'">
            <api-error :problem="rootLoad.problem"></api-error>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import Busy from "components/Busy.vue";
    import ApiError from "components/ApiError";

    import Unit from "./components/Unit.vue";

    import { BarUnitApi } from "api/BarUnitApi";

    import { ApiBarUnit, BarUnit, BarUnitWeapon } from "model/BarUnit";
    import { BarUnitName } from "model/BarUnitName";

    export const BarUnitView = Vue.extend({
        props: {

        },

        data: function() {
            return {
                unitNames: Loadable.idle() as Loading<BarUnitName[]>,
                addUnitInput: "" as string,

                definitionNames: [] as string[],
                units: new Map() as Map<string, Loading<ApiBarUnit>>,
                rootLoad: Loadable.idle() as Loading<boolean>,

                showExtraEcoStats: false as boolean,

                selectedWeaponIndex: 0 as number,
                damageTypeSet: [] as string[],
                showShieldData: false as boolean,
                showCarrierData: false as boolean,
            }
        },

        mounted: function(): void {
            const parts: string[] = location.pathname.split("/");
            if (parts.length >= 3) {
                this.definitionNames = parts[2].split(",").filter(iter => iter != "");

                this.rootLoad = Loadable.idle();
                for (const defName of this.definitionNames) {
                    this.units.set(defName, Loadable.idle());
                }
            }

            this.loadUnitNames();
            this.loadData();
        },

        methods: {
            loadUnitNames: async function(): Promise<void> {
                this.unitNames = Loadable.loading();
                this.unitNames = await BarUnitApi.getAll();
            },

            loadData: async function(): Promise<void> {
                this.rootLoad = Loadable.loading();

                for (const defName of this.definitionNames) {
                    this.units.set(defName, Loadable.loading());

                    const load: Loading<ApiBarUnit> = await BarUnitApi.getByDefinitionName(defName);
                    this.units.set(defName, load);

                    if (load.state == "error") {
                        this.rootLoad = Loadable.rewrap(load);
                    }
                }

                if (this.rootLoad.state == "loading") {
                    this.rootLoad = Loadable.loaded(true);
                }

                this.$nextTick(() => {
                    this.updateUrl();
                });
            },

            updateUrl: function(): void {
                const url = new URL(location.href);

                if (this.firstUnit != null && this.definitionNames.length == 1) {
                    document.title = `Gex / Unit / ${this.firstUnit.displayName}`;
                    url.searchParams.set("name", this.firstUnit.displayName);
                } else {
                    document.title = `Gex / Units`;
                }

                // vetur is lying, .size does exist here!!!                                                             <------
                history.replaceState({ path: url.href }, "", `/unit/${this.definitionNames.join(",")}${(url.searchParams.size > 0 ? `?${url.searchParams.toString()}` : "")}`);
            },

            addUnitWrapper: function(): void {
                this.addUnit(this.addUnitInput);
                this.addUnitInput = "";
            },

            addUnitInputWrapper: function(ev: InputEvent): void {
                if (ev.inputType == "insertText") {
                    return;
                }

                const input = ev.target as HTMLInputElement;
                const val = input.value;

                console.log(`BarUnit> input wrapper ${val} ${ev.inputType}`);

                if (ev.inputType != "insertReplacementText") {
                    return;
                }
                this.addUnitWrapper();
            },

            addUnit: async function(defName: string): Promise<void> {
                console.log(`BarUnit> loading unit [defName=${defName}]`);

                if (defName == "") {
                    return;
                }

                if (this.definitionNames.indexOf(defName) != -1) {
                    console.warn(`BarUnit> not adding unit, already in definitionNames [defName=${defName}]`);
                    return;
                }

                const unit: Loading<ApiBarUnit> = await BarUnitApi.getByDefinitionName(defName);

                if (unit.state == "loaded") {
                    this.definitionNames.push(defName);

                    this.units.set(defName, unit);
                    this.updateUrl();
                }
            },

            removeUnit: function(defName: string): void {
                this.units.delete(defName);
                this.definitionNames = this.definitionNames.filter(iter => iter != defName);
                this.updateUrl();
            },

            selectedWeaponIndexChanged: function(wi: number): void {
                this.selectedWeaponIndex = wi;
            },

            changeShowShieldData: function(b: boolean): void {
                console.log(`BarUnit> show change shield data to ${b}`);
                this.showShieldData = this.showShieldData || b;
            },

            changeShowCarrierData: function(b: boolean): void {
                console.log(`BarUnit> show change carrier data to ${b}`);
                this.showCarrierData = this.showCarrierData || b;
            },

            updateDamageTypes: function(types: string[]): void {
                for (const t of types) {
                    if (t == "default") {
                        continue;
                    }

                    if (this.damageTypeSet.indexOf(t) == -1) {
                        console.log(`BarUnit> adding damage type to show [type=${t}]`);
                        this.damageTypeSet.push(t);
                    } else {
                        console.log(`BarUnit> skipping damage type to show [type=${t}]`);
                    }
                }
            }
        },

        computed: {
            firstUnit: function(): ApiBarUnit | null {
                if (this.definitionNames.length <= 0) {
                    return null;
                }

                const loading: Loading<ApiBarUnit> = this.units.get(this.definitionNames[0])!;
                if (loading.state == "loaded") {
                    return loading.data;
                }

                return null;
            },

            firstUnitSelectedWeapon: function(): BarUnitWeapon {
                if (this.firstUnit == null) {
                    return new BarUnitWeapon();
                }

                if (this.selectedWeaponIndex >= this.firstUnit.unit.weapons.length) {
                    return new BarUnitWeapon();
                }

                return this.firstUnit.unit.weapons[this.selectedWeaponIndex];
            },

            otherUnits: function(): ApiBarUnit[] {
                if (this.definitionNames.length <= 1) {
                    return [];
                }

                const defNames: string[] = this.definitionNames.slice(1);
                const ret: ApiBarUnit[] = [];

                for (const defName of defNames) {
                    const unit: Loading<ApiBarUnit> | undefined = this.units.get(defName);
                    if (unit != undefined && unit.state == "loaded") {
                        ret.push(unit.data);
                    } else {
                        console.warn(`BarUnit> missing unit '${defName}', ${unit?.state ?? "<null>"}`);
                    }
                }

                return ret;
            }
        },

        components: {
            Busy, ApiError,
            Unit
        }
    });
    export default BarUnitView;
</script>