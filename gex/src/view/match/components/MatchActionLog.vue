
<template>

    <collapsible header-text="Action Log" :show="false" size-class="h1" bg-color="bg-light">
        <div>
            <div v-if="entries.length == 0" class="mb-3">
                <button class="btn btn-primary mb-2" @click="makeAll">
                    Generate action log
                </button>

                <small class="d-block text-muted">
                    The action log can contain thousands of entries and may lag website for a few seconds
                </small>
            </div>

            <div class="mb-3">
                <h5>Options</h5>
                <toggle-button v-model="options.showDebug">Show debug</toggle-button>
                <toggle-button v-model="options.showFrame">Show frame</toggle-button>
            </div>

            <a-table :entries="tableData"
                default-sort-column="frame" default-sort-order="asc"
                :paginate="false" :hover="true" :striped="false">

                <a-col>
                    <a-header>
                        <b>Type</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.type }}
                    </a-body>
                </a-col>

                <a-col>
                    <a-header>
                        <b>Action</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <span v-html="entry.parts.map(iter => iter.html).join(' ')">

                        </span>

                        <div v-if="options.showDebug">
                            <span v-if="entry.event == undefined">
                                --
                            </span>
                            <span v-else class="text-monospace" style="overflow-wrap: break-word">
                                <pre><code>{{JSON.stringify(entry.event, null, 2)}}</code></pre>
                            </span>
                        </div>
                    </a-body>
                </a-col>

                <a-col>
                    <a-header>
                        <b>Timestamp</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <span v-if="options.showFrame">
                            {{ entry.frame }}
                        </span>
                        <span v-else>
                            {{ entry.frame / 30 | mduration }}
                        </span>
                    </a-body>
                </a-col>

            </a-table>

        </div>
    </collapsible>

</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loadable, Loading } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol, ARank, ATableType } from "components/ATable";
    import ToggleButton from "components/ToggleButton";
    import Collapsible from "components/Collapsible.vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { GameOutput } from "model/GameOutput";
    import { GameEventUnitDef } from "model/GameEventUnitDef";

    import LocaleUtil from "util/Locale";
    import "filters/MomentFilter";

    type LogPart = {
        html: string;
    };

    type ActionLogEntry = {
        type: string;
        frame: number;
        parts: LogPart[];

        event?: object;
        unitID?: number;
        otherUnitID?: number;
    };

    export const MatchActionLog = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            output: { type: Object as PropType<GameOutput>, required: true },
        },

        data: function() {
            return {
                entries: [] as ActionLogEntry[],

                options: {
                    showDebug: false as boolean,
                    showFrame: false as boolean,
                }

            }
        },

        mounted: function(): void { },

        methods: {

            makeAll: async function(): Promise<void> {
                const entries: ActionLogEntry[] = [];

                entries.push(...this.makeUnitCreated());
                entries.push(...this.makeUnitKilled());
                entries.push(...this.makeTeamsKilled());
                entries.push(...this.makeWindUpdate());

                entries.sort((a, b) => {
                    return a.frame - b.frame;
                });

                this.entries = entries;
            },

            makeUnitCreated: function(): ActionLogEntry[] {
                const entries: ActionLogEntry[] = [];

                for (const ev of this.output.unitsCreated) {
                    const entry: ActionLogEntry = {
                        type: "unit_created",
                        frame: ev.frame,
                        event: ev,
                        unitID: ev.unitID,
                        parts: [
                            this.createPlayerName(ev.teamID),
                            this.createText("created a"),
                            this.createUnitIcon(ev.definitionID),
                            this.createUnitName(ev.definitionID),
                            this.createText(`at ${ev.unitX}, ${ev.unitZ}`)
                        ]
                    }

                    entries.push(entry);
                }

                return entries;
            },

            makeUnitKilled: function(): ActionLogEntry[] {
                const entries: ActionLogEntry[] = [];

                for (const ev of this.output.unitsKilled) {
                    const entry: ActionLogEntry = {
                        type: "unit_killed",
                        frame: ev.frame,
                        event: ev,
                        unitID: ev.unitID,
                        parts: [
                            this.createPlayerName(ev.teamID, true),
                            this.createUnitIcon(ev.definitionID),
                            this.createUnitName(ev.definitionID),
                            this.createText("was")
                        ]
                    }

                    if (ev.teamID == ev.attackerTeam && ev.weaponDefinitionID == -12) {
                        entry.parts.push(this.createText("reclaimed"));
                    } else if (ev.teamID == ev.attackerTeam) {
                        entry.parts.push(this.createText("teamkilled"));
                    } else {
                        entry.parts.push(this.createText("killed"));
                    }

                    if (ev.attackerTeam != undefined && ev.attackerDefinitionID != undefined) {
                        entry.parts.push(
                            this.createText("by"),
                            this.createPlayerName(ev.attackerTeam, true),
                            this.createUnitIcon(ev.attackerDefinitionID),
                            this.createUnitName(ev.attackerDefinitionID)
                        );
                    }

                    if (ev.attackerX != undefined && ev.attackerY != undefined && ev.attackerZ != undefined) {
                        const dist = Math.sqrt(
                            Math.pow(ev.killedX - ev.attackerX, 2)
                            + Math.pow(ev.killedY - ev.attackerY, 2)
                            + Math.pow(ev.killedZ - ev.attackerZ, 2)
                        );

                        entry.parts.push(this.createText(`(distance: ${LocaleUtil.locale(dist, 0)})`));
                    }

                    if (ev.weaponDefinitionID < 0) {
                        if (ev.weaponDefinitionID == -19) {
                            entry.parts.push(this.createText("(decayed)"));
                        } else if (ev.weaponDefinitionID == -12){

                        } else if (ev.weaponDefinitionID == -8) {
                            entry.parts.push(this.createText("(crashed)"));
                        } else if (ev.weaponDefinitionID == -21) {
                            entry.parts.push(this.createText("(lua script)"));
                        } else if (ev.weaponDefinitionID == -15) {
                            entry.parts.push(this.createText("(factory killed)"));
                        } else if (ev.weaponDefinitionID == -16) {
                            entry.parts.push(this.createText("(factory cancel)"));
                        } else {
                            entry.parts.push(this.createText(`&lt;unchecked weapon def ID ${ev.weaponDefinitionID}&gt;`));

                        }
                    }

                    entries.push(entry);
                }

                return entries;
            },

            makeTeamsKilled: function(): ActionLogEntry[] {
                const entries: ActionLogEntry[] = [];

                for (const ev of this.output.teamDiedEvents) {
                    const entry: ActionLogEntry = {
                        type: "team_killed",
                        frame: ev.frame,
                        event: ev,
                        parts: [
                            this.createPlayerName(ev.teamID),
                            this.createText("was killed")
                        ]
                    };

                    entries.push(entry);
                }

                return entries;
            },

            makeWindUpdate: function(): ActionLogEntry[] {
                const entries: ActionLogEntry[] = [];

                for (const ev of this.output.windUpdates) {
                    const entry: ActionLogEntry = {
                        type: "wind_update",
                        frame: ev.frame,
                        event: ev,
                        parts: [
                            this.createText(`Wind was recorded at ${LocaleUtil.locale(ev.value, 2)}`)
                        ]
                    };

                    entries.push(entry);
                }

                return entries;
            },

            createText: function(str: string): LogPart {
                return { html: str };
            },

            createPlayerName: function(teamID: number, possesive?: boolean): LogPart {
                const player: BarMatchPlayer | undefined = this.match.players.find(iter => iter.teamID == teamID);

                if (player == undefined) {
                    return {
                        html: `&lt;missing team ${teamID}&gt;`
                    };
                }

                return {
                    html: `<a href="/user/${player.userID}" style="color: ${player.hexColor}">${player.username}${(possesive == true ? "'s" : "")}</a>`
                };
            },

            createUnitName: function(defID: number): LogPart {
                const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(defID);

                if (unitDef == undefined) {
                    return this.createText(`&lt;missing unit def ${defID}&gt;`);
                }

                return this.createText(`${unitDef.name}`);
            },

            createUnitIcon: function(defID: number, color?: number): LogPart {
                const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(defID);

                if (unitDef == undefined) {
                    return this.createText(``);
                }

                const p = this.createText(`<img src="/image-proxy/UnitIcon?defName=${encodeURIComponent(unitDef.definitionName)}" height="16" title="Unit icon for ${unitDef.name}">`);
                return p;
            }

        },

        computed: {

            tableData: function(): Loading<ActionLogEntry[]> {
                return Loadable.loaded(this.entries);
            }

        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            ToggleButton, Collapsible
        }
    });
    export default MatchActionLog;
</script>
