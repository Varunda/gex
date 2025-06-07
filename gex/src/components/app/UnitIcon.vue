
<template>
    <img :src="url" :height="size" :width="size" :title="'Unit icon for ' + name">
</template>

<script lang="ts">
    import Vue from "vue";

    export const UnitIcon = Vue.extend({
        props: {
            name: { type: String, required: true },
            color: { type: String, required: false },
            size: { type: Number, required: false, default: 24 }
        },

        computed: {
            url: function(): string {
                let str: string = `/image-proxy/UnitIcon?defName=${this.name}`;

                if (this.color) {
                    let c: string = this.color;
                    if (c.startsWith("#")) {
                        c = Number.parseInt(c.substring(1), 16).toString();
                    }

                    str += `&color=${c}`;
                }

                return str;
            }
        }

    });
    export default UnitIcon;
</script>