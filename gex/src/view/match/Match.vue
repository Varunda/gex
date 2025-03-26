
<template>
    <div>
        <div class="d-flex align-items-center">
            <gex-menu class="flex-grow-1"></gex-menu>
        </div>

        <div class="container">
            <div v-if="match.state == 'idle'"> </div>

            <div v-else-if="match.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="match.state == 'loaded'">

                <div class="d-flex">
                    <div class="flex-grow-1">
                        <h1>{{ match.data.map }} ({{ match.data.gamemode | gamemode }})</h1>

                        <h2>played on {{ match.data.startTime | moment }}</h2>

                        <h2>took {{  match.data.durationMs / 1000 | mduration }}</h2>

                        <h2>
                            <span v-if="isFFA">
                                {{ match.data.allyTeams.length }}-way FFA
                            </span>
                            <span v-else>
                                {{ match.data.allyTeams.map(iter => iter.playerCount).join(" v ") }}
                            </span>
                        </h2>
                    </div>

                    <div v-if="match.data.processing" class="flex-grow-0">
                        <table class="table table-sm table-borderless" style="font-size: 0.8rem;">
                            <tbody>
                                <template v-if="match.data.processing">
                                    <tr is="ProcessingStep" step="Replay downloaded" :when="match.data.processing.replayDownloaded" :duration="match.data.processing.replayDownloadedMs"></tr>
                                    <tr is="ProcessingStep" step="Replay parsed" :when="match.data.processing.replayParsed" :duration="match.data.processing.replayParsedMs"></tr>
                                    <tr is="ProcessingStep" step="Replay simulated" :when="match.data.processing.replaySimulated" :duration="match.data.processing.replaySimulatedMs"></tr>
                                    <tr is="ProcessingStep" step="Events parsed" :when="match.data.processing.actionsParsed" :duration="match.data.processing.actionsParsedMs"></tr>
                                </template>
                                <tr>
                                    <td class="text-muted">Engine</td>
                                    <td class="text-muted">{{ match.data.engine }}</td>
                                </tr>
                                <tr>
                                    <td class="text-muted">Game version</td>
                                    <td class="text-muted">{{ match.data.gameVersion }}</td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </div>

                <div class="d-flex" style="gap: 0.5rem;">
                    <match-option name="Game settings" :options="match.data.gameSettings"></match-option>
                    <match-option name="Map settings" :options="match.data.mapSettings"></match-option>
                    <match-option name="Lobby settings" :options="match.data.spadsSettings"></match-option>
                    <match-option name="Host settings" :options="match.data.hostSettings"></match-option>
                </div>

                <h4>
                    <a :href="'/downloadmatch/' + gameID" download="download">
                        Download replay
                    </a>
                </h4>

                <h4>
                    View on <a :href="'https://www.beyondallreason.info/replays?gameId=' + gameID">Beyond All Reason website</a>
                </h4>

                <hr class="border"/>

                <match-teams :match="match.data" class="my-3"></match-teams>

                <div v-if="output.state == 'loaded'">
                    <match-map :match="match.data" :output="output.data" class="my-3"></match-map>

                    <div v-if="match.data.processing && match.data.processing.actionsParsed != null">

                        <team-stats-chart :stats="output.data.teamStats" :match="match.data" class="my-4"></team-stats-chart>

                        <hr class="border">

                        <match-opener :openers="computedData.opener" class="my-4"></match-opener>

                        <hr class="border">

                        <div style="position: sticky; top: 10px; z-index: 9999;" class="bg-dark pt-3 pb-1 px-2 border rounded">
                            <h4 v-if="selectedPlayer" class="text-center">
                                Viewing unit stats for
                                <span :style="{ 'color': selectedPlayer.hexColor }">
                                    {{ selectedPlayer.username }}
                                </span>
                            </h4>

                            <div class="d-flex flex-wrap mb-3">
                                <button v-for="player in match.data.players" :key="player.teamID" class="btn m-1 flex-grow-0" :style=" {
                                        'background-color': (selectedTeam == player.teamID) ? player.hexColor : 'var(--bs-secondary)',
                                        'color': (selectedTeam == player.teamID) ? 'white' : player.hexColor
                                    }" @click="selectedTeam = player.teamID">

                                    <span style="text-shadow: 1px 1px 1px black">
                                        {{ player.username }}
                                    </span>
                                </button>
                            </div>
                        </div>

                        <hr class="border">

                        <match-combat-stats :match="match.data" :unit-stats="computedData.unitStats" :selected-team="selectedTeam" class="my-4"></match-combat-stats>
                        <match-eco-stats :match="match.data" :output="output.data" :unit-stats="computedData.unitStats" :unit-resources="computedData.unitResources" :selected-team="selectedTeam" class="my-4"></match-eco-stats>

                        <hr class="border">

                        <unit-def-view :unit-defs="Array.from(output.data.unitDefinitions.values())" :output="output.data" class="my-4"></unit-def-view>
                    </div>

                    <div v-else class="text-center alert alert-warning mt-4">
                        This game has not been ran locally, and in-depth stats are not available
                    </div>

                    <match-chat :match="match.data"></match-chat>

                    <small class="text-muted">
                        {{ 
                            output.data.extraStats.length + output.data.commanderPositionUpdates.length + output.data.factoryUnitCreated.length + output.data.teamDiedEvents.length
                            + output.data.teamStats.length + output.data.unitDefinitions.size + output.data.unitResources.length + output.data.unitsCreated.length
                            + output.data.unitsKilled.length + output.data.windUpdates.length
                        }}
                        events
                    </small>
                </div>
            </div>

            <div v-else-if="match.state == 'error'">
                <api-error :error="match.problem"></api-error>
            </div>

            <div v-else>
                unchecked state of match: {{ match.state }}
            </div>
        </div>

    </div>
    
</template>

<style scoped>
    text {
        font-family: "Atkinson Hyperlegible";
    }

</style>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading"

    import "filters/MomentFilter";

    import { GexMenu } from "components/AppMenu";
    import ApiError from "components/ApiError";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";
    import Collapsible from "components/Collapsible.vue";

    import { MatchOpener } from "./components/MatchOpener.vue";
    import MatchFactories from "./components/MatchFactories.vue";
    import UnitDefView from "./components/UnitDefView.vue";
    import MatchWindGraph from "./components/MatchWindGraph.vue";
    import MatchUnitStats from "./components/MatchUnitStats.vue";
    import TeamStatsChart from "./components/TeamStatsChart.vue";
    import MatchResourceProduction from "./components/MatchResourceProduction.vue";
    import MatchTeams from "./components/MatchTeams.vue";
    import MatchMap from "./components/MatchMap.vue";
    import MatchChat from "./components/MatchChat.vue";
    import MatchOption from "./components/MatchOption.vue";
    import MatchCombatStats from "./components/MatchCombatStats.vue";

    import { BarMatchApi } from "api/BarMatchApi";
    import { GameOutputApi } from "api/GameOutputApi";

    import { GameOutput } from "model/GameOutput";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import { PlayerOpener } from "./compute/PlayerOpenerData";
    import { UnitStats } from "./compute/UnitStatData";
    import { ResourceProductionData } from "./compute/ResourceProductionData";

    import "filters/BarGamemodeFilter";
import MatchEcoStats from "./components/MatchEcoStats.vue";

    export const ProcessingStep = Vue.extend({
        props: {
            step: { type: String }, 
            when: { },
            duration: { }
        },

        template: `
            <tr>
                <td class="text-muted">{{step}}</td>
                <td class="text-muted">
                    <span v-if="when != null">
                        on {{ when | moment("YYYY-MM-DD hh:mm:ssA") }} (took {{ duration / 1000 | mduration }})
                    </span>
                    <span v-else>
                        --
                    </span>
                </td>
            </tr>
        `
    });

    export const Match = Vue.extend({
        props: {

        },

        data: function() {
            return {
                gameID: "" as string,

                match: Loadable.idle() as Loading<BarMatch>,
                output: Loadable.idle() as Loading<GameOutput>,

                loadingSteps: 2 as number,

                unitIdToDefId: new Map as Map<number, number>,

                selectedTeam: 0 as number,

                computedData: {
                    opener: [] as PlayerOpener[],
                    unitStats: [] as UnitStats[],
                    unitResources: [] as ResourceProductionData[]
                },

            };
        },

        created: function(): void {
            this.gameID = location.pathname.split("/")[2];
            document.title = "Gex / Match";
        },

        beforeMount: function(): void {
            this.loadMatch();
            this.loadOutput();
        },

        methods: {
            loadMatch: async function(): Promise<void> {
                this.match = Loadable.loading();
                this.match = await BarMatchApi.getByID(this.gameID);

                if (this.match.state != "loaded") {
                    return;
                }

                this.decLoadingStepsAndPossiblyStart();
            },

            decLoadingStepsAndPossiblyStart: function(): void {
                --this.loadingSteps;

                if (this.loadingSteps != 0) {
                    console.log(`have ${this.loadingSteps} until done loading all assets!`);
                    return;
                }

                if (this.match.state != "loaded") {
                    throw `expected match to be "loaded', is ${this.match.state} instead!"`;
                }
                if (this.output.state != "loaded") {
                    throw `expected output to be "loaded", is ${this.output.state} instead!`;
                }

                console.log(`add data loaded, starting processing`);

                for (const ev of this.output.data.unitsCreated) {
                    this.unitIdToDefId.set(ev.unitID, ev.definitionID);
                }

                this.computedData.opener = PlayerOpener.compute(this.match.data, this.output.data);
                this.computedData.unitStats = UnitStats.compute(this.output.data, this.match.data);
                this.computedData.unitResources = ResourceProductionData.compute(this.match.data, this.output.data);
            },

            loadOutput: async function(): Promise<void> {
                this.output = Loadable.loading();
                this.output = await GameOutputApi.getEvents(this.gameID);

                if (this.output.state != "loaded") {
                    return;
                }

                this.decLoadingStepsAndPossiblyStart();
            },
        },

        computed: {
            unitTweaks: function(): string {
                if (this.match.state != "loaded") {
                    return "";
                }

                return atob(this.match.data.gameSettings.tweakunits);
            },

            isFFA: function(): boolean {
                if (this.match.state != "loaded") {
                    return false;
                }
                return this.match.data.allyTeams.length > 2 && Math.max(...this.match.data.allyTeams.map(iter => iter.playerCount)) == 1;
            },

            selectedPlayer: function(): BarMatchPlayer | null {
                if (this.match.state != "loaded") {
                    return null;
                }

                return this.match.data.players.find(iter => iter.teamID == this.selectedTeam) || null;
            }
        },

        watch: {

        },

        components: {
            GexMenu, InfoHover, ApiError, ToggleButton,
            MatchOpener, MatchFactories, UnitDefView, MatchWindGraph, MatchUnitStats, TeamStatsChart, MatchResourceProduction,
            MatchTeams, MatchMap, MatchChat, MatchOption, MatchCombatStats, MatchEcoStats,
            ProcessingStep, Collapsible
        }
    });
    export default Match;
</script>