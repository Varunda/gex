<template>
    <collapsible header-text="Match tweaks" :show=false bg-color="bg-secondary" size-class="h4">
        <h4>The following Lua scripts were ran to tweak units or unit definitions</h4>

        <template v-for="found in foundTweaks">
            <collapsible :key="found[0]" :header-text="found[0] + ': ' + tweakName(found[1])" :show=false size-class="h5">
                <div>
                    <pre><code>{{ unbase64(found[1]) }}</code></pre>
                </div>
            </collapsible>
        </template>
    </collapsible>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarMatch } from "model/BarMatch";

    import Collapsible from "components/Collapsible.vue";

    // https://stackoverflow.com/a/15016605
    // why a golfed one? idk lmao
    const decodeBase64 = function(s: string) {
        var e: any={},i,b=0,c,x,l=0,a,r='',w=String.fromCharCode,L=s.length;
        var A="ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
        for(i=0;i<64;i++){e[A.charAt(i)]=i;}
        for(x=0;x<L;x++){
            c=e[s.charAt(x)];b=(b<<6)+c;l+=6;
            while(l>=8){((a=(b>>>(l-=8))&0xff)||(x<(L-2)))&&(r+=w(a));}
        }
        return r;
    };

    export const MatchTweaks = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true }
        },

        data: function() {
            return {

                tweaks: [
                    "tweakdefs",
                    "tweakdefs1", "tweakdefs2", "tweakdefs3",
                    "tweakdefs4", "tweakdefs5", "tweakdefs6",
                    "tweakdefs7", "tweakdefs8", "tweakdefs9",
                    "tweakunits",
                    "tweakunits1", "tweakunits2", "tweakunits3",
                    "tweakunits4", "tweakunits5", "tweakunits6",
                    "tweakunits7", "tweakunits8", "tweakunits9",
                ] as string[]
            }
        },

        methods: {
            unbase64: function(str: string): string {
                try {
                    return decodeBase64(str);
                } catch (err) {
                    console.error(`failed to unbase64 ${str}`);
                    return str;
                }
            },

            tweakName: function(str: string): string {
                const name = this.unbase64(str);

                if (name.startsWith("--")) {
                    let end = name.indexOf("\n");
                    if (end == -1 || end > 75) {
                        end = 75;
                    }

                    // 2 - ignore the -- at the start
                    return name.slice(2, end);
                }

                return "<unknown>";
            }
        },

        computed: {
            foundTweaks: function(): [string, string][] {
                const found: [string, string][] = [];

                for (const key of this.tweaks) {
                    const t = this.match.gameSettings[key];
                    // 'Ow' => ';'
                    if (t != "" && t != "Ow") {
                        found.push([key, t]);
                    }
                }

                return found;
            }

        },

        components: {
            Collapsible

        }
    });
    export default MatchTweaks;
    
</script>