
<template>
    <div>
        <div class="container" id="main">
            <div v-if="queue.show == true" class="mb-3">
                <div v-if="queue.index > -1" class="alert alert-info">
                    <h3 class="text-info text-center">
                        This match is in queue to be ran on Gex
                        <button title="Refresh" class="btn btn-link p-0" @click="loadQueuePosition" :class=" {'spin': queue.data.state != 'loaded'}">
                            <span>&#x21bb;</span>
                        </button>
                    </h3>
                    <div class="text-center d-block">
                        This match is in position {{queue.index + 1}}<span v-if="queue.data.state == 'loaded'"> of {{queue.data.data.length}}.</span><span v-else>.</span>
                        <span v-if="queue.processingTime != null">
                            Estimated time: {{(queue.processingTime * (queue.index + 1)) / 1000 | mduration}}
                        </span>
                    </div>
                </div>
            </div>

            <div v-if="showWaitForActions">
                <h4 class="alert alert-warning text-center">
                    Gex is currently parsing the output after replaying the match, please refresh in a minute!
                </h4>
            </div>

            <div v-if="showClickToReload">
                <h4 class="text-success text-center btn-link" @click="loadBoth">
                    This match was replayed on Gex! Click here to load the data
                </h4>
            </div>

            <div v-if="replay.status != null && showHeadlessStatus" class="alert alert-info text-center">
                <h3>This match is currently being replayed on Gex to collect stats</h3>

                <h5>
                    <span>
                        Estimated time left:
                    </span>
                    <span v-if="replay.status.simulating == true">
                        {{ (replay.status.durationFrames - replay.status.frame) / replay.status.fps | mduration }}
                    </span>
                    <span v-else>
                        Game is booting up
                    </span>
                </h5>

                <div class="mb-0">
                    <div class="progress" style="height: 1rem;">
                        <div class="progress-bar progress-bar-striped progress-bar-animated"
                            :class="{
                                'progress-bar-animated': replay.status.simulating
                            }"
                            :style="{
                                'width': (replay.status.frame / replay.status.durationFrames * 100) + '%',
                            }
                        ">
                        </div>
                    </div>

                    <span v-if="replay.status.simulating == true">
                        On frame {{ replay.status.frame }} of {{ replay.status.durationFrames }}, running at {{ (replay.status.fps / 30) | locale(2) }}x speed
                    </span>
                    <span v-else>
                        Game is currently booting up
                    </span>
                </div>
            </div>

            <div v-if="match.state == 'idle'"> </div>

            <div v-else-if="match.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="match.state == 'loaded'">

                <div class="d-flex flex-wrap">
                    <div class="flex-grow-1">
                        <h1 class="d-flex flex-wrap">
                            <a :href="'/map/' + match.data.mapName">{{ match.data.map }}</a>
                            &nbsp;
                            <span>
                                ({{ match.data.gamemode | gamemode }})
                            </span>
                        </h1>

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
                        <collapsible header-text="Gex processing metadata" size-class="h6" :show="!showMobile">
                            <table class="table table-sm table-borderless" style="font-size: 0.8rem;">
                                <tbody>
                                    <template v-if="match.data.processing">
                                        <tr is="ProcessingStep" step="Replay downloaded" :when="match.data.processing.replayDownloaded" :duration="match.data.processing.replayDownloadedMs"></tr>
                                        <tr is="ProcessingStep" step="Replay parsed" :when="match.data.processing.replayParsed" :duration="match.data.processing.replayParsedMs"></tr>
                                        <tr is="ProcessingStep" step="Replay simulated" :when="match.data.processing.replaySimulated" :duration="match.data.processing.replaySimulatedMs"></tr>
                                        <tr is="ProcessingStep" step="Events parsed" :when="match.data.processing.actionsParsed" :duration="match.data.processing.actionsParsedMs"></tr>
                                        <tr>
                                            <td class="text-muted">Prio</td>
                                            <td class="text-muted">{{ match.data.processing.priority }}</td>
                                        </tr>
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
                        </collapsible>
                    </div>
                </div>

                <div class="d-flex flex-wrap" style="gap: 0.5rem;">
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

                <h4 v-if="match.data.uploadedByID != null">
                    Uploaded by
                    <info-hover text="This match was not found from BAR's public API, and was instead uploaded by a user"></info-hover>:
                    <span v-if="match.data.uploadedBy == null">
                        unknown user {{ match.data.uploadedByID }}
                    </span>
                    <span v-else>
                        {{ match.data.uploadedBy.name }}
                    </span>
                </h4>

                <hr class="border"/>

                <match-teams :match="match.data" class="my-3"></match-teams>

                <div v-if="output.state == 'loaded' && (!match.data.processing || match.data.processing.actionsParsed == null)" class="text-center alert alert-info mt-4">
                    This match has not been replayed on Gex, and in-depth stats are not available. <a href="/faq">More info</a>
                </div>

                <div v-if="output.state == 'loaded'">
                    <match-map :match="match.data" :output="output.data" :screen-width="containerWidth" class="my-3"></match-map>

                    <div v-if="match.data.processing && match.data.processing.actionsParsed != null">

                        <team-stats-chart :stats="computedData.merged" :match="match.data" :show-mobile="showMobile" class="my-4"></team-stats-chart>

                        <hr class="border">

                        <match-opener :openers="computedData.opener" class="my-4"></match-opener>

                        <hr class="border">

                        <h1 class="wt-header bg-light text-dark">Unit stats</h1>

                        <div class="player-stats-container">
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
                        <match-eco-stats :match="match.data" :output="output.data" :unit-stats="computedData.unitStats"
                            :unit-resources="computedData.unitResources" :merged="computedData.merged" :selected-team="selectedTeam" class="my-4">
                        </match-eco-stats>
                    </div>

                    <match-chat :match="match.data" :show-mobile="showMobile" class="mb-4"></match-chat>

                    <small class="text-muted">
                        {{ 
                            output.data.extraStats.length + output.data.commanderPositionUpdates.length + output.data.factoryUnitCreated.length + output.data.teamDiedEvents.length
                            + output.data.teamStats.length + output.data.unitDefinitions.size + output.data.unitResources.length + output.data.unitsCreated.length
                            + output.data.unitsKilled.length + output.data.windUpdates.length + output.data.unitDamage.length + output.data.unitPosition.length | locale(0)
                        }}
                        events
                    </small>

                    <div v-if="match.data.processing && match.data.processing.actionsParsed != null">
                        <hr class="border">

                        <unit-def-view :unit-defs="Array.from(output.data.unitDefinitions.values())" :output="output.data" class="my-4"></unit-def-view>
                    </div>
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


    /* <div style="position: sticky; top: 10px; z-index: 9999;" class="bg-dark pt-3 pb-1 px-2 border rounded"> */
    .player-stats-container {
        position: sticky;
        top: 10px;
        z-index: 9999;
        padding-top: 1rem;
        padding-bottom: 0.25rem;
        padding-left: 0.5rem;
        padding-right: 0.5rem;
        border-radius: var(--bs-border-radius) !important;
        --bs-bg-opacity: 1;
        background-color: rgba(var(--bs-dark-rgb), var(--bs-bg-opacity)) !important;
        border: var(--bs-border-width) var(--bs-border-style) var(--bs-border-color);
    }

    @media (max-width: 768px) {
        .player-stats-container {

        }
    }

    @media (min-width: 768px) {
        .player-stats-container {

        }
    }

</style>

<script lang="ts">
    import * as sR from "signalR";
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
    import MatchEcoStats from "./components/MatchEcoStats.vue";
    import Busy from "components/Busy.vue";

    import { BarMatchApi } from "api/BarMatchApi";
    import { GameOutputApi } from "api/GameOutputApi";
    import { QueueApi } from "api/QueueApi";
    import { AppHealth, AppHealthApi, ServiceQueueCount } from "api/AppHealthApi";
    import { BarMatchProcessingApi } from "api/BarMatchProcessingApi";

    import { GameOutput } from "model/GameOutput";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { HeadlessRunStatus } from "model/HeadlessRunStatus";
    import { HeadlessRunQueueEntry } from "model/queue/HeadlessRunQueueEntry";
    import { BarMatchProcessing } from "model/BarMatchProcessing";

    import { PlayerOpener } from "./compute/PlayerOpenerData";
    import { UnitStats } from "./compute/UnitStatData";
    import { ResourceProductionData } from "./compute/ResourceProductionData";
    import MergedStats from "./compute/MergedStats";

    import "filters/BarGamemodeFilter";

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
                    unitResources: [] as ResourceProductionData[],
                    merged: [] as MergedStats[]
                },

                queue: {
                    show: false as boolean,
                    length: 0 as number,
                    data: Loadable.idle() as Loading<HeadlessRunQueueEntry[]>,
                    index: 0 as number,
                    processingTime: null as number | null,
                    wasInQueue: false as boolean,
                    intervalId: -1 as number
                },

                replay: {
                    status: null as HeadlessRunStatus | null
                },

                connection: null as sR.HubConnection | null,

                containerWidth: 0 as number,
                containerHeight: 0 as number,
                showMobile: false as boolean
            };
        },

        created: function(): void {
            this.gameID = location.pathname.split("/")[2];
            console.log(`Match> game ID ${this.gameID}`);
            document.title = "Gex / Match";
        },

        beforeMount: function(): void {
            this.loadBoth();
        },

        mounted: function(): void {
            const obs = new ResizeObserver((mutations: ResizeObserverEntry[], observer: ResizeObserver) => {
                for (const mut of mutations) {
                    this.containerWidth = mut.contentRect.width;
                    //this.containerHeight = mut.contentRect.height - 100;
                    this.containerHeight = mut.contentRect.height;
                    console.log(`Match> parent container changed sized to: ${this.containerWidth}x${this.containerHeight}`);

                    this.showMobile = this.containerWidth <= 768;
                }
            });

            const container = document.getElementById("main");
            if (container == null) {
                console.error(`Match> failed to find container #main, this will break mobile view`);
            } else {
                obs.observe(container);
            }
        },

        methods: {
            loadBoth: async function(): Promise<void> {
                this.loadingSteps = 2;
                await this.loadMatch();

                if (this.match.state == "loaded" && this.match.data.processing != null && this.match.data.processing.actionsParsed != null) {
                    this.loadOutput();
                } else {
                    this.output = Loadable.loaded(new GameOutput());
                    this.decLoadingStepsAndPossiblyStart();
                }

                this.queue.wasInQueue = false;
            },

            loadMatch: async function(): Promise<void> {
                this.match = Loadable.loading();
                this.match = await BarMatchApi.getByID(this.gameID);

                if (this.match.state != "loaded") {
                    console.error(`Match> unexpected state of loading match from api: ${this.match.state}`);
                    return;
                }

                this.replay.status = this.match.data.headlessRunStatus;
                if (this.replay.status != null) {
                    this.makeSignalRConnection();
                }

                let matchName: string = "";
                if (this.match.data.players.length == 2) {
                    matchName = `${this.match.data.players[0].username} v ${this.match.data.players[1].username}`;
                } else {
                    if (this.isFFA == true) {
                        matchName = `${this.match.data.allyTeams.length}-way FFA`;
                    } else {
                        matchName = this.match.data.allyTeams.map(iter => iter.playerCount).join(" v ");
                    }
                }

                document.title = `Gex / Match / ${matchName}`;

                if (this.match.data.processing != null && this.match.data.processing.replaySimulated == null) {

                    if (this.match.data.processing.priority > -1) {
                        console.log(`Match> game is in priority queue`);

                        this.loadPriorityIndex().then(() => {
                            console.log(`Match> game is in priority list at ${this.queue.index}`);
                            if (this.connection == null) {
                                this.makeSignalRConnection();
                            }

                            if (this.queue.wasInQueue == true) {
                                this.queue.intervalId = setInterval(async () => {
                                    if (this.queue.wasInQueue == true) {
                                        console.log(`Match> game is in update queue, updating priority list`);
                                        await this.loadPriorityIndex();
                                        console.log(`Match> game is in position ${this.queue.index} (exiting on -1)`);

                                        if (this.queue.index == -1) {
                                            console.log(`Match> no longer in priority list, stopping queue check`);
                                            clearInterval(this.queue.intervalId);
                                        }
                                    }
                                }, 6000) as unknown as number;
                            }
                        });
                    } else {
                        console.log(`Match> game is in run queue, not priority system, doing first check`);
                        // don't block further loading on the queue position
                        this.loadQueuePosition().then(() => {
                            console.log(`Match> loaded queue position, at ${this.queue.index}`);
                            if (this.connection == null) {
                                this.makeSignalRConnection();
                            }

                            if (this.queue.wasInQueue == true) {
                                this.queue.intervalId = setInterval(async () => {
                                    if (this.queue.wasInQueue == true) {
                                        console.log(`Match> game is in update queue, updating queue position`);
                                        await this.loadQueuePosition();
                                        console.log(`Match> game is in position ${this.queue.index} (exiting on -1)`);

                                        if (this.queue.index == -1) {
                                            console.log(`Match> no longer in queue, stopping queue check`);
                                            clearInterval(this.queue.intervalId);
                                        }
                                    }
                                }, 6000) as unknown as number;
                            }
                        })
                    }
                }

                this.decLoadingStepsAndPossiblyStart();
            },

            decLoadingStepsAndPossiblyStart: function(): void {
                --this.loadingSteps;

                if (this.loadingSteps > 0) {
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
                this.computedData.merged = MergedStats.compute(this.output.data);
            },

            loadOutput: async function(): Promise<void> {
                this.output = Loadable.loading();
                this.output = await GameOutputApi.getEvents(this.gameID);

                if (this.output.state != "loaded") {
                    return;
                }

                this.decLoadingStepsAndPossiblyStart();
            },

            loadPriorityIndex: async function(): Promise<void> {
                const prioList: Loading<BarMatchProcessing[]> = await BarMatchProcessingApi.getPriorityList();
                if (prioList.state != "loaded") {
                    console.error(`Match> when loading prio list, expected 'loaded', got ${prioList.state} instead`);
                    return;
                }

                this.queue.index = prioList.data.findIndex((iter: BarMatchProcessing) => iter.gameID == this.gameID);
                if (this.queue.index == -1) {
                    this.queue.show = false;
                    this.queue.processingTime = null;
                    return;
                }

                this.queue.show = true;
                this.queue.wasInQueue = true;

                const health: Loading<AppHealth> = await AppHealthApi.getHealth();
                if (health.state == "loaded") {
                    const queue: ServiceQueueCount | undefined = health.data.queues.find((iter) => iter.queueName == "headless_run_queue");
                    if (queue == undefined) {
                        this.queue.processingTime = null;
                        return;
                    }

                    this.queue.processingTime = queue.median;
                }
            },

            loadQueuePosition: async function(): Promise<void> {
                this.queue.data = await QueueApi.getHeadlessQueue();
                if (this.queue.data.state != "loaded") {
                    return;
                }

                this.queue.index = this.queue.data.data.findIndex((iter: HeadlessRunQueueEntry) => iter.gameID == this.gameID);
                if (this.queue.index == -1) {
                    this.queue.index = -1;
                    this.queue.processingTime = null;
                    return;
                }

                this.queue.show = true;
                this.queue.wasInQueue = true;

                const health: Loading<AppHealth> = await AppHealthApi.getHealth();
                if (health.state == "loaded") {
                    const queue: ServiceQueueCount | undefined = health.data.queues.find((iter) => iter.queueName == "headless_run_queue");
                    if (queue == undefined) {
                        this.queue.processingTime = null;
                        return;
                    }

                    this.queue.processingTime = queue.median;
                }
            },

            makeSignalRConnection: async function(): Promise<void> {
                console.log(`Match> creating connecion to sR`);
                if (this.connection != null) {
                    console.log(`Match> closing previous sR connection`);
                    await this.connection.stop();
                    console.log(`Match> closed previous sR connection`);
                }

                this.connection = new sR.HubConnectionBuilder()
                    .withUrl("/ws/headless-run")
                    .withAutomaticReconnect([5000, 10000, 20000, 20000])
                    .build();

                this.connection.on("UpdateProgress", (data: any) => {
                    // getting any updates on the progress means it's exited the queue, so the queue progress can be hidden
                    this.queue.index = -1;
                    clearInterval(this.queue.intervalId);
                    //console.log(JSON.stringify(data));
                    try {
                        this.replay.status = HeadlessRunStatus.parse(data);
                    } catch (err) {
                        console.error(err);
                    }
                });

                this.connection.on("Finish", () => {
                    this.replay.status = null;
                    this.queue.wasInQueue = true;
                });

                this.connection.onreconnected(() => {
                    console.log(`Match> sR reconnected!`);
                });

                this.connection.onreconnecting((err?: Error) => {
                    if (err) {
                        console.error("onreconnecting:", err);
                    } else {
                        console.log(`Match> sR reconnect started`);
                    }
                });

                this.connection.onclose((err?: Error) => {
                    if (err) {
                        console.error(`close:`, err);
                    } else {
                        console.log(`Match> sR connection closed`);
                    }
                });

                try {
                    await this.connection.start();
                    await this.connection.invoke("SubscribeToMatch", this.gameID);
                    console.log(`Match> successfully subscribed to updates [gameID=${this.gameID}]`);
                } catch (err) {
                    console.error("error during sR connection", err);
                }
            }
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
            },

            showHeadlessStatus: function(): boolean {
                return this.replay.status != null
                    && (
                        this.match.state != "loaded"
                        || this.match.data.processing == null
                        || this.match.data.processing.replaySimulated == null
                        // if the timestamp of the latest update is BEFORE the end time of simulation
                        || this.match.data.processing.replaySimulated.getTime() <= this.replay.status.timestamp.getTime()
                    )
            },

            showWaitForActions: function(): boolean {
                return this.match.state == "loaded"
                    && this.match.data.processing != null
                    && this.match.data.processing.replaySimulated != null
                    && this.match.data.processing.actionsParsed == null;
            },

            showClickToReload: function(): boolean {
                return this.queue.wasInQueue == true
                    && this.queue.index == -1
                    && this.replay.status == null;
            }

        },

        watch: {

        },

        components: {
            GexMenu, InfoHover, ApiError, ToggleButton,
            MatchOpener, MatchFactories, UnitDefView, MatchWindGraph, MatchUnitStats, TeamStatsChart, MatchResourceProduction,
            MatchTeams, MatchMap, MatchChat, MatchOption, MatchCombatStats, MatchEcoStats,
            ProcessingStep, Collapsible, Busy
        }
    });
    export default Match;
</script>