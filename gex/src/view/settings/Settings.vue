<template>
    <div>
        <h2 class="wt-header">User settings</h2>

        <div v-if="currentUser.ID == 0" class="alert alert-danger text-center">
            <div>
                <b>Not signed in</b>
            </div>

            <span>
                No account found. <a href="/login?returnUrl=settings">Login</a> to use this page
            </span>
        </div>

        <div v-else>
            <hr class="border">

            <collapsible header-text="Webhooks">
                <div>
                    <p class="mb-3">
                        Webhooks are URLs that Gex will send processed matches to. Webhooks can either be sent when games are
                        parsed <info-hover text="A game is parsed when Gex first sees a new game from the BAR API, downloads the demofile, then parses just the binary demofile"></info-hover>
                        or
                        replayed <info-hover text="A game is replayed when Gex replays the game locally, extracting events used for in depth stats"></info-hover>.
                    </p>

                    <div v-if="webhooks.state == 'idle'"></div>

                    <div v-else-if="webhooks.state == 'loading'">
                        <busy class="app-busy"></busy>
                    </div>

                    <div v-else-if="webhooks.state == 'loaded'">
                        <div v-if="webhooks.data.length == 10" class="alert alert-warning text-center">
                            <div>
                                <b>Max 10 webhooks</b>
                            </div>

                            <div>
                                Each user can only have a maximum of 10 webhooks.
                            </div>
                        </div>

                        <table class="table">
                            <thead class="table-secondary">
                                <tr>
                                    <th>Type</th>
                                    <th>URL</th>
                                    <th>Timestamp</th>
                                    <th>Shared secret</th>
                                    <th>Send test</th>
                                    <th>Delete</th>
                                </tr>
                            </thead>

                            <tbody>
                                <tr v-for="(webhook, index) in webhooks.data" :key="index">
                                    <td>
                                        {{ webhook.type }}
                                    </td>

                                    <td>
                                        <code>{{ webhook.url }}</code>
                                    </td>

                                    <td>
                                        {{ webhook.timestamp | moment }}
                                    </td>

                                    <td>
                                        <button class="btn btn-sm btn-info" @click="copySecret(webhook.sharedSecret)">
                                            Copy
                                        </button>
                                    </td>

                                    <td>
                                        <button class="btn btn-sm btn-primary" @click="sendTest(webhook.url, webhook.type, webhook.includeEvents, webhook.sharedSecret)">
                                            Send test
                                        </button>
                                    </td>

                                    <td>
                                        <button class="btn btn-sm btn-danger" @click="deleteWebhook(webhook.url, webhook.type, webhook.sharedSecret)">
                                            Delete
                                        </button>
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>

                    <div v-else-if="webhooks.state == 'error'">
                        <api-error :error="webhooks.problem"></api-error>
                    </div>

                    <div>
                        <h5>New webhook</h5>

                        <div class="mb-3">
                            <label>Type</label>
                            <select class="form-control" v-model="newWebhook.type">
                                <option value="parsed">Parsed</option>
                                <option value="replayed-events">Replayed (with events)</option>
                                <option value="replayed-noevents">Replayed (without events)</option>
                            </select>

                            <span>
                                <span v-if="newWebhook.type == 'parsed'">
                                    Gex will send a payload whenever a new match is parsed
                                </span>
                                <span v-else-if="newWebhook.type == 'replayed-events'">
                                    Gex will send a payload whenever a match is replayed, and will include the events
                                </span>
                                <span v-else-if="newWebhook.type == 'replayed-noevents'">
                                    Gex will send a payload whenever a match is replayed, but will NOT include the events
                                </span>
                            </span>
                        </div>

                        <div class="mb-3">
                            <label>URL</label>
                            <input class="form-control" type="text" v-model="newWebhook.url">
                        </div>

                        <div class="mb-3">
                            <label>Shared secret</label>
                            <input class="form-control" type="text" v-model="newWebhook.sharedSecret">

                            <span>
                                Gex will send this value in the <code>Authorization</code> header, use this to ensure payloads are coming from Gex
                            </span>
                        </div>

                        <div>
                            <button class="btn btn-primary" :disabled="!canCreateNewWebhook" @click="createWebhook">
                                Create webhook
                            </button>
                        </div>
                    </div>
                </div>
            </collapsible>

            <hr class="border">

            <div v-if="currentUser.permissions.length > 0">
                <h3>
                    Permissions
                </h3>

                This account has been granted the following permission:
                <span v-for="perm in currentUser.permissions" :key="perm.id" class="px-1 mx-1 border rounded">
                    <code>{{ perm.permission }}</code> 
                </span>
            </div>

        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import Collapsible from "components/Collapsible.vue";
    import InfoHover from "components/InfoHover.vue";
    import Busy from "components/Busy.vue";
    import ApiError from "components/ApiError";

    import { MatchProcessingWebhook } from "model/MatchProcessingWebhook";

    import { MatchProcessingWebhookApi } from "api/MatchProcessingWebhookApi";

    import AccountUtil, { AppCurrentAccount } from "util/Account";

    import "filters/MomentFilter";

    export const Settings = Vue.extend({
        props: {

        },

        data: function() {
            return {
                webhooks: Loadable.idle() as Loading<MatchProcessingWebhook[]>,

                newWebhook: {
                    url: "" as string,
                    type: "parsed" as "parsed" | "replayed-events" | "replayed-noevents",
                    sharedSecret: "" as string
                }
            }
        },

        mounted: function(): void {
            this.bindWebhooks();
        },

        methods: {

            copySecret: function(sharedSecret: string): void {
                navigator.clipboard.writeText(sharedSecret).then(() => {
                    Toaster.add("Copied", "copied secret to clipboard", "info");
                }).catch((err) => {
                    console.error(`Settings> failed to copy [err=${err}]`);
                    Toaster.add("Failed to copy", "failed to copy secret to clipboard. console may have more info", "danger");
                });
            },

            bindWebhooks: async function(): Promise<void> {
                this.webhooks = Loadable.loading();
                this.webhooks = await MatchProcessingWebhookApi.getByCurrentUser();
            },

            sendTest: async function(url: string, type: string, includeEvents: boolean, sharedSecret: string): Promise<void> {
                const res: Loading<void> = await MatchProcessingWebhookApi.sendTest(url, type, includeEvents, sharedSecret);

                if (res.state == "loaded") {
                    Toaster.add("Success", "sent payload to URL", "success");
                } else if (res.state == "error") {
                    Loadable.toastError(res, "Failed to send payload");
                } else {
                    Toaster.add("unchecked", `unchecked state of response: ${res.state}`, "warning");
                }
            },

            createWebhook: async function(): Promise<void> {
                const type: string = this.newWebhook.type.split("-")[0];

                const includeEvents: boolean = type == "replayed" && this.newWebhook.type.split("-")[1] == "events";

                const res: Loading<void> = await MatchProcessingWebhookApi
                    .create(this.newWebhook.url, type, includeEvents, this.newWebhook.sharedSecret);

                if (res.state == "loaded") {
                    Toaster.add("Success", "successfully created new webhook", "success");

                    this.newWebhook.url = "";
                    this.newWebhook.type = "parsed";
                    this.newWebhook.sharedSecret = "";
                } else if (res.state == "error") {
                    Loadable.toastError(res, "Failed to create webhook");
                } else {
                    Toaster.add("unchecked", `unchecked state of response: ${res.state}`, "warning");
                }

                this.bindWebhooks();
            },

            deleteWebhook: async function(url: string, type: string, sharedSecret: string): Promise<void> {
                const conf: string | null = prompt(`Confirm deletion of webhook to ${url}. Type "yes" (without quotes) to confirm`);
                if (conf == null || conf.toLowerCase() != "yes") {
                    return;
                }

                const res: Loading<void> = await MatchProcessingWebhookApi.deleteWebhook(url, type, sharedSecret);
                this.bindWebhooks();
            }

        },

        computed: {
            currentUser: function(): AppCurrentAccount {
                return AccountUtil.get();
            },

            canCreateNewWebhook: function(): boolean {
                return this.newWebhook.url != ""
                    && this.newWebhook.sharedSecret != ""
                    && (this.webhooks.state != "loaded" || this.webhooks.data.length < 10);
            }

        },

        components: {
            Collapsible, InfoHover, Busy, ApiError
        }
    });
    export default Settings;
</script>