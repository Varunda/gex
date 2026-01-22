<template>
    <div class="container">
        <h1 class="wt-header bg-light text-dark">Units</h1>

        <div v-if="showAll == true" class="alert alert-info text-center">
            This includes units that might not be enabled by default, such as extra unit or scavenger unit packs
        </div>

        <div>
            <input v-model="search" class="form-control" type="text" placeholder="Filter units" style="top: 0.5rem;"/>

            <toggle-button v-model="showAll">Show all</toggle-button>
        </div>

        <hr class="border"/>

        <div v-if="units.state == 'idle'"></div>

        <div v-else-if="units.state == 'loading'" class="text-center">
            Loading...
            <busy class="busy busy-sm"></busy>
        </div>

        <div v-else-if="units.state == 'loaded'">
            <div v-for="group in groupedUnits" :key="group.faction" :style="factionBackground(group.factionID)">
                <h2 :style="factionHeaderStyle(group.factionID)">{{ group.faction }}</h2>

                <div v-for="cat in group.categories" :key="cat.name" class="mb-3">
                    <h3 class="text-center mb-0">{{ cat.name }}</h3>

                    <div class="d-flex flex-wrap" style="gap: 0.75rem; justify-content: center;">
                        <div v-for="unit in cat.units" :key="unit.definitionName" class="zoom-hover text-center border border-dark position-sticky" style="border-radius: 0.5rem;" @click="dumpUnit(unit.definitionName)">
                            <div class="text-outline image-parent">
                                {{ unit.displayName }}
                            </div>

                            <unit-pic :name="unit.definitionName" :size="128" style="border-radius: 0.5rem;"></unit-pic>
                        </div>
                    </div>

                    <hr class="border mt-3 m-1"/>
                </div>
            </div>
        </div>

        <div v-else-if="units.state == 'error'">
            <api-error :error="units.problem"></api-error>
        </div>

    </div>
</template>

<style>
    .image-parent {
        width: 100%;
        text-align: center;
        position: absolute;
        bottom: 0;
        background-color: #00000066;
        padding-right: 0.25rem;
        padding-left: 0.25rem;
        border-radius: 0 0 0.5rem 0.5rem;
    }

    .zoom-hover:hover {
        outline: 3px solid #ffffff;
        background-size: 110%;
        transition: background-size 0.2s ease-in;
    }

    .jsframe-titlebar-focused {
        font-size: var(--bs-body-font-size) !important;
        font-family: 'Atkitson Hyperlegible' !important;
        font-weight: normal !important;
        background: var(--bs-light-bg-subtle) !important;
    }

    .jsframe-titlebar-default {
        font-size: var(--bs-body-font-size) !important;
        font-family: 'Atkitson Hyperlegible' !important;
        font-weight: normal !important;
        background: var(--bs-body-bg) !important;
    }
</style>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    // @ts-ignore - no types are available for this library :(
    import * as jsf from "jsframe.js";

    import EventBus from "EventBus";
    (window as any).EventBus = EventBus;

    import UnitPic from "components/app/UnitPic.vue";
    import ToggleButton from "components/ToggleButton";
    import Busy from "components/Busy.vue";
    import ApiError from "components/ApiError";

    import { BarUnitApi } from "api/BarUnitApi";

    import { ApiBarUnit, BarUnit } from "model/BarUnit";

    import ColorUtils from "util/Color";
    import { FactionUtil } from "util/Faction";
    import LocaleUtil from "util/Locale";
    
    class GroupedUnits {
        public faction: string = "";
        public factionID: number = 0;
        public categoryMap: Map<string, ApiBarUnit[]> = new Map();
        public categories: UnitCategory[] = [];
        public units: ApiBarUnit[] = [];

        public constructor(name: string, factionID: number) {
            this.factionID = factionID;
            this.faction = name;
        }
    }

    class UnitCategory {
        public name: string = "";
        public order: number = 0;
        public units: ApiBarUnit[] = [];

        public constructor(name: string, order: number) {
            this.name = name;
            this.order = order;
        }
    }

    type CategoryType = "commander" | "factory" | "con" | "eco" | "bot" | "vehicle" | "air" | "sea" | "hover" | "defense" | "unknown";

    const CategoryNameMap = new Map([
        ["commander", "Commander"],
        ["factory", "Factory"],
        ["con", "Constructor"],
        ["eco", "Economy"],
        ["bot", "Bot"],
        ["vehicle", "Vehicle"],
        ["air", "Air"],
        ["sea", "Naval"],
        ["hover", "Hover"],
        ["defense", "Static defense"],
        ["unknown", "Other"]
    ]);

    const CategoryOrderMap = new Map([
        ["commander", 0],
        ["factory", 1],
        ["con", 2],
        ["eco", 3],
        ["bot", 4],
        ["vehicle", 5],
        ["air", 6],
        ["sea", 7],
        ["hover", 8],
        ["defense", 9],
        ["unknown", 10]
    ]);

    const getUnitCategories: (unit: ApiBarUnit) => CategoryType = (unit) => {
        if (unit.definitionName == "armcom" || unit.definitionName == "corcom" || unit.definitionName == "legcom") {
            return "commander";
        }

        if (unit.unit.speed == 0 && unit.unit.buildOptions.length > 0) {
            return "factory";
        }
        if (unit.unit.speed > 0 && unit.unit.buildOptions.length > 0) {
            return "con";
        }

        if (unit.unit.movementClass.startsWith("TANK") || unit.unit.movementClass.startsWith("HTANK")
            || unit.unit.movementClass.startsWith("ATANK") || unit.unit.movementClass.startsWith("MTANK")) {

            return "vehicle";
        } else if (unit.unit.movementClass.startsWith("BOT") || unit.unit.movementClass.startsWith("HABOT")
            || unit.unit.movementClass.startsWith("HBOT") || unit.unit.movementClass.startsWith("ABOT")
            || unit.unit.movementClass.startsWith("TBOT") || unit.unit.movementClass.startsWith("SBOT")
            || unit.unit.movementClass.startsWith("HTBOT") || unit.unit.movementClass.startsWith("VBOT")) {

            return "bot";
        } else if (unit.unit.canFly == true) {
            return "air";
        } else if (unit.unit.movementClass.startsWith("BOAT") || unit.unit.movementClass.startsWith("UBOAT")) {
            return "sea";
        } else if (unit.unit.movementClass.startsWith("HOVER") || unit.unit.movementClass.startsWith("HHOVER")) {
            return "hover";
        }

        if (unit.unit.speed == 0 && unit.unit.weapons.length > 0) {
            return "defense";
        }

        // some static defense include a bit of storage, so this check is intentionally after
        if (unit.unit.energyProduced != 0 || unit.unit.energyUpkeep < 0 || unit.unit.metalExtractor != 0
            || unit.unit.extractsMetal == true || unit.unit.windGenerator > 0 || unit.unit.energyConversionCapacity != 0
            || unit.unit.tidalGenerator > 0 || unit.unit.energyStorage > 0 || unit.unit.metalStorage > 0) {
            return "eco";
        }

        return "unknown";
    };

    // get all units that can be built from another unit, and any children of that unit (recursively)
    // this is used to only show the units that with the default settings can be built
    const generateBuildTree:(root: string, units: ApiBarUnit[]) => ApiBarUnit[] = (root, units) => {
        const map: Map<string, ApiBarUnit> = new Map();
        for (const unit of units) {
            map.set(unit.definitionName, unit);
        }

        const queue: string[] = [root];
        const found: Map<string, ApiBarUnit> = new Map();

        let iter: string | undefined = queue.shift();
        while (iter != undefined) {
            const unit: ApiBarUnit | undefined = map.get(iter);
            if (unit == undefined) {
                throw `Units> missing unit ${iter} from map`;
            }

            for (const opt of unit.unit.buildOptions) {
                const optDef: ApiBarUnit | undefined = map.get(opt);
                if (optDef == undefined) {
                    console.warn(`Units> missing unit from build option [unit=${iter}] [opt=${opt}]`);
                    continue;
                }

                if (found.has(opt) == false) {
                    found.set(opt, optDef);
                    queue.push(opt);
                }
            }

            iter = queue.shift();
        }

        return Array.from(found.values());
    }

    export const Units = Vue.extend({
        props: {

        },

        data: function() {
            return {
                units: Loadable.idle() as Loading<ApiBarUnit[]>,
                showAll: false as boolean,
                search: "" as string,
                frames: new jsf.JSFrame() as any,

                windows: new Map() as Map<string, any>,
            }
        },

        created: function(): void {
            document.title = "Gex / Units";
            this.loadUnits();

            EventBus.$on("open-frame", (defName: string) => {
                console.log(`Units> opening frame for ${defName}`);
                this.dumpUnit(defName);
            });
        },

        methods: {
            loadUnits: async function(): Promise<void> {
                this.units = Loadable.loading();
                this.units = await BarUnitApi.getAllDefinitions();
            },

            dumpUnit: function(defName: string): void {
                const windowName: string = defName;

                if (this.windows.has(windowName) == true) {
                    console.log(`Units> re-using window [defName=${defName}]`);
                    this.windows.get(windowName)!.requestFocus();
                    return;
                }

                if (this.units.state != "loaded") {
                    return;
                }

                const unit: ApiBarUnit | undefined = this.units.data.find(iter => iter.definitionName == defName);
                if (unit == undefined) {
                    console.warn(`Units> failed to find unit to show in frame [defName=${defName}]`);
                    return;
                }

                const win = this.frames.create({
                    name: windowName,
                    title: `${unit.displayName} (${unit.definitionName})`,
                    left: 20, top: 20, width: 640, height: 400,
                    movable: true,
                    resizable: true,
                    style: {
                        backgroundColor: "var(--bs-tertiary-bg)",
                        titleBarColorDefault: "var(--bs-dark)"
                    },
                    html: makeUnitFrame(unit, this.units.data)
                });

                win.on("closeButton", "click", (_win: any, ev: any) => {
                    console.log(`Units> closing window ${_win.property.name}`);
                    this.windows.get(_win.property.name)?.closeFrame();
                    this.windows.delete(_win.property.name);
                });

                win.show();
                this.windows.set(windowName, win);
            },

            factionHeaderStyle: function(factionID: number) {
                return {
                    "background-color": ColorUtils.getFactionColor(factionID),
                    "padding": "0.5rem",
                    "border-radius": "0.5rem"
                };
            },

            factionBackground: function(factionID: number) {
                return {
                    "background-color": `${ColorUtils.getFactionColor(factionID)}66`,
                    "padding": "0.5rem",
                    "margin-bottom": "2rem"
                }
            }
        },

        computed: {

            groupedUnits: function(): GroupedUnits[] {
                if (this.units.state != "loaded") {
                    return [];
                }

                const sets: GroupedUnits[] = [];
                const arm: GroupedUnits = new GroupedUnits("Armada", FactionUtil.ARMADA);
                const cor: GroupedUnits = new GroupedUnits("Cortex", FactionUtil.CORTEX);
                const leg: GroupedUnits = new GroupedUnits("Legion", FactionUtil.LEGION);

                // grouping based on build tree or defname
                if (this.showAll == true) {
                    const raptor: GroupedUnits = new GroupedUnits("Raptor", 4);
                    const scav: GroupedUnits = new GroupedUnits("Scavenger", 5);
                    const misc: GroupedUnits = new GroupedUnits("Misc", 6);

                    for (const unit of this.units.data) {
                        if (this.search != "" && unit.displayName.toLowerCase().indexOf(this.search.toLowerCase()) == -1) {
                            continue;
                        }

                        if (unit.definitionName.startsWith("arm")) {
                            arm.units.push(unit);
                        } else if (unit.definitionName.startsWith("cor")) {
                            cor.units.push(unit);
                        } else if (unit.definitionName.startsWith("leg")) {
                            leg.units.push(unit);
                        } else if (unit.definitionName.startsWith("raptor")) {
                            raptor.units.push(unit);
                        } else if (unit.definitionName.startsWith("scav")) {
                            scav.units.push(unit);
                        } else {
                            misc.units.push(unit);
                        }
                    }

                    sets.push(arm, cor, leg, raptor, scav, misc);
                } else {
                    arm.units = generateBuildTree("armcom", this.units.data);
                    cor.units = generateBuildTree("corcom", this.units.data);
                    leg.units = generateBuildTree("legcom", this.units.data);

                    sets.push(arm, cor, leg);
                }

                // processing
                for (const set of sets) {
                    if (this.search != "") {
                        set.units = set.units.filter(iter => {
                            return iter.displayName.toLowerCase().indexOf(this.search.toLowerCase()) > -1;
                        });
                    }

                    for (const unit of set.units) {
                        const cat: CategoryType = getUnitCategories(unit);

                        if (set.categoryMap.has(cat) == false) {
                            set.categoryMap.set(cat, []);
                        }

                        set.categoryMap.get(cat)!.push(unit);
                    }

                    for (const iter of set.categoryMap.entries()) {
                        const unitCat: UnitCategory = new UnitCategory(
                            CategoryNameMap.get(iter[0]) ?? `<unknown ${iter[0]}>`,
                            CategoryOrderMap.get(iter[0]) ?? 0
                        );
                        unitCat.units = iter[1].sort((a, b) => {
                            return a.unit.metalCost - b.unit.metalCost || a.displayName.localeCompare(b.displayName);
                        });
                        set.categories.push(unitCat);
                    }

                    set.categories.sort((a, b) => {
                        return a.order - b.order;
                    });

                    set.units.sort((a, b) => {
                        return a.displayName.localeCompare(b.displayName);
                    });
                }

                return sets;
            }

        },

        components: {
            UnitPic, ToggleButton, Busy, ApiError
        }
    });
    export default Units;

    /**
     * helper function to make a row in a table (used for making the unit windows)
     * @param label first column in the row of the table
     * @param value second column in the row of the table
     */
    const row: (label: string, value: string) => string = (label, value) => {
        return `
            <tr>
                <td><b>${label}</b></td>
                <td>${value}</td>
            </tr>
        `;
    };

    const _D: (val: number, prec?: number) => string = (val: number, prec: number = 2) => {
        return LocaleUtil.locale(val, prec ?? 2);
    }

    /**
     * function to make the html for the unit window for a specific unit
     * @param unit unit the html is being made for
     * @param units list of all units (used for build options)
     */
    const makeUnitFrame: (unit: ApiBarUnit, units: ApiBarUnit[]) => string = (unit, units) => {

        const defMap: Map<string, ApiBarUnit> = new Map();
        for (const iter of units) {
            defMap.set(iter.definitionName, iter);
        }

        const category: CategoryType = getUnitCategories(unit);

        let extra: string = "";

        if (unit.unit.speed > 0) {
            extra += row("Speed", `${_D(unit.unit.speed)} | ${_D(unit.unit.acceleration)} accel | ${_D(unit.unit.turnRate)}°/s turns | ${unit.unit.movementClass} type`);
        }

        if (unit.unit.buildDistance > 0) {
            extra += row("Build range", _D(unit.unit.buildDistance, 0));
        }
        if (unit.unit.buildPower > 0) {
            extra += row("Build power", _D(unit.unit.buildPower, 0));
        }

        if (unit.unit.canResurrect == true) {
            extra += row("Can rez", "true");
        }

        if (unit.unit.buildOptions.length > 0) {
            const names: string[] = unit.unit.buildOptions.map(iter => {
                const name = defMap.get(iter)?.displayName ?? `&gt;missing ${iter}&lt;`;
                return `<a href="javascript:void(0)" onclick="EventBus.$emit('open-frame', '${iter}')">${name}</a>`;
            });

            extra += row("Builds", names.join(", "));
        }

        if (unit.unit.energyProduced > 0) {
            extra += row("E/s", `+${_D(unit.unit.energyProduced, 0)}`);
        }
        if (unit.unit.extractsMetal == true) {
            extra += row("Mex?", "true");
        }
        if (unit.unit.windGenerator > 0) {
            extra += row("Wind?", "true");
        }
        if (unit.unit.energyUpkeep < 0) {
            extra += row("E/s", `+${_D(Math.abs(unit.unit.energyUpkeep), 0)}`);
        }
        if (unit.unit.energyUpkeep > 0) {
            extra += row("E/s", `-${_D(unit.unit.energyUpkeep, 0)}`);
        }
        if (unit.unit.energyStorage > 0) {
            extra += row("E store", _D(unit.unit.energyStorage, 0));
        }
        if (unit.unit.metalStorage > 0) {
            extra += row("M store", _D(unit.unit.metalStorage, 0));
        }
        if (unit.unit.energyConversionCapacity > 0) {
            const metalPerEng = unit.unit.energyConversionCapacity * unit.unit.energyConversionEfficiency;
            extra += row("E conv", `${_D(metalPerEng, 2)}m per ${_D(unit.unit.energyConversionCapacity, 0)}E`);
        }

        if (unit.unit.radarDistance > 0) {
            extra += row("Radar", _D(unit.unit.radarDistance, 0));
        }
        if (unit.unit.sonarDistance > 0) {
            extra += row("Sonar", _D(unit.unit.sonarDistance, 0));
        }

        if (unit.unit.weapons.length > 0) {
            let i = 1;
            for (const weapon of unit.unit.weapons) {
                const wep = weapon.weaponDefinition;
                extra += row(`Weapon ${i}`,
                    `<b>${(weapon.count > 1 ? `[${weapon.count}x] ` : ``)}${wep.name}</b><br/>
                    DPS: ${_D(wep.defaultDps, 0)} (${_D(wep.defaultDamage, 0)} dmg, ${_D(wep.reloadTime, 2)}s reload)<br/>
                    Range: ${_D(wep.range, 0)} (${_D(wep.areaOfEffect)} splash)
                `);
                ++i;
            }
        }

        let faction: string = "Other";
        if (unit.definitionName.startsWith("arm")) {
            faction = "Armada";
        } else if (unit.definitionName.startsWith("cor")) {
            faction = "Cortex";
        } else if (unit.definitionName.startsWith("leg")) {
            faction = "Legion";
        } else if (unit.definitionName.startsWith("raptor")) {
            faction = "Raptor";
        } else if (unit.definitionName.startsWith("scav")) {
            faction = "Scav";
        }

        return `
            <div id="root-frame" class="text-light p-2" style="font-family: 'Atkitson Hyperlegible'; font-size: var(--bs-body-font-size); overflow-y: scroll; height: 100%;">
                <div class="d-flex" style="gap: 0.5rem;">
                    <img src="/image-proxy/UnitPic?defName=${encodeURIComponent(unit.definitionName)}" width="96" height="96">

                    <div class="d-flex flex-column">
                        <h2 class="d-inline mb-0">${unit.displayName}</h2>
                        <span>${unit.description}</span>
                        <a href="/unit/${unit.definitionName}" target="_blank" ref="nofollow">View full info</a>
                    </div>
                </div>

                <table class="table table-sm table-hover">
                    <tbody>
                        ${row("Faction", faction)}
                        ${row("Cost", `${_D(unit.unit.metalCost, 0)} m | ${_D(unit.unit.energyCost, 0)} E | ${_D(unit.unit.buildTime, 0)} B`)}
                        ${row("Health", `${_D(unit.unit.health, 0)}`)}
                        ${row("Vision", `${_D(unit.unit.sightDistance, 0)} | ${_D(unit.unit.airSightDistance, 0)} (air)`)}
                        ${extra}
                    </tbody>
                </table>
            </div>
        `;
    }

    const makeBotUnitData: (unit: ApiBarUnit) => string = (unit) => {
        return `
            ${row("Speed", `${unit.unit.speed}`)}
            ${row("Type", unit.unit.movementClass)}
        `;
    }

</script>