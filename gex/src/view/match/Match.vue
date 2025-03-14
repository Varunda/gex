
<template>
    <div>
        <div class="d-flex align-items-center">
            <gex-menu class="flex-grow-1"></gex-menu>
        </div>

        <div class="container">
            <div v-if="match.state == 'idle'"> </div>

            <div v-else-if="match.state == 'loading'">
                Loading...
            </div>

            <div v-else-if="match.state == 'loaded'">
                <h1>{{ match.data.map }}</h1>

                <h2>played on {{ match.data.startTime | moment }}</h2>

                <h2>took {{  match.data.durationMs / 1000 | mduration }}</h2>

                <h2>
                    {{ match.data.allyTeams.map(iter => iter.playerCount).join(" v ") }}
                </h2>

                <svg id="map-svg" :style="svgStyle">


                </svg>

                <img id="map-dims" :src="mapUrl" style="display: none;">


            </div>
        </div>
    </div>
    
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loading, Loadable } from "Loading";

    import "filters/MomentFilter";

    import { GexMenu } from "components/AppMenu";
    import InfoHover from "components/InfoHover.vue";

    import { BarMatchApi } from "api/BarMatchApi";
    import { GameOutput } from "model/GameOutput";
    import { GameOutputApi } from "api/GameOutputApi";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";
import { BarMatchPlayer } from "model/BarMatchPlayer";

    export const Match = Vue.extend({
        props: {

        },

        data: function() {
            return {
                gameID: "" as string,

                match: Loadable.idle() as Loading<BarMatch>,
                output: Loadable.idle() as Loading<GameOutput>,

                svg: null as SVGElement | null
            };
        },

        created: function(): void {
            this.gameID = location.pathname.split("/")[2];
        },

        beforeMount: function(): void {
            this.loadMatch();
            this.loadOutput();
        },

        methods: {

            loadMatch: async function(): Promise<void> {
                this.match = Loadable.loading();
                this.match = await BarMatchApi.getByID(this.gameID);

                if (this.match.state != "loaded") {
                    return;
                }

                const players: BarMatchPlayer[] = this.match.data.players;

                this.$nextTick(() => {
                    const img: HTMLElement | null = document.getElementById("map-dims");
                    this.svg = document.getElementById("map-svg") as SVGElement | null;
                    if (img == null) {
                        console.error(`missing #map-dims!`);
                        return;
                    }
                    
                    img.addEventListener("load", (ev: Event) => {
                        if (this.svg == null) {
                            console.error(`missing #map-svg`);
                            return;
                        }

                        const h: number = (img as HTMLImageElement).naturalHeight;
                        const w: number = (img as HTMLImageElement).naturalHeight;
                        console.log(`image is ${h} x ${w}`);

                        this.svg.setAttribute("height", `${h}`);
                        this.svg.setAttribute("width", `${w}`);

                        for (const player of players) {
                            
                            const dot = document.createElementNS("http://www.w3.org/2000/svg", "circle");
                            dot.setAttributeNS(null, "cx", "50%");
                            dot.setAttributeNS(null, "cy", "50%");
                            dot.setAttributeNS(null, "r", "10%");
                            dot.setAttributeNS(null, 'style', 'stroke: blue; stroke-width: 1px;');

                            this.svg.appendChild(dot);
                        }
                    });
                });
            },

            loadOutput: async function(): Promise<void> {
                this.output = Loadable.loading();
                this.output = await GameOutputApi.getEvents(this.gameID);
            }
        },

        computed: {

            mapUrl: function(): string {
                if (this.match.state != "loaded") {
                    return "";
                }
                return `https://api.bar-rts.com/maps/${this.match.data.map.replace(/ /gm, "_").toLowerCase()}/texture-mq.jpg`;
            },

            svgStyle: function(): any {
                return {
                    "background-image": `url(${this.mapUrl})`
                };
            }

        },

        components: {
            GexMenu
        }
    });
    export default Match;
</script>