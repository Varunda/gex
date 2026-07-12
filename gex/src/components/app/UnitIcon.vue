
<template>
    <!--
    <img :src="url" :height="size" :width="size" :title="HideTitle == false ? ('Unit icon for ' + name) : undefined">
    -->
    <div :class="cssClass" :style="cssStyle"></div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading } from "Loading";

    import { UnitIconAtlasOffsetApi } from "api/UnitIconAtlasOffsetApi";

    import { UnitIconAtlasJson } from "model/UnitIconAtlasData";

    export const UnitIcon = Vue.extend({
        props: {
            name: { type: String, required: true },
            color: { type: String, required: false },
            size: { type: Number, required: false, default: 24 },
            HideTitle: { type: Boolean, required: false, default: false }
        },

        data: function() {
            return {
                id: Math.floor(Math.random() * 100000) as number,
                //offset: 0 as number,
                //atlasWidth: 0 as number,
            }
        },

        mounted: function(): void {
            //this.getOffset();
        },

        methods: {

        },

        computed: {

            cssClass: function(): any {
                return [ "bui", `bui-${this.name}` ];
            },

            cssStyle: function(): any {
                return {
                    '--size': this.size,
                    '--url': this.url,
                    /*
                    'display': "inline-block",
                    'width': `${this.size}px`,
                    'height': `${this.size}px`,
                    'background-image': `url("${this.url}")`,
                    // scale to the size of the icon wanted
                    'background-size': `${this.size / 32 * this.atlasWidth}px ${this.size}px`,
                    'background-position': `-${this.offset * (this.size / 32)}px 0px`,
                    'background-repeat': 'no-repeat'
                    */
                }
            },

            url: function(): string {
                let str: string = `url("/image-proxy/UnitIconAtlas`;

                if (this.color) {
                    let c: string = this.color;
                    if (c.startsWith("#")) {
                        c = Number.parseInt(c.substring(1), 16).toString();
                    }

                    str += `?color=${c}`;
                }

                return str + "\")";
            }
        }

    });
    export default UnitIcon;
</script>