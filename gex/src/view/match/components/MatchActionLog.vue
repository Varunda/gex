
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
                <toggle-button v-model="options.combineQuickBuilds">
                    Combine short builds
                    <info-hover text="Combine constructions of a unit/building that are under 10 seconds into 1 action"></info-hover>
                </toggle-button>

                <div class="d-inline border h-100 mx-2"></div>

                <div class="ms-1 btn-group border">
                    <button type="button" class="btn" :class="[ filter.teamIDs.length > 0 ? 'btn-primary' : '' ]" data-bs-toggle="dropdown">
                        Shown players
                        <span v-if="filter.teamIDs.length > 0">
                            ({{ filter.teamIDs.length }})
                        </span>
                        <span v-else>
                            (All)
                        </span>
                    </button>
                    <button type="button" class="btn dropdown-toggle dropdown-toggle-split" data-bs-toggle="dropdown" :class="[ filter.teamIDs.length > 0 ? 'btn-primary' : '' ]">
                        <span class="visually-hidden">toggle dropdown</span>
                    </button>
                    <ul class="dropdown-menu dropdown-menu-end">
                        <li v-for="player in match.players" :key="player.teamID">
                            <a class="dropdown-item" :style="{ 'color': player.hexColor, 'user-select': 'none' }" @click.stop="toggleFilterPlayer($event)">
                                <input class="form-check-input" type="checkbox" :id="'action-log-filter-player-' + player.teamID" :data-team-id="player.teamID">
                                <label class="form-check-label w-100" :for="'action-log-filter-player-' + player.teamID">
                                    {{ player.username }}
                                </label>
                            </a>
                        </li>
                    </ul>
                </div>

                <div v-if="entries.length > 0">
                    <a href="javascript:void(0)" download="download" @click="downloadJson" class="me-2">Download action log</a>
                    <a id="downloadJsonAnchor" style="display: none"></a>
                </div>
            </div>

            <a-table v-if="showTable" :entries="tableData"
                default-sort-column="frame" default-sort-order="asc"
                :paginate="false" :hover="true" :striped="false" :show-filters="true">

                <a-col>
                    <a-header>
                        <b>Type</b>
                    </a-header>

                    <a-filter field="type" type="string" method="dropdown"
                        :conditions="[ 'equals' ]">
                    </a-filter>

                    <a-body v-slot="entry">
                        {{ entry.type }}
                    </a-body>
                </a-col>

                <a-col v-if="options.extraFilters">
                    <a-header>
                        <b>Player</b>
                    </a-header>

                    <a-filter field="teamName" type="string" method="dropdown"
                        :conditions="[ 'equals' ]">
                    </a-filter>

                    <a-body v-slot="entry">
                        {{ entry.teamName }}
                    </a-body>
                </a-col>

                <a-col v-if="options.extraFilters">
                    <a-header>
                        <b>Unit type</b>
                    </a-header>

                    <a-filter field="unitName" type="string" method="dropdown"
                        :conditions="[ 'equals' ]">
                    </a-filter>

                    <a-body v-slot="entry">
                        {{ entry.unitName }}
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
    import InfoHover from "components/InfoHover.vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchSpectator } from "model/BarMatchSpectator";
    import { GameOutput } from "model/GameOutput";
    import { GameEventUnitDef } from "model/GameEventUnitDef";

    import LocaleUtil from "util/Locale";
    import "filters/MomentFilter";
import { GameEventUnitsGiven } from "model/GameEventUnitsGiven";

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
        unitName?: string;
        teamID?: number;
        allyTeamID?: number;
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
                    extraFilters: false as boolean,
                    combineQuickBuilds: true as boolean,
                },

                filter: {
                    teamIDs: [] as number[]
                },

                showTable: true as boolean

            }
        },

        mounted: function(): void { },

        methods: {

            makeAll: async function(): Promise<void> {
                const entries: ActionLogEntry[] = [];

                entries.push(...this.makeUnitCreated());
                entries.push(...this.makeUnitKilled());
                entries.push(...this.makeTeamsKilled());
                entries.push(...this.makeUnitsShared());
                entries.push(...this.makeWindUpdate());
                entries.push(...this.makePlayerLeaves());

                entries.sort((a, b) => {
                    return a.frame - b.frame;
                });

                this.entries = entries;
            },

            makeUnitCreated: function(): ActionLogEntry[] {
                const entries: ActionLogEntry[] = [];

                for (const ev of this.output.unitsCreated) {
                    let entry: ActionLogEntry;

                    if (ev.completed != 0) {
                        const combine: boolean = this.options.combineQuickBuilds == true && (ev.completed - ev.frame) < (30 * 10);

                        entry = {
                            type: "unit_created",
                            frame: ev.frame,
                            event: ev,
                            unitID: ev.unitID,
                            teamID: ev.teamID,
                            allyTeamID: this.match.players.find(iter => iter.teamID == ev.teamID)?.allyTeamID,
                            parts: [
                                this.createPlayerName(ev.teamID),
                                combine == true ? this.createText("built a") : this.createText("started constructing a"),
                                this.createUnitIcon(ev.definitionID),
                                this.createUnitName(ev.definitionID),
                                this.createText(`at ${ev.unitX}, ${ev.unitZ}`),
                            ]
                        }

                        const dur: LogPart= this.createText(`(took ${LocaleUtil.locale((ev.completed - ev.frame) / 30, 2)}s to build)`)

                        if (combine == true) {
                            entry.parts.push(dur);
                        } else {
                            entries.push({
                                type: "unit_completed",
                                frame: ev.completed,
                                event: ev,
                                unitID: ev.unitID,
                                teamID: ev.teamID,
                                allyTeamID: this.match.players.find(iter => iter.teamID == ev.teamID)?.allyTeamID,
                                parts: [
                                    this.createPlayerName(ev.teamID),
                                    this.createText("completed constructing a"),
                                    this.createUnitIcon(ev.definitionID),
                                    this.createUnitName(ev.definitionID),
                                    dur
                                ]
                            });
                        }
                    } else {
                        entry = {
                            type: "unit_created",
                            frame: ev.frame,
                            event: ev,
                            unitID: ev.unitID,
                            teamID: ev.teamID,
                            allyTeamID: this.match.players.find(iter => iter.teamID == ev.teamID)?.allyTeamID,
                            parts: [
                                this.createPlayerName(ev.teamID),
                                this.createText("created a"),
                                this.createUnitIcon(ev.definitionID),
                                this.createUnitName(ev.definitionID),
                                this.createText(`at ${ev.unitX}, ${ev.unitZ}`)
                            ]
                        }
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
                        teamID: ev.teamID,
                        allyTeamID: this.match.players.find(iter => iter.teamID == ev.teamID)?.allyTeamID,
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

                    // https://github.com/beyond-all-reason/RecoilEngine/blob/cdfc9d7b872c3d890fc7c77bcb645a23cd9ec8e5/rts/Sim/Objects/SolidObject.h#L93-L123
                    if (ev.weaponDefinitionID < 0) {
                        if (ev.weaponDefinitionID == -19) {
                            entry.parts.push(this.createText("(decayed)"));
                        } else if (ev.weaponDefinitionID == -12) {
                            // intentionally empty, as it's checked above
                        } else if (ev.weaponDefinitionID == -8) {
                            entry.parts.push(this.createText("(crashed)"));
                        } else if (ev.weaponDefinitionID == -10) {
                            entry.parts.push(this.createText("(self-d)"));
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

            makeUnitsShared: function(): ActionLogEntry[] {
                const entries: ActionLogEntry[] = [];

                let groupedEntries: GameEventUnitsGiven[] = [];
                let previousEntry: GameEventUnitsGiven | null = null;

                for (const ev of this.output.unitsGiven) {

                    if (previousEntry != null && previousEntry.frame != ev.frame) {
                        const entry: ActionLogEntry = {
                            type: "unit_shared",
                            frame: ev.frame,
                            event: ev,
                            teamID: ev.teamID,
                            allyTeamID: this.match.players.find(iter => iter.teamID == ev.teamID)?.allyTeamID,
                            parts: [
                                this.createPlayerName(ev.teamID),
                                this.createText("shared"),
                            ]
                        };

                        const map: Map<number, number> = new Map(); // <def ID, count>
                        for (const iter of groupedEntries) {
                            map.set(iter.definitionID, (map.get(iter.definitionID) ?? 0) + 1);
                        }

                        const mapEntries = Array.from(map.entries());
                        for (let i = 0; i < mapEntries.length; ++i) {
                            const iter = mapEntries[i];

                            if (iter[1] == 1) {
                                entry.parts.push(
                                    this.createText("a"),
                                    //this.createUnitIcon(iter[0]),
                                    this.createUnitName(iter[0])
                                );
                            } else {
                                entry.parts.push(
                                    this.createText(`${iter[1]}`),
                                    //this.createUnitIcon(iter[0]),
                                    this.createUnitName(iter[0], true),
                                );
                            }

                            if (i != mapEntries.length - 1) {
                                entry.parts.push(
                                    this.createText("and")
                                )
                            }
                        }

                        entry.parts.push(
                            this.createText("to"),
                            this.createPlayerName(ev.newTeamID)
                        );

                        entries.push(entry);

                        groupedEntries = [ev];
                    } else {
                        groupedEntries.push(ev);
                    }

                    previousEntry = ev;

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
                        teamID: ev.teamID,
                        allyTeamID: this.match.players.find(iter => iter.teamID == ev.teamID)?.allyTeamID,
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

            makePlayerLeaves: function(): ActionLogEntry[] {
                return this.match.playerLeaves.map(iter => {
                    const reason: string = iter.reason == 0 ? `connection lost`
                        : iter.reason == 1 ? `quit`
                        : iter.reason == 2 ? `kicked`
                        : `<unchecked ${iter.reason}>`

                    return {
                        type: "player_left",
                        frame: iter.gameTime * 30,
                        event: iter,
                        parts: [
                            this.createPlayerName(iter.playerID),
                            this.createText(`left the game (Reason: ${reason})`)
                        ]
                    }
                });
            },

            makeSelfD: function(): ActionLogEntry[] {
                return [];
            },

            downloadJson: function(): void {
                const dataStr = "data:text/json;charset=utf-8," + encodeURIComponent(JSON.stringify({
                    "match": this.entries,
                }, null, 4));
                const dl = document.getElementById("downloadJsonAnchor");
                if (dl == null) {
                    console.error(`Match> missing #downloadJsonAnchor to attach JSON download payload to`);
                    return;
                }
                dl.setAttribute("href", dataStr);
                dl.setAttribute("download", `action-log-${this.match.id}-${this.match.fileName.replace(".sdfz", "")}.json`);
                dl.click();
            },

            createText: function(str: string): LogPart {
                return { html: str };
            },

            createPlayerName: function(teamID: number, possesive?: boolean): LogPart {
                const player: BarMatchPlayer | undefined = this.match.players.find(iter => iter.teamID == teamID);
                const spec: BarMatchSpectator | undefined = this.match.spectators.find(iter => iter.playerID == teamID);

                if (player == undefined && spec == undefined) {
                    return {
                        html: `&lt;missing team ${teamID}&gt;`
                    };
                }

                const userID: number = player?.userID ?? spec!.userID;
                const color: string = player?.hexColor ?? "#ffff00";
                const name: string = player?.username ?? spec!.username;

                return {
                    html: `<a href="/user/${userID}" style="color: ${color}" target="blank" ref="nofollow">${name}${(possesive == true ? "'s" : "")}</a>`
                };
            },

            createUnitName: function(defID: number, pluralize?: boolean): LogPart {
                const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(defID);

                if (unitDef == undefined) {
                    return this.createText(`&lt;missing unit def ${defID}&gt;`);
                }

                return this.createText(`${unitDef.name}${pluralize == true ? "s" : ""}`);
            },

            createUnitIcon: function(defID: number, color?: number): LogPart {
                const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(defID);

                if (unitDef == undefined) {
                    return this.createText(``);
                }

                //const p = this.createText(`<img src="/image-proxy/UnitIcon?defName=${encodeURIComponent(unitDef.definitionName)}" height="16" title="Unit icon for ${unitDef.name}">`);
                const p = this.createText(`<div class="bui bui-${unitDef.definitionName}" style="--size: 16" title="Unit icon for ${unitDef.name}"></div>`);
                return p;
            },

            getDefinitionName: function(defID: number): string | undefined {
                const unitDef: GameEventUnitDef | undefined = this.output.unitDefinitions.get(defID);
                return unitDef?.name;
            },

            toggleFilterPlayer: function(ev: MouseEvent): void {
                if (!ev.target) {
                    return;
                }

                let t = ev.target as HTMLElement;
                if (t.nodeName != "INPUT") {
                    return;
                }

                const teamID: string | undefined = t.dataset["teamId"];
                if (teamID == undefined) {
                    throw `missing data-team-id on ${t.id} (cannot get teamId to toggle)`;
                }

                const value = (t as HTMLInputElement).checked;
                if (value == true) {
                    this.filter.teamIDs.push(Number.parseInt(teamID));
                } else if (value == false) {
                    this.filter.teamIDs = this.filter.teamIDs.filter(iter => iter != Number.parseInt(teamID));
                }
            }

        },

        computed: {
            tableData: function(): Loading<ActionLogEntry[]> {
                return Loadable.loaded(this.entries.filter(iter => {
                    return (this.filter.teamIDs.length == 0 || (iter.teamID != undefined && this.filter.teamIDs.indexOf(iter.teamID) > -1));
                }));
            }
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            ToggleButton, Collapsible, InfoHover
        }
    });
    export default MatchActionLog;
</script>
