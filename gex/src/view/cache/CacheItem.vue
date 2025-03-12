<template>
    <div class="list-group-item pl-2 pr-0 pt-1 pb-0 border-right-0 rounded-0 text-dark" :style="{ 'background-color': colors[item.depth] }">
        <span class="bi-chevron-right d-table-cell" v-if="children.length > 0 && opened == false"></span>
        <span class="bi-chevron-down d-table-cell" v-if="children.length > 0 && opened == true"></span>
        <span class="bi-dash d-table-cell" v-if="children.length == 0"></span>

        <span @click="clicked" class="d-table-cell w-100 pl-2">
            {{item.key || "root"}}
            <span v-show="children.length > 0" class="">
                ({{children.length}})
            </span>
        </span>

        <div v-if="opened == true">
            <div class="list-group pb-0">
                <cache-item v-for="item in children" :item="item" :key="item.key">
                </cache-item>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { CacheEntry } from "./common";

    import EventBus from "EventBus";

    export const CacheItem = Vue.extend({
        name: "cache-item",

        props: {
            item: { type: Object as PropType<CacheEntry>, required: true }
        },

        data: function() {
            return {
                opened: false as boolean,
                colors: ["#d36dae", "#e76755", "#e1894c", "#e7b555", "#9ac161", "#5db990", "#479de7", "#7b6aeb", "#bc6fde", "#8ae1af"] as string[]
            }
        },

        mounted: function(): void {
            if (this.item.depth > 0 && this.children.length == 1 && this.item.parent?.children?.size == 1) {
                this.opened = true;
            }
        },

        computed: {
            children: function(): CacheEntry[] {
                // this is probably not reactive!
                return Array.from(this.item.children.values());
            }
        },

        methods: {
            clicked: function(): void {
                if (this.children.length != 0) {
                    this.opened = !this.opened;
                } else if (this.item.key != "") {
                    EventBus.$emit("view-key", this.item.key);
                } else {
                    console.warn(`root has no children?`);
                }
            }
        }
    });
    export default CacheItem;

</script>