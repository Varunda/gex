<template>
    <div style="display: grid; grid-template-rows: min-content 1fr; gap: 0.5rem; max-height: 100vh; max-width: 100vw;">
        <app-menu></app-menu>

        <div class="d-grid" style="grid-template-columns: 500px 1fr; gap: 1rem; overflow: hidden">
            <div class="overflow-y-auto">
                <h1 class="wt-header">
                    cache keys
                    <span v-if="keys.state == 'loaded'">
                        ({{keys.data.length}})
                    </span>

                    <button class="btn btn-primary" @click="loadKeys">refresh</button>
                </h1>

                <div v-if="keys.state == 'idle'"></div>

                <div v-else-if="keys.state == 'loading'">
                    loading...
                </div>

                <div v-else-if="keys.state == 'loaded'" class="list-group border-right border-bottom rounded-0">
                    <cache-item :item="root"></cache-item>
                </div>
            </div>

            <div class="overflow-y-auto">
                <h1 class="wt-header fw-normal d-flex">
                    <span class="flex-grow-1">
                        cache entry

                        <span v-if="selected.key != ''">
                            <span class="font-monospace fw-bold">
                                <code>{{selected.key}}</code>
                            </span>
                        </span>

                        <span v-if="selected.meta.state == 'loaded'" class="fs-5">
                            <br />
                                (created at
                                {{selected.meta.data.created | moment("YYYY-MM-DD hh:mm:ss")}},
                                used 
                                {{selected.meta.data.uses}} times,
                                last accessed
                                {{selected.meta.data.lastAccessed | moment("YYYY-MM-DD hh:mm:ss")}})
                        </span>
                    </span>

                    <span class="flex-grow-0">
                        <button v-if="selected.key != ''" class="btn btn-danger me-3 border-right" @click="evict">
                            evict
                        </button>

                        <button v-if="selected.key != ''" class="btn btn-primary" @click="loadKey(selected.key)">
                            refresh
                        </button>

                        <button class="btn" @click="formatted = true" :class="[ formatted == true ? 'btn-primary' : 'btn-secondary btn-outline-primary' ]">
                            view pretty
                        </button>

                        <button class="btn btn-primary" @click="formatted = false" :class="[ formatted == true ? 'btn-secondary btn-outline-primary' : 'btn-primary' ]">
                            view raw
                        </button>
                    </span>
                </h1>

                <div v-if="selected.value.state == 'loaded'" class="text-break">
                    <span v-if="formatted == true">
                        <code><pre>{{JSON.stringify(JSON.parse(selected.value.data), null, 4)}}</pre></code>
                    </span>
                    <span v-else>
                        <code>{{JSON.stringify(JSON.parse(selected.value.data), null, 0)}}</code>
                    </span>
                </div>
            </div>

        </div>

    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";
    import EventBus from "EventBus";

    import { GexMenu } from "components/AppMenu";

    import { CacheApi, CacheEntryMetadata } from "api/CacheApi";

    import "filters/MomentFilter";

    import { CacheEntry } from "./common";
    import CacheItem from "./CacheItem.vue";
    import Toaster from "../../Toaster";

    export const CacheView = Vue.extend({
        props: {

        },

        data: function() {
            return {
                formatted: false as boolean,

                keys: Loadable.idle() as Loading<string[]>,

                root: new CacheEntry as CacheEntry,

                selected: {
                    key: "" as string,
                    value: Loadable.idle() as Loading<string>,
                    meta: Loadable.idle() as Loading<CacheEntryMetadata>
                }
            }
        },

        mounted: function(): void {
            document.title = "Honooru / Cache";

            this.loadKeys();

            EventBus.$on("view-key", (key: string) => {
                console.log(`viewing key ${key}`);
                this.selected.key = key;
                this.loadKey(key);
            });
        },

        methods: {
            loadKeys: async function(): Promise<void> {
                this.keys = Loadable.loading();

                const l: Loading<string[]> = await CacheApi.getKeys();

                if (l.state != "loaded") {
                    return;
                }

                for (const key of l.data) {
                    let item: CacheEntry = this.root;
                    let newKey = "";

                    const parts: string[] = key.split(".");
                    for (const [index, part] of parts.entries()) {
                        newKey += part;
                        if (index != parts.length - 1) {
                            newKey += ".";
                        }

                        if (item.children.has(part) == false) {
                            const newItem: CacheEntry = new CacheEntry();
                            newItem.key = newKey;
                            newItem.parent = item;
                            newItem.depth = item.depth + 1;

                            item.children.set(part, newItem);
                        }

                        item = item.children.get(part)!; // force is fine, set above
                    }
                }

                function iter(elem: CacheEntry | null): void {
                    if (elem == null) {
                        return;
                    }

                    for (const [key, child] of elem.children) {
                        iter(child);
                    }

                    if (elem.children.size == 0) {
                        return;
                    }

                    console.log(`checking if ${elem.key}@${elem.depth} can be flattened`);
                    if (elem.children.size == 1) {
                        const keys: string[] = Array.from(elem.children.keys());
                        const firstKey = keys[0];

                        const first: CacheEntry = elem.children.get(firstKey)!;
                        console.log(`flattening ${elem.key} into ${first.key}/${firstKey}`);

                        elem.key = elem.children.get(firstKey)!.key;
                        elem.children.clear();
                    }
                }

                //iter(this.root);

                this.keys = l;
            },

            loadKey: async function(key: string): Promise<void> {
                this.selected.value = Loadable.loading();
                this.selected.meta = Loadable.loading();

                this.selected.value = await CacheApi.getValue(key);

                if (this.selected.value.state == "loaded" && this.selected.value.data.length > 1024 * 100) {
                    console.log(`CacheView> turning off formatting as value is ${this.selected.value.data.length} (which is over ${1024 * 100})`);
                    this.formatted = false;
                }

                this.selected.meta = await CacheApi.getMetadata(key);
            },

            evict: async function(): Promise<void> {
                if (this.selected.key == "") {
                    console.warn(`CacheView> not evicting, no key is selected`);
                    return;
                }

                const key: string = this.selected.key;
                const l: Loading<void> = await CacheApi.evict(key);
                if (l.state == "loaded") {
                    Toaster.add("entry evicted", `successfully evicted ${key} from cache`, "success");
                    this.loadKeys();
                } else {
                    console.error(`unchecked response state (evict): ${l.state}`);
                }
            }

        },

        components: {
            GexMenu, CacheItem
        }
    });
    export default CacheView;

</script>