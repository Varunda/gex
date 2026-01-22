<template>
    <img :src="url" :height="size" :width="size" :title="'Unit pic for ' + name">
</template>

<script lang="ts">
    import Vue from "vue";

    export const UnitPic = Vue.extend({
        props: {
            name: { type: String, required: true },
            size: { type: Number, required: false, default: 24 }
        },

        computed: {
            url: function(): string {
                return `/image-proxy/UnitPic?defName=${encodeURIComponent(this.name)}`;
            }
        }
    });
    export default UnitPic;
</script>