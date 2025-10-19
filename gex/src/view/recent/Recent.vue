<template>
    <div style="max-width: 100vw">
        <div v-if="searching == false" class="d-flex" style="gap: 0.5rem;">
            <toggle-button v-model="showUnprocessedGames">
                Show unprocessed
            </toggle-button>

            <button class="btn btn-primary" @click="showSearch">
                Show search
            </button>
        </div>

        <hr class="border" />

        <div v-if="searching == true" class="row mb-3">
            <div class="col-12">
                <h4 @click="hideSearch">
                    Search options
                    <span v-if="searching == true">
                        &times;
                    </span>
                </h4>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label for="search-engine">Engine</label>
                <dropdown-search name="search-engine" v-model="search.engine" :api="dropdownSearchCalls.engine"></dropdown-search>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Game version</label>
                <dropdown-search v-model="search.gameVersion" :api="dropdownSearchCalls.gameVersion"></dropdown-search>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Map</label>
                <dropdown-search v-model="search.map" :api="dropdownSearchCalls.map"></dropdown-search>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Gamemode</label>
                <select v-model.number="search.gamemode" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="1">Duel</option>
                    <option :value="2">Small team</option>
                    <option :value="3">Large team</option>
                    <option :value="4">FFA</option>
                    <option :value="5">Team FFA</option>
                    <option :value="0">Other</option>
                </select>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Start time before</label>
                <date-time-input v-model="search.startTimeBefore" :allow-null="true"></date-time-input>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Start time after</label>
                <date-time-input v-model="search.startTimeAfter" :allow-null="true"></date-time-input>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Minimum player count</label>
                <input v-model.number="search.playerCountMinimum" class="form-control" type="number">
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Maximum player count</label>
                <input v-model.number="search.playerCountMaximum" class="form-control" type="number">
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Downloaded?</label>
                <select v-model="search.processingDownloaded" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Parsed?</label>
                <select v-model="search.processingParsed" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Replayed?</label>
                <select v-model="search.processingReplayed" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Actions parsed?</label>
                <select v-model="search.processingAction" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label for="search-ranked">Ranked?</label>
                <select v-model="search.ranked" class="form-control" name="search-ranked">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Legion enabled</label>
                <select v-model="search.legionEnabled" class="form-control">
                    <option :value="null">Unset</option>
                    <option :value="false">No</option>
                    <option :value="true">Yes</option>
                </select>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Game link</label>
                <input v-model="search.gameID" class="form-control" placeholder="Put replay link or game ID here">
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Game settings</label>
                <div class="input-group">
                    <input v-model="gameSetting.key" class="form-control"/>
                    <select v-model="gameSetting.operation" class="form-control">
                        <option value="eq">equals</option>
                        <option value="ne">not equals</option>
                    </select>
                    <input v-model="gameSetting.value" class="form-control" placeholder="game setting value"/>
                    <button class="btn btn-success" @click="addGameSetting" :disabled="gameSetting.key == ''" :title="gameSetting.key == '' ? 'cannot add as there is no key' : ''">add</button>
                </div>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Order by</label>
                <select v-model="search.orderBy" class="form-control">
                    <option value="start_time">Start time</option>
                    <option value="duration">Duration</option>
                    <option value="player_count">Player count</option>
                </select>
            </div>

            <div class="col-lg-3 col-md-4 col-sm-6 col-12">
                <label>Order direction</label>
                <select v-model="search.orderByDir" class="form-control">
                    <option value="desc">Desc</option>
                    <option value="asc">Asc</option>
                </select>
            </div>

            <div v-if="search.gameSettings.length > 0" class="col-12 mt-2">
                <label>Game setting search values</label>
                <div class="">
                    <div v-for="gs in search.gameSettings" :key="gs.key" class="my-3 mx-2 d-flex">
                        <span class="flex-grow-0">
                            {{ gs.key }}
                            <span v-if="gs.operation == 'eq'">
                                =
                            </span>
                            <span v-else-if="gs.operation == 'ne'">
                                !=
                            </span>
                            {{ gs.value }}
                        </span>

                        <span class="flex-grow-0 text-danger border px-1 py-0 ms-3 rounded" @click="removeGameSetting(gs.key)">
                            &times;
                        </span>
                    </div>
                </div>
            </div>

            <div class="col-12">
                <label for="user-search-input-bottom">User search</label>
                <div class="form-control user-input-parent" style="background-color: var(--bs-tertiary-bg); width: fit-content;">
                    <button v-for="user in search.users" :key="user.userID" class="btn btn-primary btn-sm rounded mx-2">
                        {{ user.username }}
                        <span class="close" @click="removeUser(user.userID)">
                            &times;
                        </span>
                    </button>
                    
                    <input class="pe-0 d-inline-block form-control border-0" style="background-color: inherit; outline: 0; width: auto;" placeholder="Search..." v-model="userInput" id="user-search-input-bottom" />
                </div>
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
                    <a v-if="offset > 24" :href="'/recent?offset=0' + searchParam" class="btn btn-primary me-2 mobile-after">
                        First
                    </a>

                    <a :href="'/recent?offset=' + (offset - 24) + searchParam" v-if="offset >= 24" class="btn btn-primary mobile-after">
                        Newer
                    </a>

                    <div class="flex-grow-1"></div>

                    <a v-if="recent.data.length > 0" :href="'/recent?offset=' + (offset + 24) + searchParam" class="btn btn-primary">
                        Older
                    </a>
                </div>

                <match-list :matches="recent.data"></match-list>

                <div v-if="recent.data.length == 0">
                    No matches found!
                </div>

                <hr class="border">

                <div class="d-flex flex-wrap">
                    <a v-if="offset > 24" :href="'/recent?offset=0' + searchParam" class="btn btn-primary me-2 mobile-after">
                        First
                    </a>

                    <a :href="'/recent?offset=' + (offset - 24) + searchParam" v-if="offset >= 24" class="btn btn-primary mobile-after">
                        Newer
                    </a>

                    <div class="flex-grow-1"></div>

                    <span class="text-center fetch-interval">Matches are fetched every 5 minutes. Only public PvP matches without any bots are included</span>

                    <div class="flex-grow-1"></div>

                    <a v-if="recent.data.length > 0" :href="'/recent?offset=' + (offset + 24) + searchParam" class="btn btn-primary">
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

    .user-input-parent:focus-within {
        outline: white solid 1px;
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
    import DateTimeInput from "components/DateTimeInput.vue";

    import { BarMatch, SearchKeyValue } from "model/BarMatch";
    import { BarMatchApi } from "api/BarMatchApi";
    import { MatchSearchApi } from "api/MatchSearchApi";
    import { BarUserApi } from "api/BarUserApi";

    import { BarUser } from "model/BarUser";
    import { UserSearchResult } from "model/UserSearchResult";

    import "filters/MomentFilter";

    import Tribute, { TributeItem } from "tributejs";

    type MinUser = {
        username: string;
        userID: number;
    }

    export const Recent = Vue.extend({
        props: {

        },

        data: function() {
            return {
                searching: false as boolean,
                showUnprocessedGames: false as boolean,

                gameIdRegex: new RegExp(/[0-9a-f]{32}/) as RegExp,
                enterLockout: 0 as number,

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
                    gamemode: null as number | null,
                    playerCountMinimum: 0 as number,
                    playerCountMaximum: 0 as number,
                    legionEnabled: null as boolean | null,
                    gameSettings: [] as SearchKeyValue[],
                    users: [] as MinUser[],
                    processingDownloaded: null as boolean | null,
                    processingParsed: null as boolean | null,
                    processingReplayed: null as boolean | null,
                    processingAction: null as boolean | null,
                    orderBy: "start_time" as string,
                    orderByDir: "desc" as string,
                },

                userInput: "" as string,
                inputTimeout: -1 as number,
                users: Loadable.idle() as Loading<UserSearchResult[]>,

                tribute: null as Tribute<UserSearchResult> | null,

                gameSetting: {
                    key: "" as string,
                    value: "" as string,
                    operation: "eq" as "eq" | "ne"
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
                const blob = JSON.parse(atob(b64));
                if (blob.startTimeBefore) {
                    blob.startTimeBefore = new Date(blob.startTimeBefore);
                }
                if (blob.startTimeAfter) {
                    blob.startTimeAfter = new Date(blob.startTimeAfter);
                }
                blob.orderBy ??= "start_time";
                blob.orderByDir ??= "desc";
                blob.gameSettings ??= [];
                blob.users ??= [];
                this.search = blob;
                this.performSearch();
            } else {
                this.loadRecent();
            }
        },

        mounted: function(): void {
            this.$nextTick(() => {
                this.tribute = new Tribute<UserSearchResult>({
                    trigger: "", // don't trigger on anything special
                    menuShowMinLength: 3, // tag search requires at least 2 characters
                    autocompleteMode: true, // autocomplete this, not sure what false does tho
                    allowSpaces: false, // tags can't have spaces

                    // attribute from a |ExtendedTag| that is inserted into the <textarea>
                    fillAttr: "username",

                    // big government doesn't want you to know this,
                    // but despite it being named |itemClass|, you can in fact put classes in here
                    itemClass: "bg-dark border",
                    // now this one does require you to not have spaces
                    selectClass: "fw-bold",

                    //menuContainer: document.getElementById("post-search-parent") || undefined,
                    //positionMenu: false,

                    // required, otherwise remote search doesn't work
                    searchOpts: {
                        pre: "",
                        post: "",
                        skip: true // this means don't do a local search
                    },

                    noMatchTemplate: () => {
                        return "<li><span class=\"bg-secondary\">not found</span></li>";
                    },

                    // remote callback
                    values: (text: string, callback: (r: UserSearchResult[]) => void) => {
                        if (text.length <= 2) {
                            return;
                        }

                        console.log(`Recent> performing user search [text=${text}]`);
                        BarUserApi.search(text, false, false).then((value: Loading<UserSearchResult[]>) => {
                            if (value.state == "loaded") {
                                console.log(`loaded searched tags: [${value.data.map(iter => iter.username).join(" ")}]`);
                                callback(value.data.slice(0, 10));
                            }
                        });
                    },

                    // change the menu template to have the color of the tag type
                    menuItemTemplate: (item: TributeItem<UserSearchResult>): string => {
                        return `<span contentediable="false" data-user-id=${item.original.userID}>${item.original.username}</span>`;
                    }
                });

                this.hideSearch();
            });
        },

        methods: {
            showSearch: function(): void {
                this.searching = true;

                this.$nextTick(() => {
                    if (this.tribute == null) {
                        console.log(`Recent> tribute container is not present`);
                        return;
                    }

                    const elem = document.getElementById("user-search-input-bottom");
                    if (!elem) {
                        throw `failed to find #user-search-input-bottom`;
                    }
                    this.tribute.attach(elem);
                    console.log(`Recent> tribute attached to #user-search-input-bottom`);

                    // i don't think these actually matter
                    elem.addEventListener("tribute-replaced", (ev: any) => {
                        this.enterLockout = Date.now() + 200;
                        this.userInput = "";
                        this.search.users.push(ev.detail.item.original);
                    });

                    elem.addEventListener("tribute-no-match", (ev: any) => {
                        console.log(`Recent> no match event emitted`);
                    });
                });
            },

            hideSearch: function(): void {
                this.searching = false;

                const elem = document.getElementById("user-search-input-bottom");
                if (elem) {
                    this.tribute?.detach(elem);
                } else {
                    console.warn(`Recent> missing #user-search-input-bottom to detach tribute from`);
                }
            },

            enterPress: function(): void {
                const lockoutDiff: number = this.enterLockout - Date.now();
                console.log(`Recent> lockoutDiff is ${lockoutDiff}`);
                if (lockoutDiff >= 0) {
                    console.log(`Recent> enter lockout hit (diff is ${lockoutDiff})`);
                    this.enterLockout = Date.now(); // if the lockout is hit once, let the next enter key do the search
                    return;
                }

                const container: HTMLElement | null = document.querySelector(".tribute-container");
                if (container == null) {
                    console.warn(`query .tribute-container nothing returned`);
                    this.performSearch();
                    return;
                }

                if (container.style.display != "none") {
                    console.log(`not sending @do-search, auto-complete tab is opened`);
                    return;
                }

                this.performSearch();
            },

            loadRecent: async function(): Promise<void> {
                this.recent = Loadable.loading();

                if (this.showUnprocessedGames == true) {
                    this.recent = await BarMatchApi.getRecent(this.offset);
                } else {
                    this.recent = await BarMatchApi.search(this.offset, 24, this.search.orderBy, this.search.orderByDir, {
                        processingAction: true
                    });
                }
            },

            doSearchWrapper: async function(): Promise<void> {
                this.search.use = true;
                this.offset = 0;
                const url = new URL(location.href);
                history.pushState({ path: url.href }, "", `/recent?offset=0${this.searchParam}`);

                this.performSearch();
            },

            performSearch: async function(): Promise<void> {
                this.search.use = true;

                this.recent = Loadable.loading();

                if (this.search.gameID != "") {
                    const reg: RegExpExecArray | null = this.gameIdRegex.exec(this.search.gameID);
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

                this.recent = await BarMatchApi.search(this.offset, 24, this.search.orderBy, this.search.orderByDir, this.searchOptions);
            },

            addGameSetting: function(): void {
                this.removeGameSetting(this.gameSetting.key);

                this.search.gameSettings.push({
                    key: this.gameSetting.key,
                    value: this.gameSetting.value,
                    operation: this.gameSetting.operation,
                })

                this.gameSetting.key = "";
                this.gameSetting.value = "";
                this.gameSetting.operation = "eq";
            },

            removeGameSetting: function(key: string): void {
                this.search.gameSettings = this.search.gameSettings.filter(iter => iter.key != key);
            },

            searchWrapper: function(): void {
                clearTimeout(this.inputTimeout);

                const searchTerm: string = this.userInput;

                if (searchTerm.length >= 3) {
                    this.inputTimeout = setTimeout(async () => {
                        console.log(`Recent> doing user search [searchTerm=${searchTerm}]`);
                        this.users = Loadable.loaded([]);
                        const ret: Loading<UserSearchResult[]> = await BarUserApi.search(searchTerm, false, false);
                        if (ret.state != "loaded") {
                            this.users = Loadable.rewrap(ret);
                            return;
                        }

                        this.users = Loadable.loaded(ret.data.slice(0, 20));
                    }, 300) as unknown as number;
                }
            },

            removeUser: function(userID: number): void {
                this.search.users = this.search.users.filter(iter => iter.userID != userID);
            }
        },

        computed: {

            searchedUsers: function(): UserSearchResult[] {
                if (this.users.state != "loaded") {
                    return [];
                }

                return this.users.data;
            },

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

                options.orderBy = this.search.orderBy;
                options.orderByDir = this.search.orderByDir;

                if (this.search.engine && this.search.engine != "") {
                    options.engine = this.search.engine;
                }
                if (this.search.gameVersion && this.search.gameVersion != "") {
                    options.gameVersion = this.search.gameVersion;
                }
                if (this.search.map && this.search.map != "") {
                    options.map = this.search.map;
                }
                if (this.search.gamemode != null) {
                    options.gamemode = this.search.gamemode;
                }
                if (this.search.ranked != null) {
                    options.ranked = this.search.ranked;
                }
                if (this.search.startTimeBefore != null) {
                    options.startTimeBefore = this.search.startTimeBefore;
                }
                if (this.search.startTimeAfter != null) {
                    options.startTimeAfter = this.search.startTimeAfter;
                }
                if (this.search.playerCountMinimum != 0 && this.search.playerCountMinimum != undefined) {
                    options.playerCountMinimum = this.search.playerCountMinimum;
                }
                if (this.search.playerCountMaximum != 0 && this.search.playerCountMaximum != undefined) {
                    options.playerCountMaximum = this.search.playerCountMaximum;
                }
                if (this.search.legionEnabled != null) {
                    options.legionEnabled = this.search.legionEnabled;
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
                if (this.search.gameSettings.length > 0) {
                    options.gameSettings = this.search.gameSettings;
                }
                if (this.search.users.length > 0) {
                    options.users = this.search.users.map(iter => { return { username: iter.username, userID: iter.userID }; });
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
            MatchList, DropdownSearch, ToggleButton, ApiError, DateTimeInput
        }
    });

    export default Recent;
</script>