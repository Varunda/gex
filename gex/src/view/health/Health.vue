<template>
    <div>
        <div>
            <h3 class="d-inline">
                Latest update -
                <span v-if="latestUpdate != null">
                    {{latestUpdate | moment("yyyy-MM-dd hh:mm:ssa ZZZZ")}}
                    ::
                    {{latestUpdate | timeAgo}}
                </span>
            </h3>
        </div>

        <div v-if="health.state == 'loaded'">

            <div class="row">
                <div class="col-12">
                    <h1 class="wt-header">Services</h1>

                    <table class="table table-striped table-sm" style="table-layout: auto;">
                        <thead>
                            <tr class="table-secondary">
                                <th>service</th>
                                <th>enabled</th>
                                <th>last ran</th>
                                <th>duration</th>
                                <th>message</th>
                            </tr>
                        </thead>

                        <tbody>
                            <tr v-for="service in health.data.services" :key="service.name">
                                <td>{{service.name}}</td>
                                <td>
                                    <span :class="service.enabled == true ? 'text-success' : 'text-warning'">
                                        {{service.enabled}}
                                    </span>
                                    <span v-if="canManageServices" class="ms-3">
                                        <button v-if="!service.enabled" class="btn btn-sm btn-success" @click="enableService(service.name)">
                                            Enable
                                        </button>
                                        <button v-else-if="service.enabled" class="btn btn-sm btn-warning" @click="disableService(service.name)">
                                            Disable
                                        </button>
                                    </span>
                                </td>
                                <td>{{service.lastRan | moment("yyyy-MM-dd hh:mm:ssa ZZZZ")}}</td>
                                <td>{{service.runDuration / 1000 | mduration}}</td>
                                <td>{{service.message}}</td>
                            </tr>
                        </tbody>
                    </table>
                </div>

            </div>

            <div class="row">
                <div class="col-12">
                    <h1 class="wt-header">Queues</h1>

                    <table class="table table-striped table-sm" style="table-layout: fixed;">
                        <thead>
                            <tr class="table-secondary">
                                <th>Queue</th>
                                <th>Length</th>
                                <th>
                                    Processed
                                    <toggle-button v-model="settings.useCompact" class="btn-sm py-0">
                                        Compact
                                    </toggle-button>
                                </th>
                                <th title="Estimated time to complete the queue (if any items are in the queue)">
                                    ETA
                                </th>
                                <th>Average</th>
                                <th>Median</th>
                                <th>Min</th>
                                <th>Max</th>
                            </tr>
                        </thead>

                        <tbody>
                            <tr v-for="queue in health.data.queues" :key="queue.queueName">
                                <td>
                                    <span>
                                        {{queue.queueName}}
                                        <span v-if="canManageServices == true">
                                            <button class="btn btn-danger btn-sm py-0" @click="clearQueue(queue.typeName)">
                                                Clear
                                            </button>
                                        </span>
                                    </span>
                                </td>
                                <td :title="queue.count | locale">
                                    <span v-if="settings.useCompact">
                                        {{ queue.count | compact }}
                                    </span>
                                    <span v-else>
                                        {{ queue.count | locale }}
                                    </span>
                                </td>
                                <td :title="queue.processed | locale">
                                    <span v-if="settings.useCompact">
                                        {{queue.processed | compact}}
                                    </span>
                                    <span v-else>
                                        {{queue.processed | locale}}
                                    </span>
                                </td>

                                <td>
                                    <span v-if="queue.count > 0 && queue.median != null">
                                        {{ queue.count * (queue.median / 1000) | mduration }}
                                    </span>
                                    <span v-else class="text-muted">
                                        --
                                    </span>
                                </td>

                                <td>
                                    <span v-if="queue.average != null">
                                        {{queue.average | duration}}
                                    </span>
                                    <span v-else>
                                        --
                                    </span>
                                </td>
                                <td>
                                    <span v-if="queue.median != null">
                                        {{queue.median | duration}}
                                    </span>
                                    <span v-else>
                                        --
                                    </span>
                                </td>
                                <td>
                                    <span v-if="queue.min != null">
                                        {{queue.min | duration}}
                                    </span>
                                    <span v-else>
                                        --
                                    </span>
                                </td>
                                <td>
                                    <span v-if="queue.max != null">
                                        {{queue.max | duration}}
                                    </span>
                                    <span v-else>
                                        --
                                    </span>
                                </td>
                            </tr>
                        </tbody>    
                    </table>
                </div>
            </div>

            <div class="row">
                <div class="col-12">
                    <h1 class="wt-header">Headless Runs</h1>

                    <table class="table table-sm">
                        <thead>
                            <tr class="table-secondary">
                                <th>Game ID</th>
                                <th>running</th>
                                <th>frame</th>
                                <th>duration</th>
                                <th>fps</th>
                                <th>eta</th>
                            </tr>
                        </thead>

                        <tbody>
                            <tr v-for="run in health.data.headlessRuns" :key="run.gameID">
                                <td>
                                    <a :href="'/match/' + run.gameID">
                                        {{ run.gameID }}
                                    </a>
                                </td>
                                <td>{{ run.simulating }}</td>
                                <td>{{ run.frame }} ({{ run.frame / run.durationFrames * 100 | locale(2) }}%)</td>
                                <td>{{ run.durationFrames }}</td>
                                <td>{{ run.fps | locale(2) }}</td>
                                <td>
                                    <span v-if="run.simulating == true">
                                        {{ (run.durationFrames - run.frame) / run.fps | mduration }}
                                    </span>
                                    <span v-else>
                                        --
                                    </span>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { AppHealth, AppHealthApi } from "api/AppHealthApi";

    import "filters/MomentFilter";
    import "filters/TimeAgoFilter";
    import "filters/LocaleFilter";
    import "filters/DurationFilter";
    import "filters/CompactFilter";

    import { GexMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";

    import AccountUtil from "util/Account";
import { QueueApi } from "api/QueueApi";

    interface EventQueueEntry {
        when: Date;
        count: number;
    }

    export const Health = Vue.extend({
        props: {

        },

        data: function() {
            return {
                health: Loadable.idle() as Loading<AppHealth>,
                latestUpdate: null as | Date | null,

                timerID: undefined as number | undefined,
                loadingData: false as boolean,

                settings: {
                    showGraph: true as boolean,
                    useCompact: true as boolean
                }
            }
        },

        created: function(): void {
            document.title = `Gex / Health`;

            this.updateHealth();
            this.timerID = setInterval(async () => {
                const diff: number = (this.latestUpdate == null) ? Number.MAX_SAFE_INTEGER : (new Date().getTime() - this.latestUpdate.getTime());
                
                if (this.loadingData == true && diff < 10000) {
                    return;
                }

                await this.updateHealth();
            }, 1000) as unknown as number;

            // Force an update in a separate interval from the API update to ensure the time ago
            //      displays are updated. This is useful if you have a poor connection, or nothing
            //      is getting updated for another reason
            setInterval(() => {
                let needsRefresh: boolean = this.latestUpdate == null;

                if (this.latestUpdate != null) {
                    const diff: number = new Date().getTime() - this.latestUpdate.getTime();

                    // Only refresh if new data hasn't been found in 1500ms. This prevents flickering
                    //      if the two intervals are out of sync. For example, if the updateHealth interval
                    //      ran every 500ms, then this interval ran 500ms after the updateHealth interval,
                    //      this would cause the data to flicker every 500ms, very annoying
                    if (diff > 1500) {
                        console.warn(`data is ${diff}ms old, forcing a refresh`);
                        needsRefresh = true;
                    }
                }

                if (needsRefresh == true) {
                    this.$forceUpdate();
                }
            }, 1000);
        },

        methods: {
            updateHealth: async function(): Promise<void> {
                this.loadingData = true;
                this.health = await AppHealthApi.getHealth();
                this.loadingData = false;
                if (this.health.state == "loaded") {
                    this.latestUpdate = new Date();
                }
            },

            disableService: async function(name: string): Promise<void> {
                await AppHealthApi.disableService(name);
            },

            enableService: async function(name: string): Promise<void> {
                await AppHealthApi.enableService(name);
            },

            clearQueue: async function(name: string): Promise<void> {
                await QueueApi.clearQueue(name);
            }
        },

        computed: {

            canManageServices: function(): boolean {
                return AccountUtil.hasPermission("App.Account.Admin");
            }

        },

        components: {
            GexMenu, InfoHover, ToggleButton,
        }
    });
    export default Health;
</script>
