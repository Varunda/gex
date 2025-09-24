
<template>

    <div class="flex-grow-0" style="flex-basis: 1; max-width: 420px;">

        <div class="d-flex text-center justify-content-center mb-3">
            <div class="me-3">
                <img :src="'/image-proxy/UnitPic?defName=' + ApiUnit.definitionName" height="86" width="86" :title="ApiUnit.displayName" style="border-radius: 0.5rem;">
            </div>

            <div>
                <h1 class="text-start mb-0 d-flex">
                    <span class="flex-grow-1" style="overflow-x: hidden; overflow-y: hidden; text-overflow: clip; text-wrap: nowrap; max-width: 260px;" :title="ApiUnit.displayName">
                        {{ ApiUnit.displayName }}
                    </span>
                    <span>
                        <button class="btn close border" @click="emitClose" :title="'Close unit card for ' + ApiUnit.displayName">&times;</button>
                    </span>
                </h1>
                <h3 class="text-start" style="overflow-x: hidden; text-overflow: clip; text-wrap: nowrap; max-width: 300px;" :title="ApiUnit.description">
                    {{ ApiUnit.description }}
                </h3>
            </div>

            <div class="me-2">
            </div>
        </div>

        <table class="table table-sm table-sticky-header table-hover">
            <thead>
                <tr class="table-light">
                    <th>Field</th>
                    <th>Value</th>
                    <th v-if="compare">
                        Diff to {{ compare.displayName }}
                    </th>
                </tr>
            </thead>

            <tbody>
                <tr>
                    <td :colspan="colspan">
                        <h4 class="text-center mt-1 mb-1">Basic info</h4>
                    </td>
                </tr>

                <tr>
                    <td>Definition name</td>
                    <td :colspan="compareUnit == null ? 2 : 3">{{ unit.definitionName }}</td>
                </tr>
                <tr is="Cell" name="Health" field="health" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Metal cost" field="metalCost" :unit="unit" :compare="compareUnit" :low="true"> m</tr>
                <tr is="Cell" name="Energy cost" field="energyCost" :unit="unit" :compare="compareUnit" :low="true"> E</tr>
                <tr is="Cell" name="Build time" field="buildTime" :unit="unit" :compare="compareUnit" :low="true"> B</tr>
                <tr is="Cell" name="Speed" field="speed" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Acceleration" field="acceleration" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Deceleration" field="deceleration" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Turn rate" field="turnRate" :unit="unit" :compare="compareUnit"> deg/sec</tr>
                <tr is="Cell" name="Size X" field="sizeX" :unit="unit" :compare="compareUnit" :d="0"></tr>
                <tr is="Cell" name="Size Z" field="sizeZ" :unit="unit" :compare="compareUnit" :d="0"></tr>
            </tbody>
        </table>

        <select v-model.number="selectedWeaponIndex" class="form-select">
            <option v-for="(wep, index) in unit.weapons" :key="wep.weaponDefinition.definitionName" :value="index">
                {{ wep.weaponDefinition.name }}
            </option>
        </select>

        <div class="d-flex text-center justify-content-center mb-3">
            <div>
                <h3 class="text-start mb-0" style="overflow-y: hidden; text-overflow: clip; text-wrap: nowrap;" :title="selectedWeapon.name">
                    {{ selectedWeapon.name.slice(0, 25) }}<span v-if="selectedWeapon.name.length > 25">...</span>
                </h3>
            </div>
        </div>

        <table class="table table-sm table-hover">
            <tbody>
                <tr is="Cell" name="Targets" field="targetCategory" :unit="selectedWeaponEntry" :compare="CompareWeapon"></tr>
                <tr is="Cell" name="Count" field="count" :unit="selectedWeaponEntry" :compare="CompareWeapon" :d="0">x</tr>
                <tr is="Cell" name="DPS" field="defaultDps" :unit="selectedWeapon" :compare="compareWeaponDef"> dmg / sec</tr>
                <tr is="Cell" name="Damage (per burst)" field="defaultBurstDamage" :unit="selectedWeapon" :compare="compareWeaponDef"> dmg</tr>
                <tr is="Cell" name="Damage (per shot)" field="defaultDamage" :unit="selectedWeapon" :compare="compareWeaponDef"> dmg</tr>

                <tr v-for="dmgType in DamageTypes" :key="dmgType"
                    is="DamageType" :type="dmgType" :unit="selectedWeapon.damages" :compare="compareWeaponDef == null ? null : compareWeaponDef.damages" :targets="selectedWeaponEntry.targetCategory"> dmg</tr>

                <tr is="Cell" name="Fire rate" field="fireRate" :unit="selectedWeapon" :compare="compareWeaponDef"> / sec</tr>
                <tr is="Cell" name="Reload (s)" field="reloadTime" :unit="selectedWeapon" :compare="compareWeaponDef" :low="true"> sec</tr>
                <tr is="Cell" name="Projectiles" field="projectiles" :unit="selectedWeapon" :compare="compareWeaponDef" :d="0"></tr>
                <tr is="Cell" name="Sweep fire" field="sweepFire" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Range" field="range" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Splash" field="areaOfEffect" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Speed" field="velocity" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Edge effectiveness" field="edgeEffectiveness" :unit="selectedWeapon" :compare="compareWeaponDef" :d="2"></tr>
                <tr is="Cell" name="Burst" field="burst" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Burst rate" field="burstRate" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Impulse factor" field="impulseFactor" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Energy/shot" field="energyPerShot" :unit="selectedWeapon" :compare="compareWeaponDef" :low="true"> E / shot</tr>
                <tr is="Cell" name="Metal/shot" field="metalPerShot" :unit="selectedWeapon" :compare="compareWeaponDef" :low="true"> m / shot</tr>
                <tr is="Cell" name="Is EMP?" field="isParalyzer" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="EMP time" field="paralyzerTime" :unit="selectedWeapon" :compare="compareWeaponDef"> sec</tr>
                <tr is="Cell" name="Stockpile" field="isStockpile" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Stockpile reload" field="stockpileTime" :unit="selectedWeapon" :compare="compareWeaponDef" :low="true"> sec</tr>
                <tr is="Cell" name="Stockpile limit" field="stockpileLimit" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Chain damage" field="chainForkDamage" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Chain max units" field="chainMaxUnits" :unit="selectedWeapon" :compare="compareWeaponDef" :d="0"></tr>
                <tr is="Cell" name="Chain range" field="chainForkRange" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Area damage" field="timedAreaDamage" :unit="selectedWeapon" :compare="compareWeaponDef"> dmg / sec</tr>
                <tr is="Cell" name="Area range" field="timedAreaRange" :unit="selectedWeapon" :compare="compareWeaponDef"></tr>
                <tr is="Cell" name="Area duration" field="timedAreaTime" :unit="selectedWeapon" :compare="compareWeaponDef"> sec</tr>

                <template v-if="ShowShieldData || selectedWeapon.shieldData != null || (compareWeaponDef && compareWeaponDef.shieldData != null)">
                    <tr is="Header" name="Shield data" :colspan="colspan"></tr>

                    <tr is="Cell" name="Power" field="power" :unit="selectShieldData" :compare="compareShieldData"> hp</tr>
                    <tr is="Cell" name="Power recharge" field="powerRegen" :unit="selectShieldData" :compare="compareShieldData"> hp / sec</tr>
                    <tr is="Cell" name="Power recharge cost" field="powerRegenEnergy" :unit="selectShieldData" :compare="compareShieldData" :low="true"> E / sec</tr>
                    <tr is="Cell" name="Starting power" field="startingPower" :unit="selectShieldData" :compare="compareShieldData"> hp</tr>
                    <tr is="Cell" name="Radius" field="radius" :unit="selectShieldData" :compare="compareShieldData"></tr>
                    <tr is="Cell" name="Repulser" field="repulser" :unit="selectShieldData" :compare="compareShieldData"></tr>
                    <tr is="Cell" name="Energy upkeep" field="energyUpkeep" :unit="selectShieldData" :compare="compareShieldData" :low="true"> E / sec</tr>
                </template>

                <template v-if="ShowCarrierData || selectedWeapon.carriedUnit != null || (compareWeaponDef && compareWeaponDef.carriedUnit != null)">
                    <tr is="Header" name="Carried unit" :colspan="colspan"></tr>

                    <tr>
                        <td>Unit</td>
                        <td>{{ selectUnitCarried.definitionName }}</td>
                        <td v-if="compare != null"></td>
                    </tr>

                    <tr is="Cell" name="Max units" field="maxUnits" :unit="selectUnitCarried" :compare="compareCarriedUnit"></tr>
                    <tr is="Cell" name="Spawn rate" field="spawnRate" :unit="selectUnitCarried" :compare="compareCarriedUnit" :low="true"></tr>
                    <tr is="Cell" name="Metal cost" field="metalCost" :unit="selectUnitCarried" :compare="compareCarriedUnit" :low="true"> m / unit</tr>
                    <tr is="Cell" name="Energy cost" field="energyCost" :unit="selectUnitCarried" :compare="compareCarriedUnit" :low="true"> E / unit</tr>
                    <tr is="Cell" name="Control radius" field="controlRadius" :unit="selectUnitCarried" :compare="compareCarriedUnit"></tr>
                    <tr is="Cell" name="Engagemenet range" field="engagementRange" :unit="selectUnitCarried" :compare="compareCarriedUnit"></tr>
                    <tr is="Cell" name="Spawn surface" field="spawnSurface" :unit="selectUnitCarried" :compare="compareCarriedUnit"></tr>
                    <tr is="Cell" name="Decay rate" field="decayRate" :unit="selectUnitCarried" :compare="compareCarriedUnit" :low="true"> hp / sec</tr>
                    <tr is="Cell" name="Can dock?" field="enableDocking" :unit="selectUnitCarried" :compare="compareCarriedUnit"></tr>
                    <tr is="Cell" name="Docking armor" field="dockingArmor" :unit="selectUnitCarried" :compare="compareCarriedUnit"></tr>
                    <tr is="Cell" name="Docking heal rate" field="dockingHealRate" :unit="selectUnitCarried" :compare="compareCarriedUnit"> hp / sec</tr>
                    <tr is="Cell" name="Docking threshold" field="dockToHealThreshold" :unit="selectUnitCarried" :compare="compareCarriedUnit"> hp</tr>
                    <tr is="Cell" name="Docking helper speed" field="dockingHelperSpeed" :unit="selectUnitCarried" :compare="compareCarriedUnit"></tr>
                </template>

            </tbody>

        </table>

        <table class="table table-sm table-hover">
            <tbody>
                <tr is="Header" name="Resource info" :colspan="colspan"></tr>
                <tr is="Cell" name="Metal produced" field="metalProduced" :unit="unit" :compare="compareUnit"> m / sec</tr>
                <tr is="Cell" name="Metal storage" field="metalStorage" :unit="unit" :compare="compareUnit"> m</tr>
                <tr is="Cell" name="Metal extractor?" field="metalExtractor" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Energy produced" field="energyProduced" :unit="unit" :compare="compareUnit"> E / sec</tr>
                <tr is="Cell" name="Energy storage" field="energyStorage" :unit="unit" :compare="compareUnit"> E</tr>
                <tr is="Cell" name="Energy upkeep" field="energyUpkeep" :unit="unit" :compare="compareUnit" :low="true"> E / sec</tr>
                <tr is="Cell" name="Wind generation" field="windGenerator" :unit="unit" :compare="compareUnit"> E / sec (max)</tr>
                <tr is="Cell" name="Tidal generator" field="tidalGenerator" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="E conv capacity" field="energyConversionCapacity" :unit="unit" :compare="compareUnit"> E</tr>
                <tr is="Cell" name="E conv efficiency" field="energyConversionEfficiency" :unit="unit" :compare="compareUnit"> M per E</tr>

                <tr is="Header" name="Vision" :colspan="colspan"></tr>
                <tr is="Cell" name="LoS" field="sightDistance" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Air LoS" field="airSightDistance" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Radar" field="radarDistance" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Sonar" field="sonarDistance" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Radar Jamming" field="jamDistance" :unit="unit" :compare="compareUnit"></tr>

                <tr is="Header" name="Build info" :colspan="colspan"></tr>
                <tr is="Cell" name="Can build?" field="isBuilder" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Can assist?" field="canAssist" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Can reclaim?" field="canReclaim" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Can repair?" field="canRepair" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Can restore?" field="canRestore" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Can resurrect?" field="canResurrect" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Build range" field="buildDistance" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Build power" field="buildPower" :unit="unit" :compare="compareUnit"></tr>

                <tr is="Header" name="Transport info" :colspan="colspan"></tr>
                <tr is="Cell" name="Transport capacity" field="transportCapacity" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Transport size" field="transportSize" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Transport mass" field="transportMass" :unit="unit" :compare="compareUnit"></tr>

                <tr is="Header" name="Misc." :colspan="colspan"></tr>
                <tr is="Cell" name="Cloak cost still" field="cloakCostStill" :unit="unit" :compare="compareUnit" :low="true"> E / sec</tr>
                <tr is="Cell" name="Cloak cost moving" field="cloakCostMoving" :unit="unit" :compare="compareUnit" :low="true"> E / sec</tr>
                <tr is="Cell" name="EMP mult." field="paralyzeMultiplier" :unit="unit" :compare="compareUnit" :low="true"></tr>
                <tr is="Cell" name="Self-D damage" field="selfDestructDamage" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Self-D timer" field="selfDestructCountdown" :unit="unit" :compare="compareUnit" :low="true">s</tr>
                <tr is="Cell" name="Self-D EMP?" field="selfDestructWeaponDefinition.isParalyzer" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Self-D EMP time" field="selfDestructWeaponDefinition.paralyzerTime" :unit="unit" :compare="compareUnit">s</tr>
                <tr is="Cell" name="Self-D range" field="selfDestructWeaponDefinition.areaOfEffect" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Self-D falloff" field="selfDestructWeaponDefinition.edgeEffectiveness" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Explode damage" field="explodeAsDamage" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Explode EMP?" field="explodeAsWeaponDefinition.isParalyzer" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Explode EMP time" field="explodeAsWeaponDefinition.paralyzerTime" :unit="unit" :compare="compareUnit">s</tr>
                <tr is="Cell" name="Explode range" field="explodeAsWeaponDefinition.areaOfEffect" :unit="unit" :compare="compareUnit"></tr>
                <tr is="Cell" name="Explode falloff" field="explodeAsWeaponDefinition.edgeEffectiveness" :unit="unit" :compare="compareUnit"></tr>
            </tbody>
        </table>

    </div>
</template>

<style scoped>

    .no-wrap {
        text-wrap: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

</style>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import InfoHover from "components/InfoHover.vue";
    import UnitIcon from "components/app/UnitIcon.vue";

    import { ApiBarUnit, BarShieldData, BarUnit, BarUnitCarrierData, BarUnitWeapon, BarWeaponDefinition } from "model/BarUnit";

    import LocaleUtil from "util/Locale";

    const getPathValue = (obj: any, path: string): any => {
        const parts: string[] = path.split(".");
        let objPart = obj;
        let key: string | undefined = parts.shift();
        while (key != undefined) {
            if (objPart instanceof Map) {
                objPart = (objPart as Map<any,any>).get(key);
            } else {
                objPart = objPart[key];
            }
            key = parts.shift();
        }
        return objPart;
    };

    const Header = Vue.extend({
        props: {
            name: { type: String, required: true },
            colspan: { type: Number, required: true }
        },

        template: `
            <tr>
                <td :colspan="colspan">
                    <h4 class="text-center mt-3 mb-1">{{ name }}</h4>
                </td>
            </tr>
        `
    });

    const DamageType = Vue.extend({
        props: {
            type: { type: String, required: true },
            targets: { type: String, required: true },
            unit: { type: Map as PropType<Map<string, number>>, required: true },
            compare: { type: Map as PropType<Map<string, number> | null>, required: false },
        },

        methods: {
            n(num: number): string {
                return LocaleUtil.locale(num);
            },
        },

        computed: {

            name: function(): string {
                return `Damage to ${this.type == "commanders" ? "comms" : this.type}`;
            },

            canHit: function(): boolean {
                if (this.targets == "NOTSUB" && this.type == "subs") {
                    if (this.type == "subs") {
                        return false;
                    }
                }
                if (this.targets == "SURFACE") {
                    if (this.type == "vtol") {
                        return false;
                    }
                }

                return true;
            },

            value: function(): number {
                if (this.canHit == false) {
                    return 0;
                }

                return this.unit.get(this.type) ?? this.unit.get("default") ?? 0;
            },

            cvalue: function(): any {
                if (this.canHit == false) {
                    return 0;
                }

                if (!this.compare) {
                    return 0;
                }

                return this.compare.get(this.type) ?? this.compare.get("default") ?? 0;
            },

            styleHigher: function() {
                return {
                    "color": "var(--bs-info-text-emphasis)"
                }
            },

            styleLower: function()  {
                return {
                    "color": "var(--bs-info-text-emphasis)"
                }
            }
        },

        template: `
            <tr :data-is-diff="cvalue != value ? 'true' : 'false'" class="no-wrap">
                <td class="no-wrap">{{ name }}</td>
                <td class="no-wrap">
                    {{ n(value) }}<slot name="default"></slot>
                </td>
                <td v-if="compare" class="no-wrap">
                    <span v-if="cvalue > value">
                        (<span :style="styleLower">-{{ n(cvalue - value) }}</span><slot name="default"></slot>)
                    </span>
                    <span v-else-if="cvalue < value">
                        (<span :style="styleHigher">+{{ n(value - cvalue) }}</span><slot name="default"></slot>)
                    </span>
                    <span v-else-if="cvalue == value" class="text-muted">
                        (<span class="text-muted">same</span>)
                    </span>
                </td>
            </tr>
        `

    });

    const Cell = Vue.extend({
        props: {
            name: { type: String, required: true },
            field: { type: String, required: true },
            unit: { type: Object as PropType<BarUnit>, required: true },
            compare: { type: Object as PropType<BarUnit | null>, required: false },
            low: { type: Boolean, required: false, default: false },
            d: { type: Number, required: false, default: undefined },
            fallback: { required: false }
        },

        methods: {
            n(num: number): string {
                return LocaleUtil.locale(num, this.d);
            },
        },

        computed: {
            type: function(): string {
                const valueType: string = typeof this.value;

                if (valueType == "string") {
                    return "string";
                } else if (valueType == "number") {
                    return "number";
                } else if (valueType == "boolean") {
                    return "boolean";
                }

                console.error(`Unit> unhandled type of field [type=${valueType}] [field=${this.field}] [value=${this.value}]`);
                return "unknown";
            },

            value: function(): any {
                const v = getPathValue(this.unit, this.field);

                if (v == undefined && this.fallback != undefined) {
                    return this.fallback;
                }
                return v;
            },

            defaultcvalue: function(): any {
                if (this.type == "number") {
                    return 0;
                } else if (this.type == "string") {
                    return "";
                } else if (this.type == "boolean") {
                    return false;
                }
                return "";
            },

            cvalue: function(): any {
                if (!this.compare) {
                    return this.defaultcvalue;
                }

                return getPathValue(this.compare, this.field);
            },

            styleHigher: function() {
                return {
                    "color": this.low == true ? "var(--bs-warning-text-emphasis)" : "var(--bs-info-text-emphasis)"
                }
            },

            styleLower: function()  {
                return {
                    "color": this.low == false ? "var(--bs-warning-text-emphasis)" : "var(--bs-info-text-emphasis)"
                }
            }
        },

        template: `
            <tr :data-is-diff="cvalue != value ? 'true' : 'false'" class="no-wrap">
                <td class="no-wrap">{{ name }}</td>
                <td class="no-wrap">
                    <span v-if="type == 'number'">
                    {{ n(value) }}<slot name="default"></slot>
                    </span>
                    <span v-else-if="type == 'boolean'">
                        {{ value == true ? "Yes" : "No" }}
                    </span>
                    <span v-else-if="type == 'string'">
                        {{ value }}
                    </span>
                </td>
                <td v-if="compare" class="no-wrap">
                    <span v-if="type == 'number'">
                        <span v-if="cvalue > value">
                            (<span :style="styleLower">-{{ n(cvalue - value) }}</span><slot name="default"></slot>)
                        </span>
                        <span v-else-if="cvalue < value">
                            (<span :style="styleHigher">+{{ n(value - cvalue) }}</span><slot name="default"></slot>)
                        </span>
                        <span v-else-if="cvalue == value" class="text-muted">
                            (<span class="text-muted">same</span>)
                        </span>
                    </span>

                    <span v-else-if="type == 'boolean' || type == 'string'">
                        <span v-if="cvalue != value">
                            (<span style="color: var(--bs-info-text-emphasis)">diff</span>)
                        </span>
                        <span v-else>
                            (<span class="text-muted">same</span>)
                        </span>
                    </span>
                </td>
            </tr>
        `
    });

    export const Unit = Vue.extend({
        props: {
            ApiUnit: { type: Object as PropType<ApiBarUnit>, required: true },
            compare: { type: Object as PropType<ApiBarUnit | null>, required: false },
            CompareWeapon: { type: Object as PropType<BarUnitWeapon | null>, required: false },
            ShowShieldData: { type: Boolean, required: false, default: false },
            ShowCarrierData: { type: Boolean, required: false, default: false },
            DamageTypes: { type: Array as PropType<string[]>, required: true }
        },

        data: function() {
            return {
                selectedWeaponIndex: 0 as number
            }
        },

        mounted: function(): void {
            this.$emit("changeshowshield", this.selectedWeapon.shieldData != null);
            this.$emit("changeshowcarrier", this.selectedWeapon.carriedUnit != null);
            this.$emit("updatedamagetypes", Array.from(this.selectedWeapon.damages.keys()));
        },

        methods: {
            n(num: number): string {
                return LocaleUtil.locale(num);
            },

            emitClose: function(): void {
                this.$emit("close", this.unit.definitionName);
            }
        },

        computed: {
            selectedWeaponEntry: function(): BarUnitWeapon {
                if (this.selectedWeaponIndex >= this.unit.weapons.length) {
                    const wep: BarUnitWeapon = new BarUnitWeapon();
                    wep.weaponDefinition.name = "<no weapon>";
                    return wep;
                }

                return this.unit.weapons[this.selectedWeaponIndex];
            },

            selectedWeapon: function(): BarWeaponDefinition {
                return this.selectedWeaponEntry.weaponDefinition;
            },

            selectShieldData: function(): BarShieldData {
                return this.selectedWeapon.shieldData ?? new BarShieldData();
            },

            selectUnitCarried: function(): BarUnitCarrierData {
                return this.selectedWeapon.carriedUnit ?? new BarUnitCarrierData();
            },

            unit: function(): BarUnit {
                return this.ApiUnit.unit;
            },

            colspan: function(): number {
                return this.compare == null ? 2 : 3;
            },

            compareUnit: function(): BarUnit | null {
                return this.compare?.unit ?? null;
            },

            compareWeaponDef: function(): BarWeaponDefinition | null {
                return this.CompareWeapon?.weaponDefinition ?? null;
            },

            compareShieldData: function(): BarShieldData | null {
                if (this.compare == null) {
                    return null;
                }

                return this.CompareWeapon?.weaponDefinition.shieldData ?? new BarShieldData();
            },

            compareCarriedUnit: function(): BarUnitCarrierData | null {
                if (this.compare == null) {
                    return null;
                }

                return this.CompareWeapon?.weaponDefinition.carriedUnit ?? new BarUnitCarrierData();
            }
        },

        watch: {
            selectedWeaponIndex: function(): void {
                this.$emit("weaponindexchange", this.selectedWeaponIndex);

                this.$emit("changeshowshield", this.selectedWeapon.shieldData != null);
                this.$emit("changeshowcarrier", this.selectedWeapon.carriedUnit != null);
                this.$emit("updatedamagetypes", Array.from(this.selectedWeapon.damages.keys()));
            }
        },

        components: {
            InfoHover, Cell, Header, UnitIcon, DamageType
        }
    })
    export default Unit;
</script>