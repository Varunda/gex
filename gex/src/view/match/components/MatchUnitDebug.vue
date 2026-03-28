
<template>
    <collapsible header-text="Unit debug" :show="false">

        <a-table :entries="unitDebug"
            :show-filters="true"
            default-sort-field="createdAt" default-sort-order="asc">

            <a-col sort-field="unitID">
                <a-header>
                    <b>Unit ID</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.unitID }}
                </a-body>
            </a-col>

            <a-col>
                <a-header>
                    <b>unit</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.definitionName }}
                </a-body>
            </a-col>

            <a-col sort-field="teamID">
                <a-header>
                    <b>team id</b>
                </a-header>

                <a-filter field="teamID" type="number" method="dropdown"
                    :conditions="[ 'equals' ]">
                </a-filter>

                <a-body v-slot="entry">
                    {{ entry.teamID }}
                </a-body>
            </a-col>

            <a-col sort-field="createdAt">
                <a-header>
                    <b>created at</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.createdAt }}
                </a-body>
            </a-col>

            <a-col sort-field="damageDealt">
                <a-header>
                    <b>dmg done</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.damageDealt }}
                </a-body>
            </a-col>

            <a-col sort-field="damageTaken">
                <a-header>
                    <b>dmg taken</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.damageTaken }}
                </a-body>
            </a-col>

            <a-col sort-field="metalMade">
                <a-header>
                    <b>M made</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.metalMade }}
                </a-body>
            </a-col>

            <a-col sort-field="metalUsed">
                <a-header>
                    <b>M used</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.metalUsed }}
                </a-body>
            </a-col>

            <a-col sort-field="energyMade">
                <a-header>
                    <b>E made</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.energyMade }}
                </a-body>
            </a-col>

            <a-col sort-field="energyUsed">
                <a-header>
                    <b>E used</b>
                </a-header>

                <a-body v-slot="entry">
                    {{ entry.energyUsed }}
                </a-body>
            </a-col>

        </a-table>

    </collapsible>

</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol, ARank, ATableType } from "components/ATable";
    import UnitIcon from "components/app/UnitIcon.vue";

    import { Collapsible } from "components/Collapsible.vue";
    import { InfoHover } from "components/InfoHover.vue";

    import { GameEventFactoryUnitCreated } from "model/GameEventFactoryUnitCreated";
    import { GameEventUnitDef } from "model/GameEventUnitDef";
    import { GameEventUnitKilled } from "model/GameEventUnitKilled";
    import { GameEventUnitPosition } from "model/GameEventUnitPosition";

    import { BarMatch } from "model/BarMatch";
    import { GameOutput } from "model/GameOutput";


    class UnitDebug {
        public unitID: number = 0;
        public definitionName: string = "";
        public definition: GameEventUnitDef | null = null;

        public teamID: number = 0;

        public createdAt: number = 0;
        public createdBy: number | null = null;
        public destroyedAt: number | null = null;
        
        public unitsKilledCount: number = 0;
        public damageDealt: number = 0;
        public damageTaken: number = 0;
        public metalKilled: number = 0;

        public energyMade: number = 0;
        public energyUsed: number = 0;
        public metalMade: number = 0;
        public metalUsed: number = 0;

        public unitsKilled: GameEventUnitKilled[] = [];
        public unitPositions: GameEventUnitPosition[] = [];
        public unitCreatedFactory: GameEventFactoryUnitCreated[] = [];
    }

    export const MatchUnitDebug = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: { type: Object as PropType<GameOutput>, required: true },
        },

        data: function() {
            return {
                debug: new Map() as Map<number, UnitDebug>,
                arr: [] as UnitDebug[]
            }
        },

        mounted: function(): void {
            this.makeData();
            this.$forceUpdate();
        },

        methods: {

            getOrMake(unitID: number): UnitDebug {
                let d: UnitDebug | undefined = this.debug.get(unitID);
                if (d != undefined) {
                    return d;
                }

                d = new UnitDebug();
                d.unitID = unitID;
                this.debug.set(d.unitID, d);

                return d;
            },
            
            makeData: function(): void {
                this.debug.clear();

                for (const ev of this.output.unitsCreated) {
                    const d: UnitDebug = this.getOrMake(ev.unitID);
                    d.createdAt = ev.frame;
                    d.teamID = ev.teamID;
                    d.definitionName = ev.definitionName;
                    d.definition = this.output.unitDefinitions.get(ev.definitionID) ?? null;
                    this.debug.set(d.unitID, d);
                }

                for (const ev of this.output.factoryUnitCreated) {
                    const d: UnitDebug = this.getOrMake(ev.factoryUnitID);
                    d.unitCreatedFactory.push(ev);

                    const other: UnitDebug = this.getOrMake(ev.unitID);
                    other.createdBy = ev.factoryUnitID;
                    this.debug.set(d.unitID, d);
                }

                for (const ev of this.output.unitsKilled) {
                    const d: UnitDebug = this.getOrMake(ev.unitID);
                    d.destroyedAt = ev.frame;

                    if (ev.attackerID != null) {
                        const attacker: UnitDebug = this.getOrMake(ev.attackerID);
                        attacker.unitsKilled.push(ev);
                        this.debug.set(attacker.unitID, attacker);
                    }
                    this.debug.set(d.unitID, d);
                }

                for (const ev of this.output.unitResources) {
                    const d: UnitDebug = this.getOrMake(ev.unitID);
                    d.energyMade = ev.energyMade;
                    d.energyUsed = ev.energyUsed;
                    d.metalMade = ev.metalMade;
                    d.metalUsed = ev.metalUsed;
                    this.debug.set(d.unitID, d);
                }

                for (const ev of this.output.unitDamage) {
                    const d: UnitDebug = this.getOrMake(ev.unitID);
                    d.damageDealt = ev.damageDealt;
                    d.damageTaken = ev.damageTaken;
                    this.debug.set(d.unitID, d);
                }

                this.arr = Array.from(this.debug.values());
            }

        },

        computed: {

            unitDebug: function(): Loading<UnitDebug[]> {
                return Loadable.loaded(this.arr);
            }

        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            Collapsible, InfoHover
        }
    });
    export default MatchUnitDebug;

</script>