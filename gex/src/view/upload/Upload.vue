
<template>
    <div>
        <div class="d-flex">
            <div class="flex-grow-1"></div>

            <div class="flex-grow-2" style="max-width: 640px;">
                <div class="h1 text-center">
                    upload
                </div>

                <div>
                    Uploading a game makes the game public, and anyone can view it. Do not upload games that others want to keep private
                </div>

                <div class="input-group">
                    <input id="file-upload" type="file" class="form-control" @change="updateName" />
                    <label class="custom-file-label" for="file-upload"></label>
                </div>

                <div class="text-center mb-3">
                    max 50MB upload
                </div>

                <div>
                    Replays are located within the demos file of your game install
                </div>

                <div class="w-100">
                    <button class="btn btn-primary w-100" @click="doUpload">Upload</button>
                </div>
            </div>

            <div class="flex-grow-1"></div>
        </div>

        <hr class="border">

        <div class="text-center">
            <div v-if="match.state == 'idle'"></div>

            <div v-else-if="match.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="match.state == 'loaded'">
                <a :href="'/match/' + match.data.id">View match</a>
            </div>

            <div v-else-if="match.state == 'error'" class="alert alert-danger d-inline-block">
                <b>Failed to upload match:</b>
                <br>
                <span v-if="match.problem.instance != ''">
                    {{ match.problem.title }}
                </span>
                <span v-else>
                    {{ match.problem.detail }}
                </span>
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

    import { BarMatch } from "model/BarMatch";

    import Toaster from "Toaster";

    export const Upload = Vue.extend({
        props: {

        },

        data: function() {
            return {
                file: null as HTMLInputElement | null,
                fileText: "" as string,
                match: Loadable.idle() as Loading<BarMatch>
            };
        },

        created: function(): void {
            document.title = "Gex / Upload";
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.makeFile();
            });
        },

        methods: {
            makeFile: function(): void {
                this.file = document.getElementById("file-upload") as HTMLInputElement | null;
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

                    this.match = await MatchUploadApi.upload(f);
                    if (this.match.state != "loaded") {
                        Loadable.toastError(this.match, "Upload error");
                        //Toaster.add("Upload error", `error uploading!`, "danger");
                    }
                }
            }
        },

        computed: {

        },

        watch: {

        },

        components: {
            InfoHover, ApiError, ToggleButton, Collapsible, Busy
        }
    });
    export default Upload;
</script>