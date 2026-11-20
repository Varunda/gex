
<template>
    <div>
        <h2 class="wt-header">
            Tweaks
        </h2>

        <div class="alert alert-info text-center">
            Tweaks with the same name can appear multiple times due to the code within the tweak being different
        </div>

        <div v-if="entries.state == 'idle'"></div>

        <div v-else-if="entries.state == 'loading'">
            <busy class="busy busy-sm"></busy>
        </div>

        <div v-else-if="entries.state == 'loaded'">
            <div v-for="entry in groupedByHash" :key="entry.hash" class="mb-3 border-bottom pb-3">
                <h4 class="mb-0 d-flex">
                    <div class="flex-grow-1">
                        {{ entry.name || "&lt;no name&gt;" }} = <span class="font-monospace">{{ entry.base64.length }}:{{ entry.lobbyHash.slice(0, 4) }}</span>
                    </div>
                </h4>
                <div class="mb-2">
                    <span class="fs-6">
                        seen {{ entry.count }} times
                    </span>
                    <button class="btn btn-primary btn-sm" @click="copyTweak(entry.hash)">Copy</button>
                </div>

                <collapsible header-text="View Lua code" :show="false" size-class="h6" class="mb-0" bg-color="bg-secondary">
                    <pre style="background: var(--bs-light-bg-subtle)" class="p-2"><code>{{ entry.code }}</code></pre>
                </collapsible>
            </div>

            <div>
                Loaded {{ entries.data.length }} tweaks
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import Busy from "components/Busy.vue";
    import Collapsible from "components/Collapsible.vue";

    import { BarTweakEntry } from "model/BarTweakEntry";

    import { BarTweakEntryApi } from "api/BarTweakEntryApi";
    import Toaster from "Toaster";

    export const Tweaks = Vue.extend({
        props: {

        },

        data: function() {
            return {
                entries: Loadable.idle() as Loading<BarTweakEntry[]>,

                sortBy: "count" as "count" | "name" | "random",
                sortDir: "desc" as "desc" | "asc"
            }
        },

        mounted: function(): void {
            document.title = "Gex / Tweaks";
            this.bindAll();
        },

        methods: {
            bindAll: async function(): Promise<void> {
                this.entries = Loadable.loading();
                this.entries = await BarTweakEntryApi.getAll();
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
                    if (map.has(entry.hash) == false) {
                        map.set(entry.hash, entry);
                    }

                    map.get(entry.hash)!.count += entry.count;
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
            }

        },

        components: {
            Busy, Collapsible
        }
    });
    export default Tweaks;

</script>