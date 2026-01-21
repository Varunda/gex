<template>
    <div class="container">
        <h1 class="wt-header bg-light text-dark">Units</h1>

        <div class="alert alert-info text-center">
            This includes units that might not be enabled by default, such as extra unit or scavenger unit packs
        </div>

        <div>
            <input v-model="search" class="form-control" type="text" placeholder="Filter units" style="top: 0.5rem;"/>

            <toggle-button v-model="showAll">Show all</toggle-button>
        </div>

        <hr class="border"/>

        <div v-for="group in groupedUnits" :key="group.faction" :style="factionBackground(group.factionID)">

            <h2 :style="factionHeaderStyle(group.factionID)">{{ group.faction }}</h2>

            <div v-for="cat in group.categories" :key="cat.name" class="mb-3">
                <h3 class="text-center mb-0">{{ cat.name }}</h3>

                <div class="d-flex flex-wrap" style="gap: 0.75rem; justify-content: center;">
                    <div v-for="unit in cat.units" :key="unit.definitionName" class="text-center border border-dark position-sticky" style="border-radius: 0.5rem;" @click="dumpUnit(unit.definitionName)">
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

    .jsframe-titlebar-focused {
        font-size: var(--bs-body-font-size) !important;
        font-family: 'Atkitson Hyperlegible' !important;
        font-weight: normal !important;
        background: var(--bs-body-bg) !important;
    }
</style>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import * as jsf from "jsframe.js";

    import UnitPic from "components/app/UnitPic.vue";
    import ToggleButton from "components/ToggleButton";

    import { BarUnitApi } from "api/BarUnitApi";

    import { ApiBarUnit, BarUnit } from "model/BarUnit";

    import ColorUtils from "util/Color";
    import { FactionUtil } from "util/Faction";
    
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
                search: "" as string
            }
        },

        created: function(): void {
            document.title = "Gex / Units";
            this.loadUnits();
        },

        methods: {
            loadUnits: async function(): Promise<void> {
                this.units = Loadable.loading();
                this.units = await BarUnitApi.getAllDefinitions();
            },

            dumpUnit: function(defName: string): void {
                if (this.units.state != "loaded") {
                    return;
                }

                const unit: ApiBarUnit | undefined = this.units.data.find(iter => iter.definitionName == defName);
                if (unit == undefined) {
                    console.warn(`Units> failed to find unit to show in frame [defName=${defName}]`);
                    return;
                }

                const frame = new jsf.JSFrame();
                const win = frame.create({
                    title: `${unit.displayName}`,
                    left: 20, top: 20, width: 320, height: 220,
                    movable: true,
                    resizable: true,
                    style: {
                        backgroundColor: "var(--bs-tertiary-bg)",
                        titleBarColorDefault: "var(--bs-dark)"
                    },
                    html: makeUnitFrame(unit)
                });

                win.show();
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
            UnitPic, ToggleButton
        }
    });
    export default Units;

    const row: (label: string, value: string) => string = (label, value) => {
        return `
            <tr>
                <td>${label}</td>
                <td>${value}</td>
            </tr>
        `;
    };

    const makeUnitFrame: (unit: ApiBarUnit) => string = (unit) => {

        const category: CategoryType = getUnitCategories(unit);

        let extra: string = "";

        if (category == "commander") {

        } else if (category == "factory") {

        } else if (category == "con") {

        } else if (category == "eco") {

        } else if (category == "bot") {
            extra = makeBotUnitData(unit);
        } else if (category == "vehicle") {

        } else if (category == "air") {

        } else if (category == "sea") {

        } else if (category == "hover") {

        } else if (category == "defense") {

        } else if (category == "unknown") {

        }

        return `
            <div id="root-frame" class="text-light" style="font-family: 'Atkitson Hyperlegible'; font-size: var(--bs-body-font-size); overflow-y: scroll; height: 100%;">
                <a href="/unit/${unit.definitionName}" target="_blank" ref="nofollow">View full info</a>

                <table class="table table-sm table-hover">
                    <thead>
                        <tr>
                            <th></th>
                            <th></th>
                        </tr>
                    </thead>

                    <tbody>
                        ${row("Cost", `${unit.unit.metalCost} m / ${unit.unit.energyCost} E / ${unit.unit.buildTime} B`)}
                        ${row("Health", `${unit.unit.health}`)}
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