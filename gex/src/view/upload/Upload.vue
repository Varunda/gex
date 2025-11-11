
<template>
    <div>
        <div class="d-flex">
            <div class="flex-grow-1"></div>

            <div class="flex-grow-2" style="max-width: 640px;">
                <div class="h1 text-center">
                    upload
                </div>

                <div>
                    Uploading a match makes the match public, and anyone can view it. Do not upload matches that others want
                    to keep private
                </div>

                <div class="input-group">
                    <input id="file-upload" type="file" class="form-control" @change="updateName" multiple="true" />
                    <label class="custom-file-label" for="file-upload"></label>
                </div>

                <div class="text-center mb-3">
                    max 50MB upload
                </div>

                <div>
                    Replays are located within the demos file of the game install
                </div>

                <div class="w-100">
                    <button class="btn btn-primary w-100" @click="doUpload">Upload</button>
                </div>

                <hr class="border" />

                <div class="w-100 mb-3" v-if="hasAddRemovePermission">
                    <label>match pool upload</label>

                    <select v-model.number="selectedPoolID" class="form-control">
                        <option :value="undefined">none</option>
                        <template v-if="pools.state == 'loaded'">
                            <option v-for="pool in pools.data" :key="pool.id" :value="pool.id">
                                {{ pool.name }}
                            </option>
                        </template>
                    </select>

                    <span v-if="selectedPool != null">
                        matches uploaded will be added to the match pool: {{ selectedPool.name }}
                    </span>
                    <span v-else>
                        matches uploaded will not be added to a match pool
                    </span>
                </div>

                <div v-if="hasUpdatePriorityPermission" class="mb-3 w-100">
                    <label class="d-block">update priority on upload</label>
                    <toggle-button v-model="updatePrio" class="mb-2">update prio?</toggle-button>

                    <span v-if="updatePrio == true">
                        <input v-model.number="prioValue" class="form-control" type="number"/>

                        matches once uploaded will be updated with a priority of {{ prioValue }}
                    </span>
                </div>
            </div>

            <div class="flex-grow-1"></div>
        </div>

        <hr class="border">

        <div v-if="uploadCount > 1" class="text-center">
            Games uploaded: {{ uploadedCount }} / {{ uploadCount }}
        </div>

        <div class="text-center">
            <div v-if="uploadCount > 0">
                <div class="progress" style="height: 2rem;">
                    <div class="progress-bar" :style="{ 'width': `${uploadedCount / uploadCount * 100}%`, 'height': '2rem' }"></div>
                </div>
            </div>

            <div>
                <div>matches uploaded:</div>

                <div class="border-bottom pb-3 mb-3">
                    <a v-for="matchID in matchesUploaded" :key="matchID" :href="'/match/' + matchID" target="_blank" ref="nofollow" class="d-block font-monospace">
                        {{ matchID }}
                    </a>
                </div>

                <pre>{{ uploadedMatchIds }}</pre>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading"

    import "filters/MomentFilter";

    import ApiError from "components/ApiError";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";
    import Collapsible from "components/Collapsible.vue";
    import Busy from "components/Busy.vue";

    import { MatchUploadApi } from "api/MatchUploadApi";
    import { MatchPoolApi } from "api/MatchPoolApi";
    import { BarMatchProcessingApi } from "api/BarMatchProcessingApi";

    import { BarMatch } from "model/BarMatch";
    import { MatchPool } from "model/MatchPool";

    import AccountUtil from "util/Account";
    import Toaster from "Toaster";

    export const Upload = Vue.extend({
        props: {

        },

        data: function() {
            return {
                file: null as HTMLInputElement | null,
                fileText: "" as string,
                match: Loadable.idle() as Loading<BarMatch>,

                uploadCount: 0 as number,
                uploadedCount: 0 as number,

                pools: Loadable.idle() as Loading<MatchPool[]>,
                selectedPoolID: undefined as number | undefined,

                updatePrio: false as boolean,
                prioValue: 10 as number,

                matchesUploaded: [] as string[]
            };
        },

        created: function(): void {
            document.title = "Gex / Upload";
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeFile();
                this.loadPools();
            });
        },

        methods: {
            makeFile: function(): void {
                this.file = document.getElementById("file-upload") as HTMLInputElement | null;
            },

            loadPools: async function(): Promise<void> {
                this.pools = Loadable.loading();
                this.pools = await MatchPoolApi.getAll();
            },

            updateName: function(ev: any): void {
                const files: FileList = ev.target.files;
                if (files.length > 0) {
                    this.fileText = files.item(0)?.name ?? "missing name";
                }
            },

            doUpload: async function(): Promise<void> {
                const files: FileList = (this.file as any).files;

                if (files.length == 0) {
                    console.warn(`cannot upload, 0 files selected`);
                    return;
                }

                this.match = Loadable.loading();

                this.uploadCount = files.length;
                this.uploadedCount = 0;
                for (let i = 0; i < files.length; ++i) {
                    const f: File | null = files.item(i);

                    if (f == null) {
                        console.warn(`failed to get file at index ${i}`);
                        continue;
                    }

                    if (f.size > 1024 * 1024 * 50) {
                        this.match = Loadable.error("Uploads cannot be larger than 50MB");
                        console.error(`upload is too big! ${f.size} > ${1024 * 1024 * 50}`);
                        continue;
                    }

                    const match: Loading<BarMatch> = await MatchUploadApi.upload(f);
                    ++this.uploadedCount;
                    if (match.state != "loaded") {
                        Loadable.toastError(this.match, "Upload error");
                        //Toaster.add("Upload error", `error uploading!`, "danger");
                    } else {
                        const matchID: string = match.data.id;
                        if (this.matchesUploaded.find(iter => iter == matchID) == undefined) {
                            this.matchesUploaded.push(matchID);
                        }

                        if (this.selectedPoolID != undefined) {
                            console.log(`Upload> adding match to selected pool [poolID=${this.selectedPoolID}] [matchID=${match.data.id}]`);
                            await MatchPoolApi.addMatchToPool(this.selectedPoolID, match.data.id);
                        }

                        if (this.updatePrio == true) {
                            console.log(`Upload> updating match priority [matchID=${matchID}] [priority=${this.prioValue}]`);
                            await BarMatchProcessingApi.updatePriority(matchID, this.prioValue);
                        }
                    }
                }
            }
        },

        computed: {
            selectedPool: function(): MatchPool | null {
                if (this.selectedPoolID == undefined) {
                    return null;
                }

                if (this.pools.state != "loaded") {
                    return null;
                }

                return this.pools.data.find(iter => iter.id == this.selectedPoolID) ?? null;
            },

            hasAddRemovePermission: function(): boolean {
                return AccountUtil.hasPermission("Gex.MatchPoolEntry.AddRemove");
            },

            hasUpdatePriorityPermission: function(): boolean {
                return AccountUtil.hasPermission("Gex.Match.ForceReplay");
            },

            uploadedMatchIds: function(): string {
                return this.matchesUploaded.join("\n");
            }
        },

        watch: {

        },

        components: {
            InfoHover, ApiError, ToggleButton, Collapsible, Busy
        }
    });
    export default Upload;
</script>