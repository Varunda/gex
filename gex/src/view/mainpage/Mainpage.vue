<template>
    <div style="max-width: 100vw">
        <div v-if="searching == false">
            <toggle-button v-model="showUnprocessedGames">
                Show unprocessed
            </toggle-button>

            <button class="btn btn-primary" @click="searching = true">
                Search options
            </button>
        </div>

        <hr class="border" />

        <div v-if="searching == true" class="row mb-3">

            <div class="col-12">
                <h4 @click="searching = false">
                    Search options
                    <span v-if="searching == true">
                        &times;
                    </span>
                </h4>
            </div>

            <div class="col-lg-4 col-12">
                <label>Engine</label>
                <dropdown-search v-model="search.engine" :api="dropdownSearchCalls.engine"></dropdown-search>
            </div>

            <div class="col-lg-4 col-12">
                <label>Game version</label>
                <dropdown-search v-model="search.gameVersion" :api="dropdownSearchCalls.gameVersion"></dropdown-search>
            </div>

            <div class="col-lg-4 col-12">
                <label>Map</label>
                <dropdown-search v-model="search.map" :api="dropdownSearchCalls.map"></dropdown-search>
            </div>

            <div class="col-lg-4 col-12">
                <label>Ranked?</label>
                <select v-model="search.ranked" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-4 col-12">
                <label>Downloaded?</label>
                <select v-model="search.processingDownloaded" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-4 col-12">
                <label>Parsed?</label>
                <select v-model="search.processingParsed" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-4 col-12">
                <label>Replayed?</label>
                <select v-model="search.processingReplayed" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-4 col-12">
                <label>Actions parsed?</label>
                <select v-model="search.processingAction" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-4 col-12">
                <label>Minimum player count</label>
                <input v-model.number="search.playerCountMinimum" class="form-control" type="number">
            </div>

            <div class="col-lg-4 col-12">
                <label>Maximum player count</label>
                <input v-model.number="search.playerCountMaximum" class="form-control" type="number">
            </div>

            <div class="col-lg-4 col-12">
                <label>Game link</label>
                <input v-model="search.gameID" class="form-control" placeholder="Put replay link or game ID here">
            </div>

            <div class="col-12 mt-2">
                <button class="btn btn-primary" @click="doSearchWrapper">Search</button>
            </div>

            <div class="col-12">
                <hr class="border">
            </div>

        </div>

        <div>
            <div v-if="recent.state == 'idle'"></div>

            <div v-else-if="recent.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="recent.state == 'loaded'">

                <div class="d-flex flex-wrap d-md-none mb-3 pb-3 border-bottom">
                    <a v-if="offset > 24" :href="'/?offset=0' + searchParam" class="btn btn-primary me-2 mobile-after">
                        First
                    </a>

                    <a :href="'/?offset=' + (offset - 24) + searchParam" v-if="offset >= 24" class="btn btn-primary mobile-after">
                        Newer
                    </a>

                    <div class="flex-grow-1"></div>

                    <a v-if="recent.data.length > 0" :href="'/?offset=' + (offset + 24) + searchParam" class="btn btn-primary">
                        Older
                    </a>
                </div>

                <match-list :matches="recent.data"></match-list>

                <div v-if="recent.data.length == 0">
                    No matches found!
                </div>

                <hr class="border">

                <div class="d-flex flex-wrap">
                    <a v-if="offset > 24" :href="'/?offset=0' + searchParam" class="btn btn-primary me-2 mobile-after">
                        First
                    </a>

                    <a :href="'/?offset=' + (offset - 24) + searchParam" v-if="offset >= 24" class="btn btn-primary mobile-after">
                        Newer
                    </a>

                    <div class="flex-grow-1"></div>

                    <span class="text-center fetch-interval">Matches are fetched every 5 minutes. Only public matches without AI are included</span>

                    <div class="flex-grow-1"></div>

                    <a v-if="recent.data.length > 0" :href="'/?offset=' + (offset + 24) + searchParam" class="btn btn-primary">
                        Older
                    </a>
                </div>
            </div>

            <div v-else-if="recent.state == 'error'">
                <api-error :error="recent.problem"></api-error>
            </div>
        </div>
    </div>
</template>

<style scoped>

    @media (max-width: 768px) {
        .fetch-interval {
            order: 99;
        }
    }

</style>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import { GexMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";
    import MatchList from "components/app/MatchList.vue";
    import DropdownSearch from "components/DropdownSearch.vue";
    import ToggleButton from "components/ToggleButton";
    import ApiError from "components/ApiError";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchApi } from "api/BarMatchApi";
    import { MatchSearchApi } from "api/MatchSearchApi";

    import "filters/MomentFilter";

    export const Mainpage = Vue.extend({
        props: {

        },

        data: function() {
            return {
                searching: false as boolean,
                showUnprocessedGames: false as boolean,

                gameIdRegex: new RegExp(/[0-9a-f]{32}/) as RegExp,

                search: {
                    use: false as boolean,

                    gameID: "" as string,

                    engine: "" as string,
                    gameVersion: "" as string,
                    map: "" as string,
                    ranked: null as boolean | null,
                    startTimeAfter: null as Date | null,
                    startTimeBefore: null as Date | null,
                    durationMinimum: 0 as number,
                    durationMaximum: 0 as number,
                    gamemode: 0 as number,
                    playerCountMinimum: 0 as number,
                    playerCountMaximum: 0 as number,
                    processingDownloaded: null as boolean | null,
                    processingParsed: null as boolean | null,
                    processingReplayed: null as boolean | null,
                    processingAction: null as boolean | null,
                },

                offset: 0 as number,
                limit: 24 as number,
                recent: Loadable.idle() as Loading<BarMatch[]>
            }
        },

        created: function(): void {
            document.title = "Gex / Recent matches";
        },

        beforeMount: function(): void {
            const search: URLSearchParams = new URLSearchParams(location.search);
            if (search.has("offset")) {
                this.offset = Number.parseInt(search.get("offset")!);
            }

            if (search.has("showUnprocessed")) {
                this.showUnprocessedGames = search.get("showUnprocessed") == "true";
            }

            if (search.has("search") && search.get("search") != "") {
                const b64: string = search.get("search")!;
                this.search = JSON.parse(atob(b64));
                this.performSearch();
            } else {
                this.loadRecent();
            }

        },

        methods: {

            showSearch: function(): void {
                this.searching = true;
            },

            loadRecent: async function(): Promise<void> {
                this.recent = Loadable.loading();

                if (this.showUnprocessedGames == true) {
                    this.recent = await BarMatchApi.getRecent(this.offset);
                } else {
                    this.recent = await BarMatchApi.search(this.offset, 24, {
                        processingAction: true
                    });
                }
            },

            doSearchWrapper: async function(): Promise<void> {
                this.search.use = true;
                this.offset = 0;
                const url = new URL(location.href);
                history.pushState({ path: url.href }, "", `/?offset=0${this.searchParam}`);

                this.performSearch();
            },

            performSearch: async function(): Promise<void> {
                this.search.use = true;

                this.recent = Loadable.loading();

                if (this.search.gameID != "") {
                    const reg: RegExpExecArray | null = this.gameIdRegex.exec(this.search.gameID);
                    console.log(reg);
                    if (reg != null && reg.length > 0) {
                        const first: string | undefined = reg.at(0);
                        if (first != undefined) {
                            const match: Loading<BarMatch> = await BarMatchApi.getByID(first);
                            if (match.state == "loaded") {
                                this.recent = Loadable.loaded([match.data]);
                            } else if (match.state == "nocontent") {
                                this.recent = Loadable.loaded([]);
                            } else {
                                this.recent = Loadable.rewrap(match);
                            }
                            return;
                        }
                    }
                }

                this.recent = await BarMatchApi.search(this.offset, 24, this.searchOptions);

            }
        },

        computed: {

            searchParam: function(): string {
                if (this.showUnprocessedGames == true) {
                    return "&showUnprocessed=true";
                }

                if (this.search.use == false) {
                    return "";
                }

                return `&search=${btoa(JSON.stringify(this.searchOptions))}`;
            },

            searchOptions: function() {
                let options: any = {};

                if (this.search.engine && this.search.engine != "") {
                    options.engine = this.search.engine;
                }
                if (this.search.gameVersion && this.search.gameVersion != "") {
                    options.gameVersion = this.search.gameVersion;
                }
                if (this.search.map && this.search.map != "") {
                    options.map = this.search.map;
                }
                if (this.search.ranked != null) {
                    options.ranked = this.search.ranked;
                }
                if (this.search.playerCountMinimum != 0) {
                    options.playerCountMinimum = this.search.playerCountMinimum;
                }
                if (this.search.playerCountMaximum != 0) {
                    options.playerCountMaximum = this.search.playerCountMaximum;
                }
                if (this.search.processingDownloaded != null) {
                    options.processingDownloaded = this.search.processingDownloaded;
                }
                if (this.search.processingParsed != null) {
                    options.processingParsed = this.search.processingParsed;
                }
                if (this.search.processingReplayed != null) {
                    options.processingReplayed = this.search.processingReplayed;
                }
                if (this.search.processingAction != null) {
                    options.processingAction = this.search.processingAction;
                }

                return options;
            },

            dropdownSearchCalls: function() {
                return {
                    engine: MatchSearchApi.getUniqueEngines,
                    gameVersion: MatchSearchApi.getUniqueGameVersions,
                    map: MatchSearchApi.getUniqueMaps
                }
            }

        },

        watch: {
            showUnprocessedGames: function(): void {
                this.loadRecent();
            }
        },

        components: {
            InfoHover, GexMenu,
            MatchList, DropdownSearch, ToggleButton, ApiError
        }
    });

    export default Mainpage;
</script>