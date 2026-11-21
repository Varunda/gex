
<template>
    <div>
        <h2 class="wt-header">
            Tweaks
        </h2>

        <div class="alert alert-info text-center">
            <strong>Only PvP games included</strong>
            <br>
            Tweaks are only found from PvP games, and does not include PvE games. <a href="/faq#why-no-pve" target="_blank">Why not?</a>
        </div>

        <div class="row">
            <div class="col-6">
                <label>Sort by</label>
                <select class="form-control" v-model="sortBy">
                    <option value="count">Uses</option>
                    <option value="name">Name</option>
                    <option value="random">Random</option>
                </select>
            </div>

            <div class="col-6">
                <label>Sort direction</label>
                <select class="form-control" v-model="sortDir">
                    <option value="asc">Ascending</option>
                    <option value="desc">Descending</option>
                </select>
            </div>
        </div>

        <hr class="border"/>

        <div>
            <h3>Selected</h3>

            <div v-if="selected.length == 0" class="text-muted">
                No tweaks selected. Click the checkbox in tweaks below to generate commands to setup a lobby with tweaks
            </div>

            <div class="list-group mb-3">
                <draggable v-model="selected">
                    <div v-for="entry in selected" :key="entry.hash" class="list-group-item">
                        <span class="ph ph-dots-six-vertical"></span>

                        {{ entry.fullName }} 
                    </div>
                </draggable>
            </div>

            <button v-if="selected.length > 0" class="btn btn-primary" @click="copyLobbyCommands">
                Copy bset commands
            </button>

            <div v-if="selectedUnitTweakCount > 29 || selectedDefTweakCount > 29" class="alert alert-danger text-center">
                <strong>Too many tweaks selected</strong>

                <br>
                
                <div v-if="selectedDefTweakCount > 29">
                    At most 29 def tweaks can be selected ({{ selectedDefTweakCount }} def tweaks selected)
                </div>

                <div v-if="selectedUnitTweakCount > 29">
                    At most 29 unit tweaks can be selected ({{ selectedUnitTweakCount }} unit tweaks selected)
                </div>
            </div>
        </div>

        <hr class="border"/>

        <div v-if="entries.state == 'idle'"></div>

        <div v-else-if="entries.state == 'loading'">
            <busy class="busy busy-sm"></busy>
        </div>

        <div v-else-if="entries.state == 'loaded'">

            <h3 class="wt-header">Tweaks found ({{ groupedByHash.length | locale(0) }})</h3>

            <div class="mb-3">
                <div>
                    <label>Filter by name</label>
                    <input type="text" class="form-control" v-model="filter.name">
                </div>

            </div>

            <div v-for="entry in groupedByHash" :key="entry.hash" class="mb-3 border-bottom pb-3">
                <h4 class="mb-0 d-flex">
                    <div class="flex-grow-1">
                        <input type="checkbox" class="form-check-input" @click="toggleSelected(entry.hash)" :id="'tweak-toggle-' + entry.hash">

                        <div class="d-inline-flex flex-column">
                            <label :for="'tweak-toggle-' + entry.hash">
                                {{ entry.name || "&lt;no name&gt;" }}
                            </label>

                            <div class="fs-6">
                                <span>
                                    <label :for="'tweak-toggle-' + entry.hash">
                                        seen {{ entry.count | locale(0) }} times
                                    </label>
                                </span>
                            </div>
                        </div>
                    </div>

                    <div class="flex-grow-0 d-inline-flex flex-column" style="align-items: flex-end;">
                        <span class="font-monospace">{{ entry.base64.length }}:{{ entry.lobbyHash.slice(0, 4) }}</span>

                        <button class="btn btn-link p-0 border-0" data-bs-toggle="collapse" :data-bs-target="'#entry-code-' + entry.hash">
                            View lua code
                        </button>
                    </div>
                </h4>
                <div class="mb-2">
                </div>

                <div :id="'entry-code-' + entry.hash" class="collapse">
                    <pre style="background: var(--bs-light-bg-subtle)" class="p-2"><code>{{ entry.code }}</code></pre>
                </div>
            </div>

            <div>
                Loaded {{ entries.data.length | locale(0) }} tweaks
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import Toaster from "Toaster";

    import Busy from "components/Busy.vue";
    import Collapsible from "components/Collapsible.vue";
    import draggable from "vuedraggable";

    import "filters/LocaleFilter";

    import { BarTweakEntry } from "model/BarTweakEntry";

    import { BarTweakEntryApi } from "api/BarTweakEntryApi";

    export const Tweaks = Vue.extend({
        props: {

        },

        data: function() {
            return {
                entries: Loadable.idle() as Loading<BarTweakEntry[]>,

                selected: [] as BarTweakEntry[],

                sortBy: "count" as "count" | "name" | "random",
                sortDir: "desc" as "desc" | "asc",

                filter: {
                    name: "" as string
                }
            }
        },

        mounted: function(): void {
            document.title = "Gex / Tweaks";
            this.bindAll();
        },

        methods: {
            bindAll: async function(): Promise<void> {
                this.entries = Loadable.loading();
                this.entries = await BarTweakEntryApi.getAggregate();
            },

            toggleSelected: function(hash: string): void {
                console.log(`Tweaks> toggling selection [hash=${hash}]`);

                if (this.selected.find(iter => iter.hash == hash) != undefined) {
                    this.selected = this.selected.filter(iter => iter.hash != hash);
                } else {
                    const entry = this.groupedByHash.find(iter => iter.hash == hash);
                    if (entry == undefined) {
                        console.warn(`Tweak> missing hash [hash=${hash}]`);
                        return;
                    } else {
                        this.selected.push(entry);
                    }
                }
            },

            copyLobbyCommands: function(): void {
                let countUnit: number = 1;
                let countDef: number = 1;

                let text: string = "";
                for (const tweak of this.selected) {
                    let cmd = `!bset tweak${tweak.type}s`;

                    if (tweak.type == "unit") {
                        cmd += countUnit;
                        countUnit++;
                    } else if (tweak.type == "def") {
                        cmd += countDef;
                        countDef++;
                    } else {
                        Toaster.add("console error", "an error was thrown in console", "danger");
                        throw `unchecked type of tweak '${tweak.type}' (from hash ${tweak.hash}/${tweak.name})`;
                    }

                    cmd += ` ${tweak.base64}\n`;
                    text += cmd;
                }

                navigator.clipboard.writeText(text).then(() => {
                    Toaster.add(`Copied ${this.selected.length} tweaks`, "Copied lobby commands to clipboard", "info");
                }).catch((err) => {
                    console.error(`Tweaks> failed to copy lobby commands[err=${err}]`);
                    Toaster.add("Failed to copy", "failed to copy lobby commands to clipboard. console may have more info", "danger");
                });
            },

            copyTweak: function(hash: string): void {
                if (this.entries.state != "loaded") {
                    return;
                }

                const entry: BarTweakEntry | undefined = this.entries.data.find(iter => iter.hash == hash);
                if (entry == undefined) {
                    return;
                }

                navigator.clipboard.writeText(entry.base64).then(() => {
                    Toaster.add(`Copied ${entry.name}`, "copied tweak base64 to clipboard", "info");
                }).catch((err) => {
                    console.error(`Tweaks> failed to copy [hash=${hash}] [err=${err}]`);
                    Toaster.add("Failed to copy", "failed to copy tweak to clipboard. console may have more info", "danger");
                });
            }

        },

        computed: {
            groupedByHash: function(): BarTweakEntry[] {
                if (this.entries.state != "loaded") {
                    return [];
                }

                const map: Map<string, BarTweakEntry> = new Map();

                for (const entry of this.entries.data) {
                    if (this.filter.name != "") {
                        if (entry.name.toLowerCase().indexOf(this.filter.name.toLowerCase()) == -1) {
                            continue;
                        }
                    }

                    if (map.has(entry.hash) == false) {
                        map.set(entry.hash, entry);
                    } else {
                        map.get(entry.hash)!.count += entry.count;
                    }
                }

                let entries: BarTweakEntry[] = Array.from(map.values());
                
                if (this.sortBy != "random") {
                    entries.sort((a, b) => {
                        if (this.sortDir == "desc") {
                            [a, b] = [b, a];
                        }

                        if (this.sortBy == "count") {
                            return a.count - b.count;
                        } else if (this.sortBy == "name") {
                            return a.name.localeCompare(b.name);
                        } else {
                            throw `unchecked sortBy '${this.sortBy}'`;
                        }
                    });
                } else {
                    for (let i = entries.length - 1; i > 0; i--) {
                        const j = Math.floor(Math.random() * (i + 1));
                        [entries[i], entries[j]] = [entries[j], entries[i]];
                    }
                }

                return entries;
            },

            selectedUnitTweakCount: function(): number {
                return this.selected.filter(iter => iter.type == "unit").length;
            },

            selectedDefTweakCount: function(): number {
                return this.selected.filter(iter => iter.type == "def").length;
            }
        },

        components: {
            Busy, Collapsible, draggable
        }
    });
    export default Tweaks;

</script>