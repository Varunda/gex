
<template>
    <div class="list-group-item pl-2 pr-0 pt-1 pb-0 border-0 rounded-0" :class="classes">
        <span class="d-table-cell w-100 pl-2">
            {{ node.type }}:
            {{ node.token.value }}
        </span>

        <div class="list-group pb-0">
            <search-node-view v-for="(child, index) in node.children" :node="child" :key="node.depth + '-' + index"></search-node-view>
        </div>
    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { SearchNode } from "model/ApiMatchSearchResults";

    export const SearchNodeView = Vue.extend({
        name: "search-node-view",

        props: {
            node: { type: Object as PropType<SearchNode>, required: true }
        },

        data: function() {
            return {
                opened: false as boolean,
                colors: ["#d36dae", "#e76755", "#e1894c", "#e7b555", "#9ac161", "#5db990", "#479de7", "#7b6aeb", "#bc6fde", "#8ae1af"] as string[]
            }
        },

        computed: {
            classes: function() {
                return {
                    "mb-2": this.node.children.length > 0,
                    "pb-1": this.node.children.length > 0
                }
            }
        }
    });
    export default SearchNodeView;

</script>