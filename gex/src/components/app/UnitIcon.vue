
<template>
    <img :src="url" :height="size" :width="size" :title="HideTitle == false ? ('Unit icon for ' + name) : undefined">
</template>

<script lang="ts">
    import Vue from "vue";

    export const UnitIcon = Vue.extend({
        props: {
            name: { type: String, required: true },
            color: { type: String, required: false },
            size: { type: Number, required: false, default: 24 },
            HideTitle: { type: Boolean, required: false, default: false }
        },

        computed: {
            url: function(): string {
                let str: string = `/image-proxy/UnitIcon?defName=${encodeURIComponent(this.name)}`;

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